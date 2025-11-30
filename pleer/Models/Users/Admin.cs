namespace pleer.Models.Users
{
    public class Admin
    {
        public int Id { get; set; }

        public string Login { get; set; }
        public string PasswordHash { get; set; }
    }
}
