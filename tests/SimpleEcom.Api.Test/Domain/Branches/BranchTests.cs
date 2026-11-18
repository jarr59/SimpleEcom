using SimpleEcom.Api.Domain.Branches;
using SimpleEcom.Api.Domain.Branches.Events;

namespace SimpleEcom.Api.Test.Domain.Branches;

public class BranchTests
{
    private static Branch NewBranch(bool allowsPickup = true, bool allowsDelivery = true) =>
        Branch.Create("CENTRO", "Sucursal Centro", "Calle 1", null, allowsPickup, allowsDelivery, TimeProvider.System).Value;

    [Fact]
    public void Create_WithValidData_NormalizesFieldsStartsActiveAndRaisesDomainEvent()
    {
        var result = Branch.Create(" centro ", " Sucursal Centro ", " Calle 1 ", "  ", allowsPickup: true, allowsDelivery: false, TimeProvider.System);

        Assert.True(result.IsSuccess);
        Assert.Equal("CENTRO", result.Value.Code);
        Assert.Equal("Sucursal Centro", result.Value.Name);
        Assert.Equal("Calle 1", result.Value.Address);
        Assert.Null(result.Value.Phone);
        Assert.True(result.Value.AllowsPickup);
        Assert.False(result.Value.AllowsDelivery);
        Assert.True(result.Value.IsActive);
        Assert.IsType<BranchCreatedDomainEvent>(Assert.Single(result.Value.DomainEvents));
    }

    [Theory]
    [InlineData("", "Centro", "Calle 1", true, true, "Branch.CodeRequired")]
    [InlineData("C1", " ", "Calle 1", true, true, "Branch.NameRequired")]
    [InlineData("C1", "Centro", "", true, true, "Branch.AddressRequired")]
    [InlineData("C1", "Centro", "Calle 1", false, false, "Branch.NoFulfillmentOption")]
    public void Create_WithInvalidData_ReturnsValidationError(
        string code, string name, string address, bool allowsPickup, bool allowsDelivery, string expectedCode)
    {
        var result = Branch.Create(code, name, address, null, allowsPickup, allowsDelivery, TimeProvider.System);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public void Update_ValidData_ChangesEditableFieldsButKeepsCode()
    {
        var branch = NewBranch();

        var result = branch.Update("Centro Renovado", "Calle 2", " 555-0100 ", allowsPickup: false, allowsDelivery: true);

        Assert.True(result.IsSuccess);
        Assert.Equal("CENTRO", branch.Code);
        Assert.Equal("Centro Renovado", branch.Name);
        Assert.Equal("Calle 2", branch.Address);
        Assert.Equal("555-0100", branch.Phone);
        Assert.False(branch.AllowsPickup);
        Assert.True(branch.AllowsDelivery);
    }

    [Fact]
    public void Update_WithoutFulfillmentOptions_FailsWithoutPartialChanges()
    {
        var branch = NewBranch();

        var result = branch.Update("Otro nombre", "Otra calle", null, allowsPickup: false, allowsDelivery: false);

        Assert.Equal(BranchErrors.NoFulfillmentOption, result.Error);
        Assert.Equal("Sucursal Centro", branch.Name);
        Assert.Equal("Calle 1", branch.Address);
        Assert.True(branch.AllowsPickup);
    }

    [Fact]
    public void DeactivateAndActivate_ToggleIsActive()
    {
        var branch = NewBranch();

        branch.Deactivate();
        Assert.False(branch.IsActive);

        branch.Activate();
        Assert.True(branch.IsActive);
    }
}
