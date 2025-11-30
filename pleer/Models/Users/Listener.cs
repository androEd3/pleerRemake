using pleer.Models.Media;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pleer.Models.Users
{
    public class Listener
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; }

        //ban status
        public bool Status { get; set; }

        public int ProfilePictureId { get; set; }
        public ProfilePicture ProfilePicture { get; set; }

        [Required]
        [MaxLength(64)]
        public string PasswordHash { get; set; }

        [Column(TypeName = "date")]
        public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        //навигация
        public virtual ICollection<ListenerPlaylistsLink> ListenerPlaylists { get; set; } = new List<ListenerPlaylistsLink>();
    }
}
