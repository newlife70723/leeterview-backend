# 使用 .NET SDK 映像來建置
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# 複製專案並還原相依套件
COPY . ./
RUN dotnet restore

# 安裝 dotnet-ef 工具
RUN dotnet tool install --global dotnet-ef

# 在建置階段執行資料庫遷移
RUN dotnet ef database update

# 發佈應用
RUN dotnet publish -c Release -o out

# 使用 .NET 運行時映像來運行應用
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# 複製建置結果
COPY --from=build /app/out .

# 根據環境自動切換（預設 Development）
ARG ENVIRONMENT=Development
ENV ASPNETCORE_ENVIRONMENT=$ENVIRONMENT

# 開放 API 服務埠口
EXPOSE 5000

# 啟動後端 API
ENTRYPOINT ["dotnet", "leeterview-backend.dll"]
