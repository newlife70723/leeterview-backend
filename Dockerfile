 FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-and-run

# 設定工作目錄
WORKDIR /app

# 安裝 dotnet-ef 工具
RUN dotnet tool install --global dotnet-ef

# 確保工具可用，將其添加到 PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# 複製專案檔到容器
COPY . ./

# 還原依賴
RUN dotnet restore

# 建置專案
RUN dotnet build -c Release --no-restore

# 遷移資料庫並執行應用程式
CMD ["sh", "-c", "dotnet ef database update && dotnet run --no-build"]