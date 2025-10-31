using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Infrastructure.Factories;

internal class NameObfuscationServiceFactory(IEnumerable<INameObfuscationStrategy> strategies)
    : INameObfuscationServiceFactory
{
    private readonly IReadOnlyDictionary<NameObfuscationMode, INameObfuscationStrategy> strategies
        = strategies.ToDictionary(s => s.Id, s => s);

    public INameObfuscationStrategy Create(NameObfuscationMode obfuscationMode)
    {
        return !strategies.TryGetValue(obfuscationMode, out INameObfuscationStrategy? strategy)
            ? throw new ArgumentOutOfRangeException(nameof(obfuscationMode), $"Name obfuscation mode '{obfuscationMode}' is not registered.")
            : strategy;
    }
}