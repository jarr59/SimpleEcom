using SimpleEcom.UI.Web.Admin.Common;
using SimpleEcom.UI.Web.Admin.Components;
using SimpleEcom.Shared.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Cliente tipado hacia SimpleEcom.Api (URL en appsettings: Api:BaseUrl)
builder.Services.AddHttpClient<ProductsApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Falta la configuración 'Api:BaseUrl'.")));

// ViewModels de Features/: uno nuevo por cada instancia de página (Transient).
builder.Services.AddViewModels(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
