using Microsoft.EntityFrameworkCore;
using pleer.Models.DatabaseContext;
using pleer.Models.Media;
using pleer.Models.Users;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace pleer.Models.Service
{
    // Сервисный класс для работы с плейлистами
    public class ServiceMethods
    {
        public async static Task AddPlaylistWithLink(Listener listener)
        {
            using var context = new DBContext();

            try
            {
                var playlistsCount = await context.Playlists
                    .Where(p => p.CreatorId == listener.Id)
                    .CountAsync();

                PlaylistCover cover;

                string playlistTitle = string.Empty;

                if (playlistsCount == 0)
                {
                    cover = await context.PlaylistCovers
                        .FirstAsync(pc => pc.FilePath == InitializeData.GetFavoritesCoverPath());
                    playlistTitle = "Избранное";
                }
                else
                {
                    cover = await context.PlaylistCovers
                        .FirstAsync(pc => pc.FilePath == InitializeData.GetDefaultCoverPath());
                    playlistTitle = $"Плейлист {playlistsCount + 1}";
                }

                if (cover == null)
                {
                    MessageBox.Show("Обложка для плейлиста не найдена", "Медиатека",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var playlist = new Playlist()
                {
                    Title = playlistTitle,
                    CreatedAt = DateOnly.FromDateTime(DateTime.Now),
                    CoverId = cover.Id,
                    CreatorId = listener.Id
                };

                await context.Playlists.AddAsync(playlist);
                await context.SaveChangesAsync();

                var link = new ListenerPlaylistsLink()
                {
                    ListenerId = listener.Id,
                    PlaylistId = playlist.Id,
                };

                await context.ListenerPlaylistsLinks.AddAsync(link);
                await context.SaveChangesAsync();

                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании плейлиста: {ex.Message}", "Медиатека",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string GetSha256Hash(string input)
        {
            byte[] data = SHA256.HashData(Encoding.UTF8.GetBytes(input));

            StringBuilder sBuilder = new();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        //Input fields proverochki
        public static bool IsPasswordsSame(string password, string repeatedPassword)
        {
            if (password != repeatedPassword)
                return false;

            return true;
        }

        public static string IsPasswordsValidOutput(string password)
        {
            if (password.Length < 6)
                return "Пароль должен содержать минимум 6 символов";

            if (password.Length > 32)
                return "Пароль не должен превышать 32 символа";

            bool hasDigit = password.Any(char.IsDigit);
            bool hasLetter = password.Any(char.IsLetter);

            if (!hasDigit)
                return "Пароль должен содержать хотя бы одну цифру";

            if (!hasLetter)
                return "Пароль должен содержать хотя бы одну букву";

            return password;
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                string pattern = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";

                return Regex.IsMatch(email, pattern);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }

    public class InitializeData
    {
        public static void SeedData(DBContext context)
        {
            //Seed covers
            if (!context.PlaylistCovers.Any())
            {
                var pCovers = new List<PlaylistCover>()
                {
                    { new() { FilePath = GetDefaultCoverPath() } },
                    { new() { FilePath = GetFavoritesCoverPath() } },
                };
                context.AddRange(pCovers);
                context.SaveChanges();
            }

            if (!context.ProfilePictures.Any())
            {
                var pPictures = new List<ProfilePicture>()
                {
                    { new() { FilePath = GetDefaultProfilePicturePath() } },
                };
                context.AddRange(pPictures);
                context.SaveChanges();
            }
        }

        public static void CreateAdmin(DBContext context)
        {
            if (!context.Admins.Any())
            {
                var admins = new List<Admin>()
                {
                    { new() { Login = "Admin", PasswordHash = ServiceMethods.GetSha256Hash("Admin") } },
                };
                context.AddRange(admins);
                context.SaveChanges();
            }
        }

        public static void CreateListeners(DBContext context)
        {
            if (context.Listeners.Any())
                return;

            var favoritesCover = context.PlaylistCovers
                .First(pc => pc.FilePath == GetFavoritesCoverPath());

            var data = new (string Name, string Email, string Date)[]
            {
                ("Alex Chen", "alex.chen@gmail.com", "2024-01-05"),
                ("Maria Santos", "maria.santos@outlook.com", "2024-01-12"),
                ("Дмитрий К.", "dimka@yandex.ru", "2024-01-18"),
                ("Emma Wilson", "emma.w@gmail.com", "2024-02-03"),
                ("藤田健太", "kenta.fujita@mail.jp", "2024-02-14"),
                ("Олег", "oleg.music@mail.ru", "2024-02-22"),
                ("Sophie Martin", "sophie.m@yahoo.fr", "2024-03-01"),
                ("Анна Б.", "anna.b@gmail.com", "2024-03-10"),
                ("Jake Thompson", "jake.t@hotmail.com", "2024-03-15"),
                ("김민수", "minsu.kim@naver.com", "2024-03-28"),
                ("Lucas", "lucas.beats@gmail.com", "2024-04-02"),
                ("Ира", "ira.melody@yandex.ru", "2024-04-11"),
                ("Mohammed Ali", "m.ali@outlook.com", "2024-04-19"),
                ("Nina Petrova", "nina.p@mail.ru", "2024-05-03"),
                ("Tom", "tom.music@gmail.com", "2024-05-14"),
                ("Yuki", "yuki.sound@gmail.com", "2024-05-22"),
                ("Макс", "max.tune@yandex.ru", "2024-06-01"),
                ("Clara", "clara.vibes@outlook.de", "2024-06-15"),
                ("Рома", "roma.play@mail.ru", "2024-06-28"),
                ("Zoe", "zoe.audio@gmail.com", "2024-07-04")
            };

            var listeners = data.Select((d, i) => new Listener
            {
                Name = d.Name,
                Email = d.Email,
                Status = false,
                ProfilePictureId = 1,
                PasswordHash = ServiceMethods.GetSha256Hash($"testpass{i + 1}"),
                CreatedAt = DateOnly.Parse(d.Date)
            }).ToList();

            context.Listeners.AddRange(listeners);
            context.SaveChanges();

            // Создаём плейлисты и связи для каждого слушателя
            foreach (var listener in listeners)
            {
                var playlist = new Playlist
                {
                    Title = "Избранное",
                    CreatedAt = DateOnly.FromDateTime(DateTime.Now),
                    CoverId = favoritesCover.Id,
                    CreatorId = listener.Id
                };

                context.Playlists.Add(playlist);
                context.SaveChanges();

                var link = new ListenerPlaylistsLink
                {
                    ListenerId = listener.Id,
                    PlaylistId = playlist.Id
                };

                context.ListenerPlaylistsLinks.Add(link);
            }

            context.SaveChanges();
        }

        public static string GetDefaultCoverPath()
        {
            return "pack://application:,,,/Resources/ServiceImages/DefaultCover.png";
        }

        public static string GetDefaultProfilePicturePath()
        {
            return "pack://application:,,,/Resources/ServiceImages/DefaultPicture.png";
        }

        public static string GetFavoritesCoverPath()
        {
            return "pack://application:,,,/Resources/ServiceImages/FavoritesCover.png";
        }
    }

    public class PictureService
    {
        public ProfilePicture SaveProfilePicture(string sourceImagePath, Listener listener)
        {
            try
            {
                DBContext context = new();

                if (Uri.TryCreate(sourceImagePath, UriKind.Absolute, out var uri))
                {
                    sourceImagePath = uri.LocalPath;
                }

                string extension = Path.GetExtension(sourceImagePath);
                string fileName = string.Empty;
                string destinationPath = string.Empty;

                if (listener != null)
                {
                    var listenerProfilePicturePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                        "pleer",
                        "ServiceImages",
                        "ProfilePictures",
                        $"Listener_{listener.Id}");
                    Directory.CreateDirectory(listenerProfilePicturePath);

                    int fileCount = Directory.GetFiles(listenerProfilePicturePath).Length;

                    fileName = $"ProfilePicture_{fileCount}{extension}";
                    destinationPath = Path.Combine(listenerProfilePicturePath, fileName);
                }

                File.Copy(sourceImagePath, destinationPath, overwrite: true);

                var profilePicture = new ProfilePicture
                {
                    FilePath = destinationPath,
                };

                return profilePicture;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении изображения: {ex.Message}");
            }
        }

        public static PlaylistCover SavePlaylistCover(string sourceImagePath, int coverId)
        {
            try
            {
                var projectPlaylistCoversPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "pleer",
                "ServiceImages",
                "PlaylistCovers");

                Directory.CreateDirectory(projectPlaylistCoversPath);

                string extension = Path.GetExtension(sourceImagePath);
                string fileName = $"playlistCover_{coverId}{extension}";
                string destinationPath = Path.Combine(projectPlaylistCoversPath, fileName);

                File.Copy(sourceImagePath, destinationPath, overwrite: true);

                var playlistCover = new PlaylistCover
                {
                    FilePath = destinationPath,
                };

                return playlistCover;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении изображения: {ex.Message}");
            }
        }
    }
}
