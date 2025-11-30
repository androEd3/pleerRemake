using System.ComponentModel.DataAnnotations;

namespace pleer.Models.Media
{
    public class PlaylistCover
    {
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; }
    }
}
