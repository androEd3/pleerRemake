using pleer.Models.Users;

namespace pleer.Models.Media
{
    public class ListenerPlaylistsLink
    {
        public int ListenerId { get; set; }
        public virtual Listener Listener { get; set; }

        public int PlaylistId { get; set; }
        public virtual Playlist Playlist { get; set; }
    }
}
