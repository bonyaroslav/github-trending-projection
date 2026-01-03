using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace Api.Support;

internal static class ValidationProblemFactory
{
    public static IResult FromValidationResult(ValidationResult result)
    {
        var errors = result.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        return Results.ValidationProblem(errors);
    }
}
