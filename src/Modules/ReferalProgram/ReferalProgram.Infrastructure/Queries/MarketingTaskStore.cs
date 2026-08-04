using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;

namespace ReferalProgram.Infrastructure.Queries;

public sealed class MarketingTaskStore(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource) : IMarketingTaskStore
{
    public async Task<bool> HasIncompleteAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM public.marketing_tasks
                WHERE status <> 'completed'
            );
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<bool> TryAddAsync(
        int taskKey,
        long taskQueryId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO public.marketing_tasks (task_key, task_query_id, status)
            VALUES (@taskKey, @taskQueryId, 'pending')
            ON CONFLICT (task_key, task_query_id) DO NOTHING;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var inserted = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { taskKey, taskQueryId },
                cancellationToken: cancellationToken));

        return inserted == 1;
    }
}
