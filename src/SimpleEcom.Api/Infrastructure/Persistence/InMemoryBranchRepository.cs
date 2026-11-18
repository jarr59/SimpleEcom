using System.Collections.Concurrent;
using SimpleEcom.Api.Domain.Branches;

namespace SimpleEcom.Api.Infrastructure.Persistence;

/// <summary>Repositorio de sucursales en memoria. Se usa solo como doble en los tests.</summary>
public sealed class InMemoryBranchRepository : IBranchRepository
{
    private readonly ConcurrentDictionary<BranchId, Branch> _branches = new();

    public Task<Branch?> GetByIdAsync(BranchId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_branches.GetValueOrDefault(id));

    public Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Branch>>(_branches.Values.OrderBy(b => b.Name).ToList());

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = Branch.NormalizeCode(code);
        return Task.FromResult(_branches.Values.Any(b => b.Code == normalized));
    }

    public Task AddAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        _branches[branch.Id] = branch;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        _branches[branch.Id] = branch;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        _branches.TryRemove(branch.Id, out _);
        return Task.CompletedTask;
    }
}
