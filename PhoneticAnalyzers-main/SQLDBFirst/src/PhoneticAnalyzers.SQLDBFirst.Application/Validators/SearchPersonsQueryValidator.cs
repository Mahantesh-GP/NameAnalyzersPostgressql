using FluentValidation;
using PhoneticAnalyzers.SQLDBFirst.Application.Queries;

namespace PhoneticAnalyzers.SQLDBFirst.Application.Validators;

/// <summary>
/// Validator for SearchPersonsQuery.
/// </summary>
public class SearchPersonsQueryValidator : AbstractValidator<SearchPersonsQuery>
{
    public SearchPersonsQueryValidator()
    {
        RuleFor(x => x.SearchName)
            .NotEmpty().WithMessage("SearchName is required")
            .MaximumLength(200).WithMessage("SearchName must not exceed 200 characters");

        RuleFor(x => x.MinSimilarity)
            .InclusiveBetween(0.0, 1.0).WithMessage("MinSimilarity must be between 0.0 and 1.0");
    }
}
