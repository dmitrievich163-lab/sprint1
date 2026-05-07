namespace AspNetCoreApi
{
    public class PaginatedResult<T>
    {
     
        public int TotalCount { get; set; }

     
        public List<T> Items { get; set; } = new List<T>();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }
    }
}
