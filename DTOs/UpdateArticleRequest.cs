namespace LeeterviewBackend.DTOs
{
    public class UpdateArticleRequest
    {
        public required int Id { get; set; }
        public required string Title { get; set; }
        public required string Category { get; set; } = "other";
        public required string Content { get; set; }
    }
}
