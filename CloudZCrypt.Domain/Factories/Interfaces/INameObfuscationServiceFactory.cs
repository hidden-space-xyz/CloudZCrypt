using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Domain.Factories.Interfaces;

public interface INameObfuscationServiceFactory
{
    INameObfuscationStrategy Create(NameObfuscationMode obfuscationMode);
}
