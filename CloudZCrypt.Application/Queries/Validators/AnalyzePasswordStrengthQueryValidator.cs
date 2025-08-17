using FluentValidation;

namespace CloudZCrypt.Application.Queries.Validators;
public class AnalyzePasswordStrengthQueryValidator : AbstractValidator<AnalyzePasswordStrengthQuery>
{
    public AnalyzePasswordStrengthQueryValidator()
    {
        RuleFor(x => x.Password)
            .NotNull()
            .WithMessage("Password cannot be null")
            .NotEmpty()
            .WithMessage("Password cannot be empty");
    }
}
