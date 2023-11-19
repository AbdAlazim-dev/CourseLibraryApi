using Microsoft.EntityFrameworkCore;

namespace CourseLibrary.API.Services;

public class PagedList<T> : List<T> 
{
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public int PageSize { get; private set; }
    public int TotalCount { get; private set; }
    public bool HasPrevius => (CurrentPage > 1);
    public bool HasNext => (CurrentPage < TotalPages); 

    //constructer to set values that can be logicly created
    public PagedList(List<T> items ,int count, int pageNumber, int pageSize)
    {
        CurrentPage = pageNumber;
        TotalCount = count;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        AddRange(items);
    }
    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> collection, int pageNumber, int pageSize)
    {
        var count = collection.Count();
        var items = await collection
            .Skip((pageNumber - 1) * pageSize )
            .Take(pageSize).ToListAsync();

        return new PagedList<T>(items, count, pageNumber, pageSize); 
    }



    
}
