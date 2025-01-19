using leeterview_backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ✅ 正確加入 Swagger/OpenAPI 支援
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

// 註冊 DbContext 並設定資料庫連線字串
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔥 自動切換環境設定
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevCors",
            policy => policy.AllowAnyOrigin()
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
            policy => policy.WithOrigins("https://leeterview.net")  // 正式環境網址
                            .AllowAnyHeader()
                            .AllowAnyMethod());
    });

    Console.WriteLine("🚀 正在運行【正式環境】");
}

var app = builder.Build();

// ✅ 啟用 Swagger（僅在開發環境）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔥 中間件正確順序
app.UseHttpsRedirection();  // 啟用 HTTPS 重定向
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
}
else
{
    app.UseCors("ProdCors");
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
