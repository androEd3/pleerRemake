using pleer.Models.IA;
using pleer.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace pleer.Models.Media
{
    public class Playlist
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Title { get; set; }

        public int CoverId { get; set; }
        public PlaylistCover? Cover { get; set; }

        public int CreatorId { get; set; }
        public Listener? Creator { get; set; }

        [MaxLength(300)]
        public string? Description { get; set; }

        [Required]
        public DateOnly CreatedAt { get; set; }

        public List<string> TracksId { get; set; } = [];

        //навигация
        public virtual ICollection<ListenerPlaylistsLink> ListenerPlaylists { get; set; } = [];
    }
}
