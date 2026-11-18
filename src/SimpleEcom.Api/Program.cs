using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Infrastructure;
using SimpleEcom.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeatureHandlers(typeof(Program).Assembly);
builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Crea/actualiza la base de datos y carga datos de ejemplo si está vacía.
await DatabaseInitializer.InitializeAsync(app.Services);

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapEndpoints(app.MapGroup("api"));

app.Run();

public partial class Program;
