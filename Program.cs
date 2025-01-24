using LeeterviewBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 載入 appsettings.json 並加入環境變數
var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// 解析佔位符
var configuration = configurationBuilder.Build();
foreach (var (key, value) in configuration.AsEnumerable())
{
    if (value != null && value.Contains("${"))
    {
        configuration[key] = ReplacePlaceholders(value, Environment.GetEnvironmentVariables());
    }
}

// 替換後的設定應用到 Builder
builder.Configuration.AddConfiguration(configuration);

// 讀取 Redis 連接字串，如果為 null，則使用預設值
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";

// 打印 Redis 連接字串
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
logger.LogInformation("Redis Connection String: {RedisConnectionString}", redisConnectionString);

// 連接 Redis
var redis = ConnectionMultiplexer.Connect(redisConnectionString);

// 注入 Redis 連接
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// 讀取密鑰
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("leeterviewApiSuperLongKey1234567890123456"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,  // 檢查過期時間
            ValidateIssuerSigningKey = true,  // 檢查簽名密鑰
            IssuerSigningKey = jwtKey,  // 使用的簽名密鑰
            ClockSkew = TimeSpan.Zero,  // 設置過期時間容錯
            ValidIssuer = "Leeterview", // 設定發行者
            ValidAudience = "Leeterview API" // 設定受眾
        };
    });

builder.Services.AddAuthorization();

// 使用解析後的資料庫連線字串
var connectionString = configuration.GetConnectionString("DefaultConnection");
builder.Logging.AddConsole(); // 啟用 Console 日誌

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 自動切換環境設定
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevCors",
            policy => policy.AllowAnyOrigin() // 開發環境允許所有來源
                            .AllowAnyHeader()
                            .AllowAnyMethod());
    });

    Console.WriteLine("🚀 正在運行【開發環境】");
}
else
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ProdCors",
            policy => policy.WithOrigins("https://leeterview.net") // 正式環境僅允許前端網域
                            .AllowAnyHeader()
                            .AllowAnyMethod());
    });

    Console.WriteLine("🚀 正在運行【正式環境】");
}

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Database connection string: {ConnectionString}", connectionString);
});

// 🔥 啟用 CORS
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
}
else
{
    app.UseCors("ProdCors");
}

// ✅ 啟用 Swagger（僅在開發環境）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

// 啟用身份驗證中介軟體
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Helper 方法
static string ReplacePlaceholders(string input, System.Collections.IDictionary envVars)
{
    foreach (var key in envVars.Keys)
    {
        var placeholder = $"${{{key}}}";
        if (input.Contains(placeholder))
        {
            input = input.Replace(placeholder, envVars[key]?.ToString());
        }
    }
    return input;
}
