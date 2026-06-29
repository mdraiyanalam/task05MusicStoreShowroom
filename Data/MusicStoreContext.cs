using Microsoft.EntityFrameworkCore;
using MusicStore.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MusicStore.Data;

public class MusicStoreContext : DbContext
{
    public DbSet<Song> Songs { get; set; } = null!;

    public MusicStoreContext(DbContextOptions<MusicStoreContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed some base data if needed
        base.OnModelCreating(modelBuilder);
    }
}