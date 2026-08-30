using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Infrastructure.Queries;

public sealed class ReferalProgramQueries(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource) : IReferalProgramQueries
{
    public async Task<IReadOnlyCollection<ReferalProgramResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                marketing_addr AS "MarketingAddr",
                is_task_processing_enabled AS "IsTaskProcessingEnabled"
            FROM public.referal_program
            ORDER BY marketing_addr;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return (await connection.QueryAsync<ReferalProgramResponse>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)))
            .AsList();
    }
}
