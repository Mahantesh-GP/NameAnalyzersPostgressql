using FluentValidation;
using PhoneticAnalyzers.SQLDBFirst.Application.Commands;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Validators;

/// <summary>
/// Validator for IngestPersonCommand.
/// </summary>
public class IngestPersonCommandValidator : AbstractValidator<IngestPersonCommand>
{
    public IngestPersonCommandValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("ExternalId is required")
            .MaximumLength(100).WithMessage("ExternalId must not exceed 100 characters");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required")
            .MaximumLength(200).WithMessage("FullName must not exceed 200 characters")
            .Must(ContainAtLeastTwoNames).WithMessage("FullName must contain at least two names (first and last)");

        RuleFor(x => x.County)
            .MaximumLength(50).WithMessage("County must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.County));
    }

    private bool ContainAtLeastTwoNames(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return false;

        var names = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return names.Length >= 2;
    }
}
