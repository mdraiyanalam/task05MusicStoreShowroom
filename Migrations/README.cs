// Migrations folder for Entity Framework Core database schema changes
// 
// IMPORTANT: This folder is prepared for future database requirements, but is NOT currently used.
// 
// ARCHITECTURE DECISION: No database is required for storing random data.
// 
// All song data (titles, artists, albums, genres, covers, audio, lyrics) is generated
// deterministically in-memory on the server-side using seed-based generation. This approach:
// 
// - Eliminates storage overhead (no data persistence layer needed)
// - Ensures perfect reproducibility (same seed = same data always)
// - Scales infinitely (no database limits or storage growth)
// - Maintains privacy (no data collection/retention)
// - Simplifies operations (no backup/migration/recovery needed)
//
// If migrations are needed in the future (e.g., for lookup tables or configuration storage),
// they should be placed in this folder following Entity Framework Core naming conventions:
// 
// Example:
//   [Timestamp]_InitialCreate.cs
//   [Timestamp]_AddLookupTables.cs
//
// To generate migrations in the future:
//   dotnet ef migrations add InitialCreate
//   dotnet ef database update
//
// Note: The application currently uses JSON files in the Data/ folder for locale lookups,
// which is the recommended approach instead of database lookups for this use case.

namespace MusicStore.Migrations;

// This class intentionally left empty - no migrations are currently needed.
// The folder structure exists for future extensibility and follows standard EF Core conventions.
