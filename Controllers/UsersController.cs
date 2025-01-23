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

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, existingUser.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
    }
}
