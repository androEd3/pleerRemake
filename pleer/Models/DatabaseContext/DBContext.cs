using Microsoft.EntityFrameworkCore;
using pleer.Models.Media;
using pleer.Models.Users;
using System.Xml;

namespace pleer.Models.DatabaseContext
{
    public class DBContext : DbContext
    {
        public DBContext() : base()
        { }

        // Users
        public DbSet<Listener> Listeners { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<ProfilePicture> ProfilePictures { get; set; }

        // Playlists
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistCover> PlaylistCovers { get; set; }
        public DbSet<ListenerPlaylistsLink> ListenerPlaylistsLinks { get; set; }

        //Ban lists

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=ROONIN-COMPUTAH\\SQLEXPRESS;Database=pleerRemake;Integrated Security=SSPI;Trust Server Certificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ListenerPlaylistsLink>(entity =>
            {
                // Установка составного ключа
                entity.HasKey(upl => new { upl.ListenerId, upl.PlaylistId });

                // Настройка отношения с User
                entity.HasOne(upl => upl.Listener)
                      .WithMany(u => u.ListenerPlaylists)
                      .HasForeignKey(upl => upl.ListenerId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Настройка отношения с Playlist
                entity.HasOne(upl => upl.Playlist)
                      .WithMany(p => p.ListenerPlaylists)
                      .HasForeignKey(upl => upl.PlaylistId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
