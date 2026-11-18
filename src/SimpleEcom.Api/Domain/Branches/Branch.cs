using SimpleEcom.Api.Domain.Branches.Events;
using SimpleEcom.Shared.Domain;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Domain.Branches;

/// <summary>
/// Agregado Sucursal: punto de venta donde el cliente compra. Define qué modos de entrega ofrece
/// (retiro en tienda y/o envío a domicilio). Los precios y el stock por sucursal viven en sus propios agregados.
/// </summary>
public sealed class Branch : AggregateRoot<BranchId>
{
    private Branch(BranchId id, string code, string name, string address, string? phone, bool allowsPickup, bool allowsDelivery)
        : base(id)
    {
        Code = code;
        Name = name;
        Address = address;
        Phone = phone;
        AllowsPickup = allowsPickup;
        AllowsDelivery = allowsDelivery;
        IsActive = true;
    }

    /// <summary>Constructor para EF Core; no usar desde código de aplicación.</summary>
    private Branch() : base(default)
    {
        Code = null!;
        Name = null!;
        Address = null!;
    }

    /// <summary>Código corto e inmutable (p. ej. "CENTRO"), único entre sucursales.</summary>
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string? Phone { get; private set; }

    /// <summary>El cliente puede recoger sus pedidos en esta sucursal.</summary>
    public bool AllowsPickup { get; private set; }

    /// <summary>La sucursal vende con envío a domicilio.</summary>
    public bool AllowsDelivery { get; private set; }

    /// <summary>Una sucursal inactiva no aparece en la tienda ni acepta pedidos.</summary>
    public bool IsActive { get; private set; }

    public static Result<Branch> Create(
        string code,
        string name,
        string address,
        string? phone,
        bool allowsPickup,
        bool allowsDelivery,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BranchErrors.CodeRequired;
        }

        var validation = Validate(name, address, allowsPickup, allowsDelivery);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        var branch = new Branch(
            BranchId.New(),
            NormalizeCode(code),
            name.Trim(),
            address.Trim(),
            NormalizeOptional(phone),
            allowsPickup,
            allowsDelivery);

        branch.Raise(new BranchCreatedDomainEvent(branch.Id, timeProvider.GetUtcNow()));
        return branch;
    }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    /// <summary>
    /// Actualiza los datos editables de forma atómica: o se aplican todos o ninguno.
    /// El código es inmutable una vez creado.
    /// </summary>
    public Result Update(string name, string address, string? phone, bool allowsPickup, bool allowsDelivery)
    {
        var validation = Validate(name, address, allowsPickup, allowsDelivery);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Address = address.Trim();
        Phone = NormalizeOptional(phone);
        AllowsPickup = allowsPickup;
        AllowsDelivery = allowsDelivery;
        return Result.Success();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static Result Validate(string name, string address, bool allowsPickup, bool allowsDelivery)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(BranchErrors.NameRequired);
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            return Result.Failure(BranchErrors.AddressRequired);
        }

        if (!allowsPickup && !allowsDelivery)
        {
            return Result.Failure(BranchErrors.NoFulfillmentOption);
        }

        return Result.Success();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
