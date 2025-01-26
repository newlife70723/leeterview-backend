using System.ComponentModel.DataAnnotations;

namespace LeeterviewBackend.DTOs
{
    public class ArticleRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        public required string Title { get; set; }
        
        [Required(ErrorMessage = "Category is required.")]
        public required string Category { get; set; }
        
        [Required(ErrorMessage = "Content is required.")]
        public required string Content { get; set; }
    }
}
