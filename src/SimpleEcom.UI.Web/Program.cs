using SimpleEcom.Shared.Http;
using SimpleEcom.UI.Web.Common;
using SimpleEcom.UI.Web.Components;
using SimpleEcom.UI.Web.Features.Branches;

var builder = WebApplication.CreateBuilder(args);

// La tienda se renderiza en servidor de forma estática (SSR + streaming): rápida y apta para buscadores.
// Se mantiene el modo interactivo registrado para futuros componentes (p. ej. el carrito).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Clientes tipados hacia SimpleEcom.Api (URL en appsettings: Api:BaseUrl)
var apiBaseUrl = new Uri(builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Falta la configuración 'Api:BaseUrl'."));
builder.Services.AddHttpClient<ProductsApiClient>(client => client.BaseAddress = apiBaseUrl);
builder.Services.AddHttpClient<BranchesApiClient>(client => client.BaseAddress = apiBaseUrl);

builder.Services.AddSingleton(TimeProvider.System);

// ViewModels de Features/: uno nuevo por cada instancia de página (Transient).
builder.Services.AddViewModels(typeof(Program).Assembly);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // HSTS de 30 días por defecto; ajustar para producción: https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Valida el token de los formularios POST (p. ej. la elección de sucursal).
app.UseAntiforgery();

app.MapStaticAssets();
app.MapBranchSelection();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
