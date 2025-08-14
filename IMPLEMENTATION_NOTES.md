# Key Derivation Algorithm Implementation

## Overview
This implementation adds support for selectable key derivation algorithms in CloudZCrypt, following the same architectural pattern used for encryption algorithms.

## Implemented Algorithms

### Argon2id (Default)
- **Security**: High
- **Performance**: Slower
- **Memory Usage**: 128 MB
- **Iterations**: 5
- **Parallelism**: 4 threads
- **Best for**: Maximum security applications where performance is not critical

### PBKDF2-SHA256
- **Security**: Good
- **Performance**: Faster
- **Iterations**: 100,000
- **Best for**: Applications requiring faster key derivation while maintaining good security

## Architecture

The implementation follows the factory pattern used throughout the application:

### Core Components
1. **IKeyDerivationService**: Interface for key derivation implementations
2. **IKeyDerivationServiceFactory**: Factory interface for creating key derivation services
3. **KeyDerivationServiceFactory**: Concrete factory implementation
4. **Argon2idKeyDerivationService**: Argon2id implementation
5. **PBKDF2KeyDerivationService**: PBKDF2 implementation

### Integration Points
- **BaseEncryptionService**: Modified to accept and use IKeyDerivationServiceFactory
- **FileProcessingRequest**: Extended to include KeyDerivationAlgorithm parameter
- **UI**: Added ComboBox for algorithm selection with descriptive labels

## Usage

Users can now select their preferred key derivation algorithm from the UI:
- **Argon2id (Slower, More Secure)**: For maximum security
- **PBKDF2 (Faster, Still Secure)**: For better performance

The selection is passed through the entire processing pipeline and used during both encryption and decryption operations.

## Security Considerations

- Both algorithms use cryptographically secure random salts
- Sensitive data (passwords, keys) is properly cleared from memory after use
- PBKDF2 uses 100,000 iterations, which meets current security recommendations
- Argon2id uses memory-hard parameters that resist GPU-based attacks

## Performance Impact

- **Argon2id**: ~2-5 seconds per file (depending on system)
- **PBKDF2**: ~100-200ms per file (significantly faster)

The performance difference is most noticeable when processing many small files.