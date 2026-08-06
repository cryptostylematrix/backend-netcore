using Ardalis.Result;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace ReferalProgram.Presentation;

public static class ResultExtension
{
    public static IResult ToResult<T>(this Result<T> result) =>
        result.Status switch
        {
            ResultStatus.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found"),
            ResultStatus.Error => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Could not process request",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", result.Errors }
                }),
            _ => throw new InvalidOperationException($"Unhandled result status: {result.Status}")
        };
}
