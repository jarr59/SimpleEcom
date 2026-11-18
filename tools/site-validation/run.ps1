<#
.SYNOPSIS
    Compila, ejecuta los tests, arranca Api + tienda + Admin y los valida con validate.mjs.

.DESCRIPTION
    Escribe en -OutDir: summary.txt (resumen), build.log, tests.log, validation.log, report.json,
    capturas de los fallos y los logs de cada aplicación (api.log, store.log, admin.log + .err).
    Al terminar detiene las aplicaciones que arrancó. Código de salida 0 solo si todo pasa.

.PARAMETER SkipTests
    No ejecuta dotnet test (sí compila).

.PARAMETER UseRunning
    No compila ni arranca nada: valida las instancias que ya estén levantadas en los puertos de launchSettings.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/site-validation/run.ps1
#>
param(
    [switch]$SkipTests,
    [switch]$UseRunning,
    [string]$OutDir = (Join-Path $env:TEMP 'simpleecom-validation')
)

# Continue: en Windows PowerShell 5.1, con Stop, cualquier línea de stderr redirigida de dotnet/npm aborta el script.
$ErrorActionPreference = 'Continue'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$here = $PSScriptRoot
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')

if (Test-Path $OutDir) { Remove-Item -Recurse -Force $OutDir }
New-Item -ItemType Directory -Force $OutDir | Out-Null

# Smart App Control/WDAC de Windows bloquea a veces DLLs recién compiladas (error 0x800711C7), en bin/ o en cualquier
# otra carpeta. No se puede evitar desde aquí: se detecta y se informa como problema del entorno, no del código.
$wdacPattern = '0x800711C7'
$wdacNote = 'Windows (Smart App Control/WDAC) bloqueó una DLL recién compilada: es del entorno, no del código.'

$summary = [System.Collections.Generic.List[string]]::new()
function Add-Summary([string]$line) { $summary.Add($line); Write-Host $line }

# Ejecuta un comando nativo desde la carpeta actual con su salida (UTF-8, tal cual) en $OutDir\$log.
# Vía cmd: en Windows PowerShell 5.1, *> escribe UTF-16 y envuelve stderr en errores de PowerShell.
function Invoke-Logged([string]$log, [string]$commandLine) {
    & cmd /c "$commandLine > `"$(Join-Path $OutDir $log)`" 2>&1"
    return $LASTEXITCODE
}

Set-Location $root

$apps = @(
    @{ Name = 'api';   Project = 'src/SimpleEcom.Api';          Port = 7221 },
    @{ Name = 'store'; Project = 'src/SimpleEcom.UI.Web';       Port = 7171 },
    @{ Name = 'admin'; Project = 'src/SimpleEcom.UI.Web.Admin'; Port = 7146 }
)
$started = @()
$exitCode = 0

try {
    if (-not $UseRunning) {
        $busy = $apps | Where-Object { Get-NetTCPConnection -LocalPort $_.Port -State Listen -ErrorAction SilentlyContinue }
        if ($busy) {
            Add-Summary "SETUP FAIL: puertos ocupados ($(($busy | ForEach-Object { "$($_.Name):$($_.Port)" }) -join ', ')). Detén esas aplicaciones o usa -UseRunning."
            exit 2
        }

        if ((Invoke-Logged 'build.log' 'dotnet build SimpleEcom.slnx -nologo -v q') -ne 0) {
            Add-Summary "BUILD FAIL (ver build.log)"
            exit 2
        }
        Add-Summary "BUILD OK"

        if (-not $SkipTests) {
            $testExit = Invoke-Logged 'tests.log' 'dotnet test SimpleEcom.slnx --no-build -nologo'
            $testLog = Get-Content -Encoding UTF8 (Join-Path $OutDir 'tests.log')
            if ($testExit -ne 0) { Add-Summary "TESTS FAIL (ver tests.log)"; $exitCode = 1 } else { Add-Summary "TESTS OK" }
            $testLog | Where-Object { $_ -match '^\s*(Passed!|Failed!)|Catastrophic|0x800711C7' } |
                Select-Object -First 10 | ForEach-Object { Add-Summary "  $($_.Trim())" }
            if ($testLog -match $wdacPattern) { Add-Summary "  AVISO tests: $wdacNote" }
        }

        foreach ($app in $apps) {
            $process = Start-Process dotnet `
                -ArgumentList @('run', '--no-build', '--launch-profile', 'https', '--project', $app.Project) `
                -WorkingDirectory $root -WindowStyle Hidden -PassThru `
                -RedirectStandardOutput (Join-Path $OutDir "$($app.Name).log") `
                -RedirectStandardError (Join-Path $OutDir "$($app.Name).err")
            $started += $process
        }
        Add-Summary "APPS arrancadas: api :7221, tienda :7171, admin :7146"
    }

    if (-not (Test-Path (Join-Path $here 'node_modules\playwright'))) {
        $env:PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD = '1'
        Push-Location $here
        try { $npmExit = Invoke-Logged 'npm.log' 'npm ci --no-audit --no-fund' } finally { Pop-Location }
        if ($npmExit -ne 0) { Add-Summary "NPM FAIL (ver npm.log)"; exit 2 }
    }

    $env:SIMPLEECOM_VALIDATION_OUT = $OutDir
    Push-Location $here
    try { $validationExit = Invoke-Logged 'validation.log' 'node validate.mjs' } finally { Pop-Location }
    Get-Content -Encoding UTF8 (Join-Path $OutDir 'validation.log') | Where-Object { $_ -match '^(FAIL|WARN)|^Resultado' } | ForEach-Object { Add-Summary $_ }
    if ($validationExit -ne 0) { $exitCode = 1 }

    # Aplicaciones que no llegaron a arrancar (la validación solo ve "no responde").
    foreach ($app in $apps) {
        $err = Join-Path $OutDir "$($app.Name).err"
        if ((Test-Path $err) -and (Select-String -Path $err -Pattern 'Unhandled exception' -Quiet)) {
            Add-Summary "APP FAIL $($app.Name): excepción al arrancar (ver $($app.Name).err)"
            if (Select-String -Path $err -Pattern $wdacPattern -Quiet) { Add-Summary "  AVISO $($app.Name): $wdacNote" }
        }
    }
}
finally {
    foreach ($process in $started) {
        # dotnet run lanza la aplicación como proceso hijo: se cierra el árbol entero...
        & taskkill /PID $process.Id /T /F *> $null
    }
    if ($started) {
        # ...y, por si el hijo quedó huérfano, lo que escuche en nuestros puertos (estaban libres al empezar).
        foreach ($app in $apps) {
            Get-NetTCPConnection -LocalPort $app.Port -State Listen -ErrorAction SilentlyContinue |
                ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
        }
    }
    $summary | Set-Content -Encoding utf8 (Join-Path $OutDir 'summary.txt')
    Write-Host "Resultados en $OutDir"
}

exit $exitCode
