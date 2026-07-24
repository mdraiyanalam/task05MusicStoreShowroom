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
    // If ffmpeg produced mp3 bytes, return as audio/mpeg, otherwise fall back to audio/wav
    var isMp3 = audio.Length > 3 && audio[0] == 0x49 && audio[1] == 0x44 && audio[2] == 0x33; // ID3 MP3 header
    return Results.File(audio, isMp3 ? "audio/mpeg" : "audio/wav");
});

app.MapGet("/lyrics/{language}/{seed:long}/{id:int}", (string language, long seed, int id, SongGeneratorService svc) =>
{
    var lines = svc.GenerateLyrics(language, seed, id);
    var dto = lines.Select(l => new { time = l.time, text = l.line }).ToArray();
    return Results.Json(dto);
});

app.MapGet("/cover/{language}/{seed:long}/{id:int}", (string language, long seed, int id, SongGeneratorService svc) =>
{
    // Generate metadata using the requested language so cover text reflects locale
    var song = svc.GenerateSongById(language, seed, id);
    var svg = svc.GenerateCoverSvg(seed, id, song?.Title ?? $"Song {id}", song?.Artist ?? "Unknown");
    return Results.File(svg, "image/svg+xml");
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
            var safeName = SanitizeFileName($"{song.Title} - {(!string.IsNullOrEmpty(song.Album) ? song.Album : "Single")} - {song.Artist}");
            var mp3Name = safeName + ".mp3";
            var entry = archive.CreateEntry(mp3Name);
            using var es = entry.Open();
            var audio = svc.GenerateAudioMp3(seed, song.Id);
            // If audio is MP3 (ffmpeg produced), write directly; otherwise try to convert wav to mp3 via ffmpeg
            var isMp3 = audio.Length > 3 && audio[0] == 0x49 && audio[1] == 0x44 && audio[2] == 0x33;
            if (isMp3)
            {
                es.Write(audio, 0, audio.Length);
            }
            else
            {
                try
                {
                    var tempWav = Path.Combine(Path.GetTempPath(), $"exp_{seed}_{song.Id}_{Guid.NewGuid()}.wav");
                    var tempMp3 = Path.Combine(Path.GetTempPath(), $"exp_{seed}_{song.Id}_{Guid.NewGuid()}.mp3");
                    File.WriteAllBytes(tempWav, audio);
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-y -i \"{tempWav}\" -b:a 128k \"{tempMp3}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var p = System.Diagnostics.Process.Start(psi))
                    {
                        p.WaitForExit(15000);
                    }
                    if (File.Exists(tempMp3))
                    {
                        var mp3bytes = File.ReadAllBytes(tempMp3);
                        es.Write(mp3bytes, 0, mp3bytes.Length);
                    }
                    else
                    {
                        es.Write(audio, 0, audio.Length);
                    }
                    try { File.Delete(tempWav); } catch { }
                    try { File.Delete(tempMp3); } catch { }
                }
                catch
                {
                    es.Write(audio, 0, audio.Length);
                }
            }
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
