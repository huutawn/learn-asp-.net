namespace IdentityService.Api.Exceptions;

public sealed class ConflictException(string message)
    : Exception(message);