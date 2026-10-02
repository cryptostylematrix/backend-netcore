using Microsoft.AspNetCore.Http;
using FastEndpoints;
using MediatR;
using ScheduledTasks.Application;
using ScheduledTasks.Application.Abstractions;

namespace ScheduledTasks.Presentation;

public sealed class GetPublicSchedulesRequest
{
    [QueryParam, BindFrom("module")]
    public string Module { get; init; } = "";
    [QueryParam, BindFrom("scope")]
    public string Scope { get; init; } = "";
    [QueryParam, BindFrom("resource_type")]
    public string ResourceType { get; init; } = "";
}

public sealed class GetPublicSchedulesEndpoint(ISender sender)
    : Endpoint<GetPublicSchedulesRequest, IReadOnlyList<PublicSchedule>>
{
    public override void Configure()
    {
        Get("/api/scheduled-tasks/schedules");
        Tags("ScheduledTasks");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetPublicSchedulesRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicSchedulesQuery(request.Module, request.Scope, request.ResourceType), ct);
        if (!result.IsSuccess)
        {
            await Send.ResultAsync(Results.Problem(statusCode: 400, title: "Module, scope and resource type are required."));
            return;
        }
        Response = result.Value;
    }
}
