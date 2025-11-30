using Microsoft.EntityFrameworkCore;
using pleer.Models.DatabaseContext;
using pleer.Models.Media;
using pleer.Models.Users;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

namespace pleer.Models.Service
{
    // Сервисный класс для работы с плейлистами
    public class ServiceMethods
    {
        public static void AddPlaylistWithLink(Listener listener)
        {
            using var context = new DBContext();

            try
            {
                var playlistsCount = context.Playlists
                    .Where(p => p.CreatorId == listener.Id)
                    .Count();

                PlaylistCover cover;

                string playlistTitle = string.Empty;

                if (playlistsCount == 0)
                {
                    cover = context.PlaylistCovers
                        .First(pc => pc.FilePath == InitializeData.GetFavoritesCoverPath());
                    playlistTitle = "Избранное";
                }
                else
                {
                    cover = context.PlaylistCovers
                        .First(pc => pc.FilePath == InitializeData.GetDefaultCoverPath());
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

                context.Playlists.Add(playlist);
                context.SaveChanges();

                var link = new ListenerPlaylistsLink()
                {
                    ListenerId = listener.Id,
                    PlaylistId = playlist.Id,
                };

                context.ListenerPlaylistsLinks.Add(link);
                context.SaveChangesAsync();

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
            var listeners = new List<Listener>
            {
                new Listener
                {
                    Name = "Михаил Смирнов",
                    Email = "misha.smirnov@gmail.com",
                    Status = false,
                    ProfilePictureId = 10,
                    PasswordHash = "ab12cd34ef56789012345678901234567890123456789012345678901234aaaa",
                    CreatedAt = new DateOnly(2024, 1, 10)
                },
                new Listener
                {
                    Name = "Ольга Новикова",
                    Email = "olga.novikova@yandex.ru",
                    Status = false,
                    ProfilePictureId = 11,
                    PasswordHash = "bc23de45fg67890123456789012345678901234567890123456789012345bbbb",
                    CreatedAt = new DateOnly(2024, 2, 14)
                },
                new Listener
                {
                    Name = "Артём Морозов",
                    Email = "artem.moroz@mail.ru",
                    Status = false,
                    ProfilePictureId = 12,
                    PasswordHash = "cd34ef56gh78901234567890123456789012345678901234567890123456cccc",
                    CreatedAt = new DateOnly(2024, 3, 22)
                },
                new Listener
                {
                    Name = "Елена Соколова",
                    Email = "elena.sok@outlook.com",
                    Status = true, // Заблокирован
                    ProfilePictureId = 13,
                    PasswordHash = "de45fg67hi89012345678901234567890123456789012345678901234567dddd",
                    CreatedAt = new DateOnly(2024, 4, 8)
                },
                new Listener
                {
                    Name = "Никита Лебедев",
                    Email = "nikita.lebedev@gmail.com",
                    Status = false,
                    ProfilePictureId = 14,
                    PasswordHash = "ef56gh78ij90123456789012345678901234567890123456789012345678eeee",
                    CreatedAt = new DateOnly(2024, 5, 30)
                },
                new Listener
                {
                    Name = "Дарья Кузнецова",
                    Email = "dasha.kuz@proton.me",
                    Status = false,
                    ProfilePictureId = 15,
                    PasswordHash = "fg67hi89jk01234567890123456789012345678901234567890123456789ffff",
                    CreatedAt = new DateOnly(2024, 7, 12)
                },
                new Listener
                {
                    Name = "Сергей Попов",
                    Email = "sergey.popov@yandex.ru",
                    Status = true, // Заблокирован
                    ProfilePictureId = 16,
                    PasswordHash = "gh78ij90kl12345678901234567890123456789012345678901234567890gggg",
                    CreatedAt = new DateOnly(2024, 9, 5)
                },
                new Listener
                {
                    Name = "Анастасия Волкова",
                    Email = "nastya.volkova@mail.ru",
                    Status = false,
                    ProfilePictureId = 17,
                    PasswordHash = "hi89jk01lm23456789012345678901234567890123456789012345678901hhhh",
                    CreatedAt = new DateOnly(2024, 11, 18)
                },
                new Listener
                {
                    Name = "Иван Федоров",
                    Email = "ivan.fedorov@gmail.com",
                    Status = false,
                    ProfilePictureId = 18,
                    PasswordHash = "ij90kl12mn34567890123456789012345678901234567890123456789012iiii",
                    CreatedAt = new DateOnly(2025, 2, 25)
                }
            };

            context.Listeners.AddRange(listeners);
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
