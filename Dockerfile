FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/CommercialBrainz.PlexProvider/CommercialBrainz.PlexProvider.csproj src/CommercialBrainz.PlexProvider/
RUN dotnet restore src/CommercialBrainz.PlexProvider/CommercialBrainz.PlexProvider.csproj
COPY src/CommercialBrainz.PlexProvider/ src/CommercialBrainz.PlexProvider/
RUN dotnet publish src/CommercialBrainz.PlexProvider/CommercialBrainz.PlexProvider.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg libchromaprint-tools \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV PORT=8765 \
    ASPNETCORE_URLS=http://0.0.0.0:8765 \
    FFMPEG_PATH=ffmpeg \
    FFPROBE_PATH=ffprobe \
    FPCALC_PATH=fpcalc \
    MEDIA_ROOTS=/media

EXPOSE 8765
ENTRYPOINT ["dotnet", "CommercialBrainz.PlexProvider.dll"]
