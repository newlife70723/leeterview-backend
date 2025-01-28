using LeeterviewBackend.DTOs;  
using LeeterviewBackend.Data;  
using LeeterviewBackend.Models;
using Microsoft.AspNetCore.Mvc; 
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authorization;


namespace LeeterviewBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase 
    {
        private readonly ApplicationDbContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly ICategoryRepository _categoryRepository;

        public ArticlesController(ApplicationDbContext context, IConnectionMultiplexer redis, ICategoryRepository categoryRepository) 
        {
            _context = context;
            _redis = redis;
            _categoryRepository = categoryRepository;
        }

        [HttpGet("GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
             var categories = await _categoryRepository.GetCategoriesAsync();

            if (categories == null || !categories.Any())
            {
                return NotFound(new ApiResponse<object>
                {
                    Status = 404,
                    Message = "No categories found",
                    Data = null,
                    Error = new { Code = "NO_CATEGORIES_FOUND", Details = "No categories found" }
                });
            }

            return Ok(new ApiResponse<object>
            {
                Status = 200,
                Message = "Categories retrieved successfully",
                Data = new { Code = "GET_CATEGORY_SUCCESS", Details = "Categories retrieved successfully", Categories = categories },
                Error = null
            });
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

        [Authorize]
        [HttpPost("CreateNewArticle")]
        public async Task<IActionResult> CreateNewArticle([FromBody] ArticleRequest request)
        {
            var userIdString = User.FindFirst("userId")?.Value;
            
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Status = 401,
                    Message = "Unauthorized: Invalid token",
                    Data = null,
                    Error = new { Code = "INVALID_TOKEN", Details = "Authorization failed" }
                });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<object>
                {
                    Status = 400,
                    Message = string.Join(", ", errors),
                    Data = null,
                    Error = new { Code = "VALIDATION_FAILED", Details = "Invalid input data" }
                });
            }

            var validCategories = await _categoryRepository.GetCategoriesAsync();
            if (!validCategories.Contains(request.Category))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Status = 400,
                    Message = "Invalid category",
                    Data = null,
                    Error = new { Code = "INVALID_CATEGORY", Details = "The provided category is not valid" }
                });
            }

            var newArticle = new Article
            {
                UserId = userId,
                Title = request.Title,
                Category = request.Category,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Like = 0
            };

            await _context.Articles.AddAsync(newArticle);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Status = 200,
                Message = "Article created successfully",
                Data = new { Code = "ARTICLE_CREATED_SUCCESSFULLY", Details = "Article created successfully", Data = newArticle },
                Error = null
            });
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetArticleById(int postId)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;    
            int? currentUserId = string.IsNullOrEmpty(userIdClaim) ? null : int.Parse(userIdClaim);

            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == postId);

            if (article == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Status = 404,
                    Message = "Article not found",
                    Data = null,
                    Error = new { Code = "ARTICLE_NOT_FOUND", Details = $"No article found with ID: {postId}" }
                });
            }

            bool isEditable = currentUserId.HasValue && article.UserId == currentUserId.Value;

            return Ok(new ApiResponse<object>
            {
                Status = 200,
                Message = "Article retrieved successfully",
                Data = new 
                {
                    Code = "ARTICLE_RETRIEVE_SUCCESS",
                    Details = "Article retrieved successfully",
                    Article = new
                    {
                        article.Id,
                        article.Title,
                        article.Category,
                        article.Content,
                        article.CreatedAt,
                        article.Like,
                        Editable = isEditable
                    }
                },
                Error = null
            });
        }

        [HttpGet("GetPosts")]
        public async Task<IActionResult> GetPosts([FromQuery] ArticleSearchCriteria criteria)
        {
            try
            {
                IQueryable<Article> query = _context.Articles;

                if (!string.IsNullOrEmpty(criteria.Category))
                {
                    query = query.Where(a => a.Category.ToLower() == criteria.Category.ToLower());
                }

                if (criteria.UserId.HasValue)
                {
                    query = query.Where(a => a.UserId == criteria.UserId.Value);
                }

                if (criteria.CreatedAfter.HasValue)
                {
                    query = query.Where(a => a.CreatedAt > criteria.CreatedAfter.Value);
                }

                if (!string.IsNullOrEmpty(criteria.TitleKeyword))
                {
                    query = query.Where(a => a.Title.Contains(criteria.TitleKeyword));
                }

                if (!string.IsNullOrEmpty(criteria.SortBy))
                {
                    query = criteria.SortBy.ToLower() switch
                    {
                        "like" => criteria.IsDescending ? query.OrderByDescending(a => a.Like) : query.OrderBy(a => a.Like),
                        "date" => criteria.IsDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
                        _ => query
                    };
                }

                int totalRecords = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalRecords / (double)criteria.PageSize);

                query = query
                    .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                    .Take(criteria.PageSize);

                var articles = await query
                    .Select(a => new
                    {
                        a.Id,
                        a.Title,
                        a.Category,
                        a.CreatedAt,
                        a.Like
                    })
                    .ToListAsync();

                if (!articles.Any())
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Status = 404,
                        Message = "No articles found.",
                        Data = null,
                        Error = new { Code = "NO_ARTICLES_FOUND", Details = "No articles match the criteria." }
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Status = 200,
                    Message = "Articles retrieved successfully.",
                    Data = new
                    {
                        Code = "ARTICLES_RETRIEVED_SUCCESS",
                        Details = "Articles retrieved successfully.",
                        Articles = articles,
                        Pagination = new
                        {
                            CurrentPage = criteria.PageNumber,
                            PageSize = criteria.PageSize,
                            TotalPages = totalPages,
                            TotalRecords = totalRecords
                        }
                    },
                    Error = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "An error occurred while retrieving articles.",
                    Data = null,
                    Error = new { Code = "SERVER_ERROR", Details = ex.Message }
                });
            }
        }


        [Authorize]
        [HttpPut("EditPost")]
        public async Task<IActionResult> EditPost([FromBody] UpdateArticleRequest request)
        {
            var userId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Status = 401,
                    Message = "Unauthorized: Invalid token",
                    Data = null,
                    Error = new { Code = "INVALID_TOKEN", Details = "Authorization failed" }
                });
            }

            var userIdInt = int.Parse(userId);
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == request.Id);

            if (article == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Status = 404,
                    Message = "Article not found",
                    Data = null,
                    Error = new { Code = "ARTICLE_NOT_FOUND", Details = "Article not found" }
                });
            }

            if (article.UserId != userIdInt)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Status = 403,
                    Message = "You are not authorized to edit this article",
                    Data = null,
                    Error = new { Code = "FORBIDDEN", Details = "Unauthorized access to edit this article" }
                });
            }

            if (!string.IsNullOrEmpty(request.Title))
            {
                article.Title = request.Title;
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                article.Category = request.Category;
            }

            if (!string.IsNullOrEmpty(request.Content))
            {
                article.Content = request.Content;
            }

            article.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Status = 200,
                Message = "Edit article successfully",
                Data = new
                {
                    Code = "EDIT_ARTICLE_SUCCESS",
                    Details = "Article updated successfully",
                    Article = article
                },
                Error = null
            });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var userIdString = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Status = 401,
                    Message = "Unauthorized: Invalid token",
                    Data = null,
                    Error = new { Code = "INVALID_TOKEN", Details = "Authorization failed" }
                });
            };

            var userId = int.Parse(userIdString);

            var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (article == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Status = 404,
                    Message = "Article not found",
                    Data = null,
                    Error = new { Code = "ARTICLE_NOT_FOUND", Details = "Article not found" }
                });
            }

            if (article.UserId != userId)
            {
                return BadRequest(new ApiResponse<object>
                {
                      Status = 403,
                    Message = "You are not authorized to delete this article",
                    Data = null,
                    Error = new { Code = "FORBIDDEN", Details = "Unauthorized access to delete this article" }
                });
            }

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Status = 200,
                Message = "Article deleted successfully",
                Data = new { Code = "ARTICLE_DELETED_SUCCESS", Details = "The article has been deleted." },
                Error = null
            });
        }
    }
}