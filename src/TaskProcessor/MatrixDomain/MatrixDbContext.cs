namespace Matrix.Infrastructure.Data;

public sealed class MatrixDbContext(DbContextOptions<MatrixDbContext> options) : DbContext(options), IMatrixUnitOfWork
{
    internal DbSet<MultiPlace> MultiPlaces { get; init; }
    internal DbSet<MultiLock> MultiLocks { get; init; }
}