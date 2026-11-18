using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Domain.Branches;

public static class BranchErrors
{
    public static readonly Error CodeRequired = Error.Validation("Branch.CodeRequired", "El código de la sucursal es obligatorio.");
    public static readonly Error NameRequired = Error.Validation("Branch.NameRequired", "El nombre de la sucursal es obligatorio.");
    public static readonly Error AddressRequired = Error.Validation("Branch.AddressRequired", "La dirección de la sucursal es obligatoria.");

    public static readonly Error NoFulfillmentOption = Error.Validation(
        "Branch.NoFulfillmentOption",
        "La sucursal debe ofrecer retiro en tienda, envío a domicilio o ambos.");

    public static Error NotFound(BranchId id) => Error.NotFound("Branch.NotFound", $"No existe la sucursal '{id.Value}'.");
    public static Error CodeAlreadyExists(string code) => Error.Conflict("Branch.CodeAlreadyExists", $"Ya existe una sucursal con código '{code}'.");
}
