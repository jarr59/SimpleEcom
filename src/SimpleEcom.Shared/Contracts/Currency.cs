using System.Text.Json.Serialization;

namespace SimpleEcom.Shared.Contracts;

/// <summary>
/// Monedas admitidas por el ecommerce (códigos ISO 4217).
/// Se serializa como texto ("USD") en la Api. El valor por defecto es el dólar estadounidense.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Currency>))]
public enum Currency
{
    /// <summary>Dólar estadounidense.</summary>
    USD = 0,

    /// <summary>Euro.</summary>
    EUR = 1
}
