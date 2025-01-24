using LeeterviewBackend.DTOs;  
using LeeterviewBackend.Data;  
using LeeterviewBackend.Models;
using Microsoft.AspNetCore.Mvc; 
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace LeeterviewBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase 
    {
        private readonly ApplicationDbContext _context;
        private readonly IConnectionMultiplexer _redis;

        public ArticlesController(ApplicationDbContext context, IConnectionMultiplexer redis) 
        {
            _context = context;
            _redis = redis;
        }

        [HttpGet("GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            var db = _redis.GetDatabase();
            var redisKey = "article_labels";

            var cachedLabels = await db.ListRangeAsync(redisKey);

            if (cachedLabels.Length > 0)
            {
                // Redis 中有資料，直接返回 Redis 中的資料
                var labels = cachedLabels.Select(label => label.ToString()).ToList();

                var redisSuccessResponse = new ApiResponse<object>
                {
                    Status = 200,
                    Message = "Get labels success from Redis",
                    Data = new { Code = "GET_LABELS_SUCCESS", Details = "Get labels success from Redis", Data = labels },
                    Error = null,
                };

                return Ok(redisSuccessResponse);
            }

            var labelsFromDb = await _context.ArticleLabels
                                .Select(label => label.Label)
                                .ToListAsync();

            if (labelsFromDb == null || !labelsFromDb.Any())
            {
                var response = new ApiResponse<object>
                {
                    Status = 404,
                    Message = "No labels found",
                    Data = null,
                    Error = new { Code = "NO_LABELS_FOUND", Details = "No labels found" }
                };
                return NotFound(response);
            }

            // save in Redis
            await db.ListRightPushAsync(redisKey, labelsFromDb.Select(label => (RedisValue)label).ToArray());

            var successResponse = new ApiResponse<object>
            {
                Status = 200,
                Message = "Get labels success",
                Data = new { Code = "GET_LABELS_SUCCESS", Details = "Get labels success", Labels = labelsFromDb },
                Error = null,
            };

            return Ok(successResponse); 
        }

        [HttpPost("CreateNewLabel")]
        public async Task<IActionResult> CreateNewLabel([FromBody] LabelRequest request)
        {
            if (string.IsNullOrEmpty(request.Label))
            {
                var response = new ApiResponse<object>
                {
                    Status = 409,
                    Message = "Miss Label Name",
                    Data = null,
                    Error = new { Code = "MISS_LABEL_NAME", Details = "Miss Label Name" }
                };
                return BadRequest(response);
            }

            var db = _redis.GetDatabase();
            var redisKey = "article_labels";

            // check redis
            var existingLabels = await db.ListRangeAsync(redisKey);
            if (existingLabels.Any(label => label.ToString() == request.Label))
            {
                return Conflict(new ApiResponse<object>
                {
                    Status = 409,
                    Message = "Label already exists",
                    Data = null,
                    Error = new { Code = "LABEL_EXISTS", Details = "Label already exists in Redis" }
                });
            }

            // check db
            var existingLabelInDb = await _context.ArticleLabels
                .Where(label => label.Label == request.Label)
                .FirstOrDefaultAsync();

            if (existingLabelInDb != null)
            {
                return Conflict(new ApiResponse<object>
                {
                    Status = 409,
                    Message = "Label already exists",
                    Data = null,
                    Error = new { Code = "LABEL_EXISTS", Details = "Label already exists in database" }
                });
            }

            // save in database
            var newLabel = new ArticleLabel { Label = request.Label };
            _context.ArticleLabels.Add(newLabel);
            await _context.SaveChangesAsync();

            // update redis
            await db.ListRightPushAsync(redisKey, request.Label);

            var successResponse = new ApiResponse<object>
            {
                Status = 200,
                Message = "Create label success",
                Data = new { Code = "CREATE_LABEL_SUCCESS", Details = "Create label success" },
            };
            return Ok(successResponse);
        }
    }

}