namespace LeeterviewBackend.DTOs
{
    public class ArticleSearchCriteria
    {
        public int? Id { get; set; }
        public string? Category { get; set; }
        public int? UserId { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public string? TitleKeyword { get; set; }
        public string? SortBy { get; set; } 
        public bool IsDescending { get; set; } = true; 
        public int PageNumber { get; set; } = 1; 
        public int PageSize { get; set; } = 20; 
    }
}
