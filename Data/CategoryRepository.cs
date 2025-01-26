using StackExchange.Redis;
using LeeterviewBackend.Data;
using LeeterviewBackend.Models;
using Microsoft.EntityFrameworkCore;

public class CategoryRepository : ICategoryRepository
{
    private readonly IDatabase _redis;
    private readonly ApplicationDbContext _context;

    public CategoryRepository(IConnectionMultiplexer redis, ApplicationDbContext context)
    {
        _redis = redis.GetDatabase();
        _context = context;
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        var redisKey = "article_labels";

        var cachedCategories = await _redis.ListRangeAsync(redisKey);
        if (cachedCategories.Length > 0)
        {
            return cachedCategories.Select(category => category.ToString()).ToList();
        }

        var categoriesFromDb = await _context.ArticleLabels
            .Select(label => label.Label)
            .ToListAsync();

        if (categoriesFromDb.Any())
        {
            await _redis.ListRightPushAsync(redisKey, categoriesFromDb.Select(category => (RedisValue)category).ToArray());
            await _redis.KeyExpireAsync(redisKey, TimeSpan.FromHours(1));
        }

        return categoriesFromDb;
    }
}
