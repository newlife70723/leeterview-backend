using BCrypt.Net;  
using LeeterviewBackend.Data;  
using LeeterviewBackend.Models;  
using Microsoft.AspNetCore.Mvc;  
using Microsoft.EntityFrameworkCore; 
using LeeterviewBackend.DTOs;  
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace LeeterviewBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            if (user == null)
            {
                var errorResponse = new ApiResponse<object>
                {
                    Status = 400,
                    Message = "Invalid data provided",
                    Error = new { Code = "INVALID_DATA", Details = "The provided user data is null or invalid." }
                };
                return BadRequest(errorResponse);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == user.Username);

            if (existingUser == null)
            {
                var errorResponse = new ApiResponse<object>
                {
                    Status = 409,
                    Message = "User doesn't exist",
                    Error = new { Code = "USER_NOT_FOUND", Details = "The user doesn't exist." }
                };
                return Unauthorized(errorResponse);
            }

            var checkPassword = BCrypt.Net.BCrypt.Verify(user.Password, existingUser.Password);

            if (!checkPassword)
            {
                var errorResponse = new ApiResponse<object>
                {
                    Status = 401,
                    Message = "Password error",
                    Error = new { Code = "PASSWORD_ERROR", Details = "Incorrect password." }
                };
                return Unauthorized(errorResponse);
            }

            var userId = existingUser.Id;

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, existingUser.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", userId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("leeterviewApiSuperLongKey1234567890123456"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "Leeterview",
                audience: "Leeterview API",
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            var successResponse = new ApiResponse<object>
            {
                Status = 200,
                Message = "Login Success",
                Data = new { Code = "LOGIN_SUCCESS", Details = "Login success!", Token = jwtToken }
            };

            return Ok(successResponse);
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (user == null)
            {
                var errorResponse = new ApiResponse<object>
                {
                    Status = 400,
                    Message = "Invalid data provided",
                    Error = new { Code = "INVALID_DATA", Details = "The provided user data is null or invalid." }
                };
                return BadRequest(errorResponse);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == user.Username || u.Email == user.Email);

            if (existingUser != null)
            {
                var errorResponse = new ApiResponse<object>
                {
                    Status = 409,  // Conflict error for username duplication
                    Message = "Username already exists",
                    Error = new { Code = "USERNAME_EXISTS", Details = $"The username '{user.Username}' is already taken." }
                };
                return Conflict(errorResponse);
            }

            // Hash password
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var successResponse = new ApiResponse<object>
            {
                Status = 200,
                Message = "User registered successfully",
                Data = new { userId = user.Id, username = user.Username }
            };

            return Ok(successResponse);
        }

        [Authorize]
        [HttpPut("UpdateAvatar")]
        public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest request)
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

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Status = 404,
                    Message = "User not found",
                    Data = null,
                    Error = new { Code = "USER_NOT_FOUND", Details = "No user matches the provided ID" }
                });
            }

            user.AvatarUrl = request.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Status = 200,
                    Message = "Avatar updated successfully",
                    Data = new { Code = "AVATAR_UPDATED", Details = "User avatar updated successfully" },
                    Error = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "An error occurred while updating the avatar",
                    Data = null,
                    Error = new { Code = "SERVER_ERROR", Details = ex.Message }
                });
            }
        }

        [Authorize]
        [HttpPut("UpdateUserProfile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileRequest request)
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

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Status = 404,
                    Message = "User not found",
                    Data = null,
                    Error = new { Code = "USER_NOT_FOUND", Details = "No user matches the provided ID" }
                });
            }

            // 更新用戶資料（不包括 Username）
            if (!string.IsNullOrEmpty(request.Email))
            {
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.Bio))
            {
                user.Bio = request.Bio;
            }

            if (!string.IsNullOrEmpty(request.Location))
            {
                user.Location = request.Location;
            }

            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<object>
                {
                    Status = 200,
                    Message = "User profile updated successfully",
                    Data = new
                    {
                        Code = "PROFILE_UPDATED",
                        Details = "User profile updated successfully",
                        User = new
                        {
                            user.Email,
                            user.Bio,
                            user.Location,
                            user.UpdatedAt
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
                    Message = "Failed to update user profile",
                    Data = null,
                    Error = new { Code = "UPDATE_FAILED", Details = ex.Message }
                });
            }
        }


        [Authorize]
        [HttpGet("GetUserProfile")]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                // 從 Token 中解析出用戶 ID
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

                // 從資料庫中查詢用戶
                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Status = 404,
                        Message = "User not found",
                        Data = null,
                        Error = new { Code = "USER_NOT_FOUND", Details = "No user matches the provided ID" }
                    });
                }

                // 返回用戶基本信息和統計數據
                var userProfile = new
                {
                    user.AvatarUrl,
                    user.Email,
                    user.Location,
                    user.Bio,
                    TotalPosts = await _context.Articles.CountAsync(a => a.UserId == user.Id),
                    TotalLikes = await _context.Articles
                        .Where(a => a.UserId == user.Id)
                        .SumAsync(a => a.Like)
                };

                return Ok(new ApiResponse<object>
                {
                    Status = 200,
                    Message = "User profile retrieved successfully",
                    Data = new
                    {
                        Code = "USER_PROFILE_RETRIEVED",
                        Details = "User profile retrieved successfully",
                        Profile = userProfile
                    },
                    Error = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "Failed to retrieve user profile",
                    Data = null,
                    Error = new { Code = "SERVER_ERROR", Details = ex.Message }
                });
            }
        }


    }
}
