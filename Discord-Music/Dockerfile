FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY Discord-Music/*.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y \
    libopus-dev \
    libsodium-dev \
    libssl-dev \
    ffmpeg

COPY --from=build /app/out .

COPY Discord-Music/Languages /app/Languages
COPY Discord-Music/Config.json /app/Config.json

ENTRYPOINT ["dotnet", "Discord-Music.dll"]