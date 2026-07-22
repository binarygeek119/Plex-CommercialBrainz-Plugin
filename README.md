# CommercialBrainz Plex Metadata Provider

HTTP [Custom Metadata Provider](https://developer.plex.tv/pms/#section/API-Info/Metadata-Providers) for TV commercials using [CommercialBrainz](https://commercialbrainz.duckdns.org/) ([source](https://github.com/binarygeek119/CommercialBrainz)).

Requires **Plex Media Server 1.43.0+**.

## How matching works

1. Existing CommercialBrainz GUID on the item  
2. Local file **SHA-256** → `GET /api/v1/hashes/lookup?file_sha256=`  
3. Optional **Chromaprint** (`fpcalc`) → `POST /api/v1/hashes/lookup`  
4. **Stash-style video pHash** (ffmpeg contact sheet + imagehash-compatible DCT) → `GET /api/v1/hashes/lookup?phash=`  
5. **Title search** → `GET /api/v1/search`

Plex only sends a relative `filename` in match requests. Set `MEDIA_ROOTS` to absolute directories that contain those files (same volumes mounted into Docker as into Plex).

## Quick start (Docker)

1. Edit `docker-compose.yml` and set the media volume to your commercials library path.
2. Start the provider:

```bash
docker compose up -d --build
```

3. In Plex: **Settings → Troubleshooting → Metadata Agents → Add Provider**  
   URL: `http://<host>:8765/movie`  
   (use your LAN IP or Docker host hostname; `localhost` only works if PMS runs on the same machine)
4. **Add Agent** → name it CommercialBrainz → set this provider as Primary → also add **Plex Local Media Assets**.
5. Create or edit a **Movies** library of commercials → Advanced → select that agent → scan / refresh metadata.

## Quick start (local)

```bash
dotnet run --project src/CommercialBrainz.PlexProvider
```

Then add `http://localhost:8765/movie` as a provider in Plex.

## Configuration

| Environment variable | Default | Description |
|----------------------|---------|-------------|
| `API_BASE_URL` | `https://commercialbrainz.duckdns.org/api/v1` | CommercialBrainz API base |
| `SITE_BASE_URL` | `https://commercialbrainz.duckdns.org` | Public site base |
| `PHASH_THRESHOLD` | `12` | Max Hamming distance for pHash matches |
| `ENABLE_AUDIO_FINGERPRINT` | `true` | Use `fpcalc` when available |
| `MEDIA_ROOTS` | _(empty)_ | `;`- or `,`-separated absolute roots for resolving `filename` |
| `FFMPEG_PATH` | `ffmpeg` | ffmpeg binary |
| `FFPROBE_PATH` | `ffprobe` | ffprobe binary |
| `FPCALC_PATH` | `fpcalc` | Chromaprint fpcalc binary |
| `PORT` | `8765` | Listen port |
| `BASE_URL` | `http://localhost:8765` | Advertised URL (logging / docs) |

## Provider endpoints

| Method | Path | Role |
|--------|------|------|
| GET | `/movie` | MediaProvider definition |
| POST | `/movie/library/metadata/matches` | Match movies |
| GET | `/movie/library/metadata/{ratingKey}` | Full metadata |
| GET | `/movie/library/metadata/{ratingKey}/images` | Poster images |

Identifier: `tv.plex.agents.custom.commercialbrainz.movie`

## Requirements

- .NET 8 SDK (local) or Docker
- **ffmpeg** / **ffprobe** for pHash matching
- Optional: **fpcalc** for audio fingerprint matching
- Media files readable at paths under `MEDIA_ROOTS`

## Build & test

```bash
dotnet build -c Release
dotnet test -c Release
```

## Library tips

- Store commercials in a **Movies** library  
- Prefer filenames that include the commercial title for title-search fallback  
- Hash matching needs `MEDIA_ROOTS` to align with Plex’s relative `filename` values  

## Related

- [Jellyfin plugin](https://github.com/binarygeek119/CommercialBrainz-jellyfin-plugin)
- [CommercialBrainz](https://github.com/binarygeek119/CommercialBrainz)
