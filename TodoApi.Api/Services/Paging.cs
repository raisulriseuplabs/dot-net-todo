namespace TodoApi.Api.Services;

public static class Paging
{
    public const int MaxPageSize = 100;

    /// <summary>Out-of-range paging values are corrected rather than rejected: page ≥ 1, 1 ≤ pageSize ≤ 100.</summary>
    public static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));
}
