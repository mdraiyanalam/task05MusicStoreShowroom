using MusicStore.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<SongGeneratorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Map endpoints for generated audio and covers (after app is created)
app.MapGet("/audio/{seed:long}/{id:int}", (long seed, int id, SongGeneratorService svc) =>
{
    var audio = svc.GenerateAudioMp3(seed, id);
    // We generated WAV bytes; return as audio/wav
    return Results.File(audio, "audio/wav");
});

app.MapGet("/cover/{language}/{seed:long}/{id:int}", (string language, long seed, int id, SongGeneratorService svc) =>
{
    // Generate metadata using the requested language so cover text reflects locale
    var song = svc.GenerateSongById(language, seed, id);
    var png = svc.GenerateCoverPng(seed, id, song?.Title ?? $"Song {id}", song?.Artist ?? "Unknown");
    return Results.File(png, "image/png");
});

app.MapGet("/export", (HttpRequest req, SongGeneratorService svc) =>
{
    // expects query: seed, page, count, Language
    long seed = long.TryParse(req.Query["seed"], out var sv) ? sv : 42L;
    int page = int.TryParse(req.Query["page"], out var pv) ? pv : 1;
    int count = int.TryParse(req.Query["count"], out var cv) ? cv : 12;
    string language = req.Query["Language"].FirstOrDefault() ?? req.Query["language"].FirstOrDefault() ?? "en-US";

    var songs = svc.GenerateSongs(language, seed, 0, count, page);
    using var ms = new MemoryStream();
    using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
    {
        foreach (var song in songs)
        {
            var fileName = SanitizeFileName($"{song.Title} - {(!string.IsNullOrEmpty(song.Album) ? song.Album : "Single")} - {song.Artist}.wav");
            var entry = archive.CreateEntry(fileName);
            using var es = entry.Open();
            var audio = svc.GenerateAudioMp3(seed, song.Id);
            es.Write(audio, 0, audio.Length);
        }
    }
    ms.Position = 0;
    return Results.File(ms.ToArray(), "application/zip", $"songs-{seed}-p{page}.zip");
});

string SanitizeFileName(string name)
{
    foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
    return name;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.Run();
