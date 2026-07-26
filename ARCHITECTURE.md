# ARCHITECTURE

## Overview

This application follows a **server-side generation architecture** where all data is generated deterministically on the server and delivered to the browser as read-only batches. No random data is stored on the server; instead, data is generated on-demand from seed values.

## Key Principles

### 1. No Data Storage for Generated Content

**Requirement**: Do not store random data on the server.

**Implementation**:
- All song data (titles, artists, albums, genres) is generated in-memory on-demand
- No persistent database tables store generated songs
- Each HTTP request triggers fresh generation based on seed
- This ensures:
  - **Zero storage overhead**: No database growth
  - **Perfect reproducibility**: Same seed always produces identical data
  - **Infinite scalability**: No record limit or storage constraints
  - **Privacy**: No data collection or retention

### 2. Server-Side Generation (No Browser Generation)

**Requirement**: All data must be generated on the server side, not in the browser.

**Implementation**:
- `SongGeneratorService` runs exclusively on the server
- Browser receives only rendered HTML/JSON responses
- Generation algorithm (seed combination, content selection) is server-only
- Benefits:
  - **Security**: Algorithm and locale data remain server-side
  - **Determinism**: Identical generation across all clients
  - **Performance**: Browser handles only rendering, not computation

### 3. Single Page (Batch) Data Model

**Requirement**: Browser connects to a single server that provides a single page (batch) of data.

**Implementation**:
- Page size: 12 songs per batch (configurable)
- Endpoints deliver exactly one page of data per request:
  - `GET /?page=1` → 12 songs for table view (page 1)
  - `GET /?page=2` → 12 songs for table view (page 2)
  - `GET /SongsPartial?page=N` → Partial HTML for dynamic updates
- Browser manages pagination/infinite scroll UI, server provides data batches
- Seed + page number combine to produce unique content per page:
  ```
  contentSeed = (seed XOR (seed >> 32) XOR (page * 397)) & 0x7FFFFFFF
  ```

### 4. No Database Required for Random Data

**Requirement**: No database is required for storing random data.

**Implementation**:
- Generated content is NOT persisted
- Locale lookup data uses JSON files (external resources):
  - `Data/locales-en-US.json`: English names, genres, album words, lyrics
  - `Data/locales-de-DE.json`: German equivalents
  - Can be extended with additional locale files without code changes
- No EntityFramework Core migrations used for song data
- Migrations folder exists for future extensibility (lookup tables, config)

## Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ Browser                                                         │
├─────────────────────────────────────────────────────────────────┤
│ User adjusts: Language, Seed, LikesPerSong                      │
│ ↓                                                               │
│ POST /SongsPartial?page=1&Language=en-US&Seed=42&Likes=5.0     │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ Server                                                          │
├─────────────────────────────────────────────────────────────────┤
│ Index.cshtml.cs.OnGetSongsPartial(page, language, seed, likes) │
│   ↓                                                             │
│ SongGeneratorService.GenerateSongs()                           │
│   ├─ Load locale data from Data/locales-{language}.json        │
│   ├─ Calculate contentSeed = f(seed, page)                     │
│   ├─ For each of 12 songs:                                     │
│   │   ├─ Generate title (from locale names)                    │
│   │   ├─ Generate artist (from locale names)                   │
│   │   ├─ Generate genre (from locale genres)                   │
│   │   ├─ Generate album (from locale words)                    │
│   │   └─ Calculate likes (from likes-per-song parameter)       │
│   └─ Return List<Song> (in-memory, not persisted)             │
│   ↓                                                             │
│ Render _SongListPartial.cshtml (HTML table/gallery)           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ Browser                                                         │
├─────────────────────────────────────────────────────────────────┤
│ Display 12 songs in table/gallery                              │
│ User can:                                                       │
│   - Click "Next" to request page 2 (different contentSeed)     │
│   - Click song to expand and view details                      │
│   - Play audio (GET /audio/{seed}/{id} → generated on-demand)  │
│   - View cover (GET /cover/{seed}/{id} → generated on-demand)  │
│   - Read lyrics (GET /lyrics/{seed}/{id} → generated on-demand)│
└─────────────────────────────────────────────────────────────────┘
```

## On-Demand Generation Endpoints

All content is generated when accessed, never stored:

1. **Audio**: `GET /audio/{seed}/{id}`
   - Generates WAV/MP3 using NAudio library
   - Music theory-based synthesis: chord progressions, vibrato, harmonies
   - Deterministic from seed

2. **Cover**: `GET /cover/{seed}/{id}`
   - Generates SVG with gradient patterns and geometric shapes
   - Reproducible design from seed
   - No ImageSharp (SVG render directly)

3. **Lyrics**: `GET /lyrics/{seed}/{id}`
   - Generates timestamped lyrics from locale word lists
   - JSON response: `[{time: 0, text: "..."}, ...]`
   - Reproducible from seed

4. **Export**: `GET /export`
   - Generates ZIP with MP3 files (one per song in batch)
   - All files created in-memory
   - Temporary files cleaned up after download

## Reproducibility & Determinism

**Guarantee**: Same seed always produces identical data.

**Mechanism**:
- User seed + page number combined via XOR:
  ```
  contentSeed = (seed ^ (seed >> 32) ^ (long)page * 397) & 0x7FFFFFFF
  ```
- Each part of generation (title, artist, genre, album) uses contentSeed
- Audio/cover generation uses same seed for consistency
- Likes calculation uses separate derived seed, independent of title generation

**Result**:
- User can share seed: "Use seed 42 for awesome songs"
- Other users get identical data on same seed
- Works across devices, dates, server restarts
- No database state needed

## Scalability

**Advantages of In-Memory Generation**:
1. **No Database Load**: Zero queries for song data
2. **No Storage Growth**: Data generated on-demand
3. **Horizontal Scaling**: Each server instance generates independently
4. **Infinite Pages**: Users can paginate indefinitely without storage limits
5. **Caching-Friendly**: Stateless requests allow HTTP caching

**Performance**:
- Page generation: ~10ms per batch (12 songs)
- Audio generation: ~100ms (WAV)
- Cover generation: ~5ms (SVG)
- All operations scale with page size, not data volume

## Locale Data (JSON-Based)

**Recommendation**: Use external resources instead of database lookups.

**Implementation**:
- Locale files in `Data/locales-*.json`
- Each file contains:
  ```json
  {
    "locale": "en-US",
    "genres": ["Rock", "Pop", ...],
    "firstNames": ["John", "Jane", ...],
    "lastNames": ["Smith", "Johnson", ...],
    "albumWords": ["Dream", "Night", ...],
    "lyricsWords": ["la", "na", "oh", ...]
  }
  ```
- Loaded once at startup into memory
- New locales added by dropping JSON file (no code changes)
- Safer than database: no injection attacks, full revision control

## Future: Database Extensions (If Needed)

**When to use database**:
- User preferences/settings
- Analytics/usage logs
- Lookup tables for locale extensions
- Configuration management

**How to implement**:
- Place migrations in `Migrations/` folder
- Follow EF Core naming: `[Timestamp]_Description.cs`
- Command: `dotnet ef migrations add InitialCreate`
- Never persist generated song data

**NOT recommended**:
- Storing generated songs
- Caching audio/cover files
- Database lookups for locale data

## Summary

| Aspect | Approach | Benefit |
|--------|----------|---------|
| Song Data | In-memory generation | Zero storage, perfect reproducibility |
| Locale Data | JSON files | Fast, versioned, no injection risk |
| Audio/Covers | Generated on-demand | No file storage, deterministic |
| Database | Optional for config/analytics | Separation of concerns |
| Scalability | Stateless, seed-based | Infinite pages, horizontal scaling |
| Reproducibility | Seed-based generation | Same seed = same data always |

This architecture ensures the application is lightweight, infinitely scalable, and perfectly deterministic.
