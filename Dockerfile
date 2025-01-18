# 建置階段
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# 複製檔案並還原相依套件
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# 執行階段
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# 複製建置結果
COPY --from=build /app/out .

# 開放 API 服務埠口
EXPOSE 5000

# 啟動後端 API
ENTRYPOINT ["dotnet", "leeterview-backend.dll"]

