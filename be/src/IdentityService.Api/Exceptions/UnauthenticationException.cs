namespace IdentityService.Api.Exceptions;

public sealed class UnauthenticationException(string message)
    : Exception(message);