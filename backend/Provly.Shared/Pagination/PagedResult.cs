namespace Provly.Shared.Pagination;

public class PagedResult<T>(IList<T> data, int pageNumber, int pageSize, long totalRecords)
{
    public IList<T> Data { get; set; } = data;
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public long TotalRecords { get; set; } = totalRecords;
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
}