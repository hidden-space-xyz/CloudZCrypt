using FluentValidation;

namespace CloudZCrypt.Application.Commands.Validators;

/// <summary>
/// Validator for the GeneratePasswordQuery
/// </summary>
public class GeneratePasswordQueryValidator : AbstractValidator<GeneratePasswordCommand>
{
    public GeneratePasswordQueryValidator()
    {
        RuleFor(x => x.Length)
            .GreaterThan(0)
            .WithMessage("Password length must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Password length must be 1000 characters or less");

        RuleFor(x => x)
            .Must(x => x.IncludeUppercase || x.IncludeLowercase || x.IncludeNumbers || x.IncludeSpecialCharacters)
            .WithMessage("At least one character type must be included");
    }
}