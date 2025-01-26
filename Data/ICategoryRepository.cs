namespace LeeterviewBackend.Data
{
    public interface ICategoryRepository
    {
        Task<List<string>> GetCategoriesAsync();
    }
}