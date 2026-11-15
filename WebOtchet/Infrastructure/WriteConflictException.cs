namespace WebOtchet.Infrastructure;

public sealed class WriteConflictException(string message) : Exception(message);
