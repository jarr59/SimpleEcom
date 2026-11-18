using System.ComponentModel.DataAnnotations;
using SimpleEcom.Shared.Contracts.Branches;

namespace SimpleEcom.UI.Web.Admin.Features.Branches;

/// <summary>
/// Modelo del formulario de sucursal. Solo valida en la UI para dar feedback inmediato;
/// las reglas de negocio reales las aplica el dominio en la Api.
/// </summary>
public sealed class BranchFormModel : IValidatableObject
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(500, ErrorMessage = "La dirección no puede superar los 500 caracteres.")]
    public string Address { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    public string? Phone { get; set; }

    public bool AllowsPickup { get; set; } = true;

    public bool AllowsDelivery { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AllowsPickup && !AllowsDelivery)
        {
            yield return new ValidationResult(
                "La sucursal debe ofrecer retiro en tienda, envío a domicilio o ambos.",
                [nameof(AllowsPickup), nameof(AllowsDelivery)]);
        }
    }

    public static BranchFormModel FromResponse(BranchResponse branch) => new()
    {
        Code = branch.Code,
        Name = branch.Name,
        Address = branch.Address,
        Phone = branch.Phone,
        AllowsPickup = branch.AllowsPickup,
        AllowsDelivery = branch.AllowsDelivery
    };

    public CreateBranchRequest ToCreateRequest() => new(Code, Name, Address, Phone, AllowsPickup, AllowsDelivery);

    public UpdateBranchRequest ToUpdateRequest() => new(Name, Address, Phone, AllowsPickup, AllowsDelivery);
}
