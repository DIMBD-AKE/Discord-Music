# 1. 빌드 스테이지
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 모든 .csproj 파일들을 구조에 맞춰 복사 (restore 캐시 활용)
COPY ["Discord-Music/Discord-Music.csproj", "Discord-Music/"]
COPY ["YoutubeExplode/YoutubeExplode.csproj", "YoutubeExplode/"]

# 의존성 복구 (restore)
RUN dotnet restore "Discord-Music/Discord-Music.csproj"

# 전체 소스 코드 복사
COPY . .

# 빌드 및 게시
WORKDIR "/src/Discord-Music"
RUN dotnet publish "Discord-Music.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. 런타임 스테이지
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Discord 봇 운영에 필요한 라이브러리 설치
RUN apt-get update && apt-get install -y \
    libopus-dev \
    libsodium-dev \
    libssl-dev \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# 빌드 결과물만 복사
COPY --from=build /app/publish .

# 필요한 설정 파일 및 리소스 복사 (빌드 결과물에 포함되지 않는 경우)
COPY Discord-Music/Languages ./Languages
COPY Discord-Music/Config.json ./Config.json

ENTRYPOINT ["dotnet", "Discord-Music.dll"]