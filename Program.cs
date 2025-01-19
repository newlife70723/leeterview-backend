using leeterview_backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ✅ 加入 Swagger/OpenAPI 支援
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ 註冊 DbContext 並設定資料庫連線字串
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ 加入 CORS 支援
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

// ✅ 加入控制器服務
builder.Services.AddControllers();

var app = builder.Build();

// ✅ 啟用 CORS
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

// ✅ 設定控制器路由
app.MapControllers();

app.Run();
