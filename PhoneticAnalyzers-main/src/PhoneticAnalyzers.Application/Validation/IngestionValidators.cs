using FluentValidation;
using PhoneticAnalyzers.Application.Commands.Ingestion;

namespace PhoneticAnalyzers.Application.Validation;

/// <summary>
/// Validator for <see cref="IngestPersonCommand"/>
/// </summary>
public sealed class IngestPersonCommandValidator : AbstractValidator<IngestPersonCommand>
{
    /// <summary>
    /// Creates a new validator for <see cref="IngestPersonCommand"/>
    /// </summary>
    public IngestPersonCommandValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("External ID is required")
            .MaximumLength(64).WithMessage("External ID cannot exceed 64 characters");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters");

        RuleFor(x => x.SourceSystem)
            .MaximumLength(50).WithMessage("Source system cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.SourceSystem));
    }
}

/// <summary>
/// Validator for <see cref="IngestPersonBatchCommand"/>
/// </summary>
public sealed class IngestPersonBatchCommandValidator : AbstractValidator<IngestPersonBatchCommand>
{
    /// <summary>
    /// Creates a new validator for <see cref="IngestPersonBatchCommand"/>
    /// </summary>
    public IngestPersonBatchCommandValidator()
    {
        RuleFor(x => x.Persons)
            .NotEmpty().WithMessage("Batch must contain at least one person")
            .Must(p => p.Count <= 1000).WithMessage("Batch size cannot exceed 1000 items");

        RuleFor(x => x.BatchSize)
            .GreaterThan(0).WithMessage("Batch size must be greater than zero")
            .LessThanOrEqualTo(100).WithMessage("Batch size cannot exceed 100");

        RuleForEach(x => x.Persons).SetValidator(new PersonBatchItemValidator());
    }
}

/// <summary>
/// Validator for items in <see cref="IngestPersonBatchCommand"/>
/// </summary>
public sealed class PersonBatchItemValidator : AbstractValidator<PersonBatchItem>
{
    /// <summary>
    /// Creates a new validator for <see cref="PersonBatchItem"/>
    /// </summary>
    public PersonBatchItemValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("External ID is required")
            .MaximumLength(64).WithMessage("External ID cannot exceed 64 characters");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters");
    }
}
