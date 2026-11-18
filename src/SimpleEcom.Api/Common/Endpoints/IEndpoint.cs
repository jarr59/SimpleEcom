namespace SimpleEcom.Api.Common.Endpoints;

/// <summary>Cada slice expone su endpoint implementando esta interfaz; se registra automáticamente.</summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
