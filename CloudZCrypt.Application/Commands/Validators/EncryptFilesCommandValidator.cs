using FluentValidation;

namespace CloudZCrypt.Application.Commands.Validators;
public class EncryptFilesCommandValidator : AbstractValidator<EncryptFilesCommand>
{
    public EncryptFilesCommandValidator()
    {
        RuleFor(x => x.SourceDirectory)
            .NotEmpty()
            .WithMessage("Source directory is required")
            .Must(Directory.Exists)
            .WithMessage("Source directory must exist");

        RuleFor(x => x.DestinationDirectory)
            .NotEmpty()
            .WithMessage("Destination directory is required");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long");

        RuleFor(x => x.EncryptionAlgorithm)
            .IsInEnum()
            .WithMessage("Invalid encryption algorithm");

        RuleFor(x => x.KeyDerivationAlgorithm)
            .IsInEnum()
            .WithMessage("Invalid key derivation algorithm");

        RuleFor(x => x.EncryptOperation)
            .IsInEnum()
            .WithMessage("Invalid encrypt operation");
    }
}
