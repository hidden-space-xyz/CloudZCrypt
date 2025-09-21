using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.WPF.ViewModels;

public sealed class KeyDerivationAlgorithmViewModel
{
    public required KeyDerivationAlgorithm Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string Summary { get; init; }

    public static KeyDerivationAlgorithmViewModel FromStrategy(
        IKeyDerivationAlgorithmStrategy strategy
    )
    {
        return new()
        {
            Id = strategy.Id,
            DisplayName = strategy.DisplayName,
            Description = strategy.Description,
            Summary = strategy.Summary,
        };
    }

    public override string ToString()
    {
        return DisplayName;
    }
}
