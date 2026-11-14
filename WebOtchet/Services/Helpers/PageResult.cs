namespace WebOtchet.Services.Helpers;

public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages =>
        (int)Math.Ceiling((double)TotalCount / PageSize);
}
