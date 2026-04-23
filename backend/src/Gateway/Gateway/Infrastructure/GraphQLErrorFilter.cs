using HotChocolate;
using MetadataService.Domain.Exceptions;

namespace Gateway.Infrastructure;

/// <summary>
/// HotChocolate error filter — chuyển domain exceptions thành GraphQL errors có cấu trúc.
/// </summary>
public sealed class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is null)
            return error;

        return error.Exception switch
        {
            // Domain exceptions → user-facing errors (không expose stacktrace)
            DatasetNotFoundException ex => error
                .WithMessage(ex.Message)
                .WithCode("DATASET_NOT_FOUND")
                .RemoveException(),

            DuplicateDatasetException ex => error
                .WithMessage(ex.Message)
                .WithCode("DUPLICATE_DATASET")
                .RemoveException(),

            FluentValidation.ValidationException ex => error
                .WithMessage("Validation failed.")
                .WithCode("VALIDATION_ERROR")
                .WithExtensions(new Dictionary<string, object?>
                {
                    ["errors"] = ex.Errors
                        .Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                        .ToArray()
                })
                .RemoveException(),

            NotImplementedException => error
                .WithMessage("Chức năng này chưa được implement.")
                .WithCode("NOT_IMPLEMENTED")
                .RemoveException(),

            // Unknown exceptions — giữ nguyên (HotChocolate sẽ ẩn details ở production)
            _ => error
        };
    }
}
