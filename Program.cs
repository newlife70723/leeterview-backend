using LeeterviewBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StackExchange.Redis;
using LeeterviewBackend.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// load appsettings.json 
var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Redis
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
var redis = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// S3 service
builder.Services.AddSingleton<S3Service>();

// JWT
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("JWT_SECRET_KEY is not set.");
}

var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, 
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtKey,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = "Leeterview",
            ValidAudience = "Leeterview API"
        };
    });

builder.Services.AddAuthorization();

// load database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Logging.AddConsole();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// change cors setting
var corsAllowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? "*";
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        if (corsAllowedOrigins == "*")
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            var origins = corsAllowedOrigins.Split(';', StringSplitOptions.RemoveEmptyEntries);
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});


// build application
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // check database exist
        var databaseCreator = context.Database.GetService<IRelationalDatabaseCreator>();

        if (!databaseCreator.Exists())
        {
            logger.LogWarning("Database doesn't exist, building...");
            databaseCreator.Create();
            logger.LogInformation("Database build successfully!");
        }

        // exec migration
        context.Database.Migrate();
        logger.LogInformation("Database update succefully!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database update failed: {Message}", ex.Message);
        throw;
    }
}

app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();


