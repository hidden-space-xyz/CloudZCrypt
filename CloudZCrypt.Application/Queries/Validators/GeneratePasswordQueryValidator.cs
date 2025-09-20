using FluentValidation;

namespace CloudZCrypt.Application.Queries.Validators;
public class GeneratePasswordQueryValidator : AbstractValidator<GeneratePasswordQuery>
{
    public GeneratePasswordQueryValidator()
    {
        RuleFor(x => x.Length)
            .GreaterThan(0)
            .WithMessage("Password length must be greater than 0")
            .LessThanOrEqualTo(512)
            .WithMessage("Password length must be 512 characters or less");

        RuleFor(x => x)
            .Must(x => x.IncludeUppercase || x.IncludeLowercase || x.IncludeNumbers || x.IncludeSpecialCharacters)
            .WithMessage("At least one character type must be included");
    }
}
