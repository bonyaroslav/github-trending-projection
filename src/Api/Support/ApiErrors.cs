using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Api.Support;

internal static class ApiErrors
{
    public static IResult ValidationProblem(string field, string message)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = new[] { message }
        });
    }

    public static IResult NotFound(string detail)
    {
        return Results.Problem(
            title: "Not Found",
            detail: detail,
            statusCode: StatusCodes.Status404NotFound,
            type: "https://httpstatuses.com/404");
    }

    public static IResult Conflict(string detail)
    {
        return Results.Problem(
            title: "Conflict",
            detail: detail,
            statusCode: StatusCodes.Status409Conflict,
            type: "https://httpstatuses.com/409");
    }
}
