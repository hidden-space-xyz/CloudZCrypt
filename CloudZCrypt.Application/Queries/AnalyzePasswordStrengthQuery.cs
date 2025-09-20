using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using MediatR;

namespace CloudZCrypt.Application.Queries;

// Refactored to primary-constructor record
public sealed record AnalyzePasswordStrengthQuery(string Password) : IRequest<Result<PasswordStrengthResult>>;
