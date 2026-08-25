namespace HighMetroServer.Models;

public class DataBaseQueryPage(int pageSize, int currPage)
{
    public int PageSize { get; init; } = pageSize;
    public int CurrentPage { get; init; } = currPage;
}