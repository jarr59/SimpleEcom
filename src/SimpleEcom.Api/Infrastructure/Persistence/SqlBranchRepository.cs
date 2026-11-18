using Microsoft.EntityFrameworkCore;
using SimpleEcom.Api.Domain.Branches;

namespace SimpleEcom.Api.Infrastructure.Persistence;

/// <summary>Repositorio de sucursales sobre SQL Server (EF Core). Cada operación de escritura confirma sus cambios.</summary>
public sealed class SqlBranchRepository(SimpleEcomDbContext dbContext) : IBranchRepository
{
    public Task<Branch?> GetByIdAsync(BranchId id, CancellationToken cancellationToken = default) =>
        dbContext.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Branches.OrderBy(b => b.Name).ToListAsync(cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = Branch.NormalizeCode(code);
        return dbContext.Branches.AnyAsync(b => b.Code == normalized, cancellationToken);
    }

    public async Task AddAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        dbContext.Branches.Update(branch);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        dbContext.Branches.Remove(branch);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
