
# 🔐 CloudZCrypt

## Your Personal File Vault for Windows

<p align="center">  
<img alt=".NET" src="https://img.shields.io/badge/.NET-9-512BD4?logo=dotnet&logoColor=white" />  
<img alt="C#" src="https://img.shields.io/badge/C%23-13-239120?logo=csharp&logoColor=white" />  
<img alt="WPF" src="https://img.shields.io/badge/WPF-Desktop-5C2D91?logo=windows&logoColor=white" />  
<img alt="Windows" src="https://img.shields.io/badge/Windows-10%2B-0078D6?logo=windows&logoColor=white" />  
<img alt="PRs Welcome" src="https://img.shields.io/badge/PRs-welcome-brightgreen.svg" />  
</p>

---

**CloudZCrypt is a simple, powerful tool that helps you protect your sensitive files with military-grade encryption.**

## 📋 What CloudZCrypt Does For You

CloudZCrypt gives you privacy and security in a few simple clicks:

- **🛡️ Protect Sensitive Documents** - Encrypt financial records, personal photos, medical information, and more
- **☁️ Secure Cloud Storage** - Safely store encrypted files on any cloud like Dropbox, Google Drive, or OneDrive
- **🔒 Control Your Privacy** - Keep your data private, even when sharing devices or storage
- **✅ Peace of Mind** - Industry-standard encryption means your files stay private until you decide otherwise

## ❓ Why Choose CloudZCrypt?

- **🖱️ Simple Interface** - No cryptography knowledge needed - just select files, choose a password, and encrypt
- **🏦 Military-Grade Security** - Uses the same encryption standards trusted by financial institutions
- **🔌 No Internet Required** - Works completely offline, keeping your sensitive data off the network
- **🛠️ Multiple Security Options** - Choose from multiple proven encryption methods to match your needs
- **💯 Completely Free** - Open-source and free to use, forever

## ⭐ Features for Privacy Enthusiasts

- **🔄 Multiple Encryption Algorithms** - AES, ChaCha20, Twofish, Serpent, and Camellia
- **🔑 Multiple Key Derivation Algorithms** - Argon2id and PBKDF2.
- **📊 Password Strength Guidance** - Built-in analyzer helps you create truly secure passwords
- **💻 Local Processing Only** - Your files and passwords never leave your computer
- **👁️ Zero Data Collection** - We don't track, collect, or transmit any of your information

## 📘 How to Use CloudZCrypt

**To Encrypt:**
1. Select the file(s) or folder you want to encrypt
2. Choose where to save the encrypted files
3. Select your preferred encryption settings.
4. Create a strong password (and save it somewhere safe!)
5. Click "Encrypt" and wait for the process to complete

**To Decrypt:**
1. Select the encrypted file(s)
2. Choose where to save the decrypted files
3. Select the same encryption settings you used previously
4. Enter the password you used to encrypt
5. Click "Decrypt" and wait for completion

**Important:** ⚠️ There is no password recovery. If you forget your password, your encrypted files cannot be decrypted.

## 🚀 Roadmap

CloudZCrypt is constantly evolving. Here's what we're planning for future releases:

- **🐧 Linux Support** - We're working to make CloudZCrypt available for Linux users
- **🎨 Enhanced User Interface** - Upcoming UI improvements for better usability and aesthetics
- **🔐 Additional Encryption Algorithms** - Expanding our cryptography options with more advanced algorithms
- **⚙️ Advanced Parameter Configuration** - Expert mode allowing customization of encryption parameters for advanced users
- **👥 Community-Driven Development** - We highly value community suggestions and contributions to guide the project's future

We're committed to continuously improving CloudZCrypt based on user feedback and security best practices. Your suggestions are always welcome and will help shape the application's future.

## 🧩 About the Name

CloudZCrypt originated from its initial purpose: creating a tool that would encrypt files securely for cloud storage services. The name combines "Cloud" (representing cloud storage), "Z" (as a stylistic element), and "Crypt" (for encryption). While the application was originally designed for securing files before uploading them to any cloud storage service, it works equally well for encrypting files stored locally on your device, offering versatile protection regardless of where your files ultimately reside.

## 👨‍💻 For Developers

CloudZCrypt welcomes developer contributions! The project is built on modern technologies and follows clean architecture principles:

- Modern .NET 9 and C# 13
- Clean, layered architecture with separation of concerns
- Well-documented code with clear patterns
- Designed for easy extension with new algorithms

### Architecture Overview

CloudZCrypt uses a clean, modular architecture with these key components:

- **Presentation:** WPF interface with MVVM pattern
- **Application:** Orchestration layer connecting UI to core functionality
- **Domain:** Core business logic and encryption strategy interfaces
- **Infrastructure:** Concrete implementations of encryption algorithms

The codebase emphasizes:
- Strategy pattern for pluggable encryption algorithms
- Factory pattern for runtime algorithm resolution
- Dependency injection for testability
- Clear separation of concerns

### How to Contribute

To contribute:
1. Fork the repository
2. Create a feature branch from `develop`
3. Implement your changes with tests
4. Submit a pull request

We especially welcome contributions for UI and security improvements.

## 🔍 Security Notes

- 🔑 Your security depends on your password strength - use long, complex passwords
- 🔄 Keep your operating system and CloudZCrypt updated

---

## 📝 About This Project

CloudZCrypt is maintained with care for privacy and simplicity. It's free, open-source software designed to put you in control of your private information.

**Questions or suggestions?** Open an issue on our GitHub page.

*Your privacy matters. Your files belong to you alone.*
```