using SimpleEcom.Shared.Contracts;
using SimpleEcom.Shared.Domain;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Domain.Products;

public sealed class Money : ValueObject
{
    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public Currency Currency { get; }

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (amount < 0)
        {
            return ProductErrors.NegativePrice;
        }

        // Un entero fuera del enum (p. ej. 99 deserializado) no es una moneda válida.
        if (!Enum.IsDefined(currency))
        {
            return ProductErrors.InvalidCurrency;
        }

        return new Money(decimal.Round(amount, 2), currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
