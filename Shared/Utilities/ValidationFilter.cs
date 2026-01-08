using FluentValidation;
using VoiceApi.Shared.Models;

namespace VoiceApi.Shared.Utilities;

public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();

        if (validator is not null)
        {
            // Find the argument of type T
            var argument = context.Arguments.OfType<T>().FirstOrDefault();
            if (argument is not null)
            {
                var validationResult = await validator.ValidateAsync(argument);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Results.BadRequest(
                        ApiResponse<object>.Fail("Validation failed", errors)
                    );
                }
            }
        }

        return await next(context);
    }
}
