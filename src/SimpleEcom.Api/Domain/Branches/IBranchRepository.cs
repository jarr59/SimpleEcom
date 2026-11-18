namespace SimpleEcom.Api.Domain.Branches;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(BranchId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(Branch branch, CancellationToken cancellationToken = default);
    Task UpdateAsync(Branch branch, CancellationToken cancellationToken = default);
    Task RemoveAsync(Branch branch, CancellationToken cancellationToken = default);
}
