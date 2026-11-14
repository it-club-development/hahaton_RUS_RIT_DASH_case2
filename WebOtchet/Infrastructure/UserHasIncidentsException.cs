namespace WebOtchet.Infrastructure;

public sealed class UserHasIncidentsException(Exception innerException)
    : Exception("Нельзя удалить пользователя, пока с ним связаны аварии.", innerException)
{
}
