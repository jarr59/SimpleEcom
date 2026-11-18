using SimpleEcom.Shared.Domain;

namespace SimpleEcom.Api.Domain.Branches.Events;

public sealed record BranchCreatedDomainEvent(BranchId BranchId, DateTimeOffset OccurredOn) : IDomainEvent;
