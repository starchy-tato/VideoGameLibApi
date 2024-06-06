using Microsoft.EntityFrameworkCore;

class VideoGameDb : DbContext
{
    public VideoGameDb(DbContextOptions<VideoGameDb> options)
        : base(options) { }

    public DbSet<VideoGame> VideoGames => Set<VideoGame>();
}