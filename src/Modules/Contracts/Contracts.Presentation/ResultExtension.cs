using Ardalis.Result;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Contracts.Presentation;


public static class ResultExtension
{
    public static IResult ToResult(this Result result) =>
        GenerateIResult(result.Status, result.Errors);

    public static IResult ToResult<T>(this Result<T> result) =>
        GenerateIResult(result.Status, result.Errors);

    private static IResult GenerateIResult(ResultStatus resultStatus, IEnumerable<string> resultErrors)
    {
        return resultStatus switch
        {
            ResultStatus.Ok =>
                throw new InvalidOperationException("Cannot convert successful result to IResult"),
            ResultStatus.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", new[] { resultErrors } }
                }),
            ResultStatus.Forbidden => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access denied",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", new[] { resultErrors } }
                }),
            ResultStatus.Unauthorized => Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", new[] { resultErrors } }
                }),
            ResultStatus.Invalid => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", new[] { resultErrors } }
                }),
            ResultStatus.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", new[] { resultErrors } }
                }),
            ResultStatus.CriticalError => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", new[] { resultErrors } }
                }),
            ResultStatus.Unavailable => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Service unavailable",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", new[] { resultErrors } }
                }),
            ResultStatus.Error => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "An unexpected error occurred",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", new[] { resultErrors } }
                }),
            _ => throw new InvalidOperationException("Unhandled result status")
        };
    }
}