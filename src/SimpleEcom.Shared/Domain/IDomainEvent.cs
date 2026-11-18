namespace SimpleEcom.Shared.Domain;

/// <summary>Evento de dominio: algo relevante que ya ocurrió dentro de un agregado.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
