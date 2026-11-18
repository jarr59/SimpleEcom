using SimpleEcom.Shared.Domain;

namespace SimpleEcom.Api.Domain.Products.Events;

public sealed record ProductCreatedDomainEvent(ProductId ProductId, DateTimeOffset OccurredOn) : IDomainEvent;
