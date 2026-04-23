using FluentValidation;

namespace MetadataService.Application.Datasets.Commands.CreateDataset;

public sealed class CreateDatasetCommandValidator : AbstractValidator<CreateDatasetCommand>
{
    private static readonly string[] ValidSourceTypes = ["postgresql", "view", "custom_sql"];

    public CreateDatasetCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.CreatedBy).NotEmpty().WithMessage("CreatedBy is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dataset name is required.")
            .MaximumLength(255).WithMessage("Dataset name must not exceed 255 characters.")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_\s\-]+$")
            .WithMessage("Name must start with a letter and contain only letters, digits, spaces, underscores, or hyphens.");

        RuleFor(x => x.SourceType)
            .NotEmpty().WithMessage("SourceType is required.")
            .Must(t => ValidSourceTypes.Contains(t.ToLowerInvariant()))
            .WithMessage($"SourceType must be one of: {string.Join(", ", ValidSourceTypes)}.");

        When(x => x.SourceType?.ToLowerInvariant() == "custom_sql", () =>
        {
            RuleFor(x => x.CustomSql)
                .NotEmpty().WithMessage("CustomSql is required when SourceType is 'custom_sql'.");
        });

        When(x => x.SourceType?.ToLowerInvariant() != "custom_sql", () =>
        {
            RuleFor(x => x.TableName)
                .NotEmpty().WithMessage("TableName is required when SourceType is not 'custom_sql'.");
        });
    }
}
