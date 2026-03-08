using System.ComponentModel.DataAnnotations;

namespace Provly.Shared.Pagination;

public class PaginationParameters
{
    private int _pageSize = 10;
    private const int MaxPageSize = 500;
    
    [Range(1, int.MaxValue, ErrorMessage = "The 'page' must be greater than 0.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 500, ErrorMessage = "The 'pageSize' must be between 1 and 500.")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }
}