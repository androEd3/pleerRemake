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
            var listeners = new List<Listener>
            {
                new Listener
                {
                    Name = "Михаил Смирнов",
                    Email = "misha.smirnov@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "ab12cd34ef56789012345678901234567890123456789012345678901234aaaa",
                    CreatedAt = new DateOnly(2024, 1, 10)
                },
                new Listener
                {
                    Name = "Ольга Новикова",
                    Email = "olga.novikova@yandex.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "bc23de45fg67890123456789012345678901234567890123456789012345bbbb",
                    CreatedAt = new DateOnly(2024, 2, 14)
                },
                new Listener
                {
                    Name = "Артём Морозов",
                    Email = "artem.moroz@mail.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "cd34ef56gh78901234567890123456789012345678901234567890123456cccc",
                    CreatedAt = new DateOnly(2024, 3, 22)
                },
                new Listener
                {
                    Name = "Елена Соколова",
                    Email = "elena.sok@outlook.com",
                    Status = true,
                    ProfilePictureId = 1,
                    PasswordHash = "de45fg67hi89012345678901234567890123456789012345678901234567dddd",
                    CreatedAt = new DateOnly(2024, 4, 8)
                },
                new Listener
                {
                    Name = "Никита Лебедев",
                    Email = "nikita.lebedev@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "ef56gh78ij90123456789012345678901234567890123456789012345678eeee",
                    CreatedAt = new DateOnly(2024, 5, 30)
                },
                new Listener
                {
                    Name = "Дарья Кузнецова",
                    Email = "dasha.kuz@proton.me",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "fg67hi89jk01234567890123456789012345678901234567890123456789ffff",
                    CreatedAt = new DateOnly(2024, 7, 12)
                },
                new Listener
                {
                    Name = "Сергей Попов",
                    Email = "sergey.popov@yandex.ru",
                    Status = true,
                    ProfilePictureId = 1,
                    PasswordHash = "gh78ij90kl12345678901234567890123456789012345678901234567890gggg",
                    CreatedAt = new DateOnly(2024, 9, 5)
                },
                new Listener
                {
                    Name = "Анастасия Волкова",
                    Email = "nastya.volkova@mail.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "hi89jk01lm23456789012345678901234567890123456789012345678901hhhh",
                    CreatedAt = new DateOnly(2024, 11, 18)
                },
                new Listener
                {
                    Name = "Иван Федоров",
                    Email = "ivan.fedorov@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "ij90kl12mn34567890123456789012345678901234567890123456789012iiii",
                    CreatedAt = new DateOnly(2025, 2, 25)
                },
                new Listener
                {
                    Name = "Алексей Козлов",
                    Email = "alexey.kozlov@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "jk01lm23no45678901234567890123456789012345678901234567890123jjjj",
                    CreatedAt = new DateOnly(2024, 1, 5)
                },
                new Listener
                {
                    Name = "Мария Петрова",
                    Email = "maria.petrova@yandex.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "kl12mn34op56789012345678901234567890123456789012345678901234kkkk",
                    CreatedAt = new DateOnly(2024, 1, 18)
                },
                new Listener
                {
                    Name = "Дмитрий Васильев",
                    Email = "dima.vasilev@mail.ru",
                    Status = true,
                    ProfilePictureId = 1,
                    PasswordHash = "lm23no45pq67890123456789012345678901234567890123456789012345llll",
                    CreatedAt = new DateOnly(2024, 2, 3)
                },
                new Listener
                {
                    Name = "Екатерина Михайлова",
                    Email = "kate.mikhailova@outlook.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "mn34op56qr78901234567890123456789012345678901234567890123456mmmm",
                    CreatedAt = new DateOnly(2024, 2, 20)
                },
                new Listener
                {
                    Name = "Андрей Николаев",
                    Email = "andrey.nikolaev@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "no45pq67rs89012345678901234567890123456789012345678901234567nnnn",
                    CreatedAt = new DateOnly(2024, 3, 1)
                },
                new Listener
                {
                    Name = "Татьяна Егорова",
                    Email = "tanya.egorova@yandex.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "op56qr78st90123456789012345678901234567890123456789012345678oooo",
                    CreatedAt = new DateOnly(2024, 3, 15)
                },
                new Listener
                {
                    Name = "Максим Белов",
                    Email = "max.belov@proton.me",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "pq67rs89tu01234567890123456789012345678901234567890123456789pppp",
                    CreatedAt = new DateOnly(2024, 4, 2)
                },
                new Listener
                {
                    Name = "Юлия Орлова",
                    Email = "julia.orlova@mail.ru",
                    Status = true,
                    ProfilePictureId = 1,
                    PasswordHash = "qr78st90uv12345678901234567890123456789012345678901234567890qqqq",
                    CreatedAt = new DateOnly(2024, 4, 19)
                },
                new Listener
                {
                    Name = "Павел Сидоров",
                    Email = "pavel.sidorov@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "rs89tu01vw23456789012345678901234567890123456789012345678901rrrr",
                    CreatedAt = new DateOnly(2024, 5, 7)
                },
                new Listener
                {
                    Name = "Наталья Романова",
                    Email = "natasha.romanova@outlook.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "st90uv12wx34567890123456789012345678901234567890123456789012ssss",
                    CreatedAt = new DateOnly(2024, 5, 22)
                },
                new Listener
                {
                    Name = "Владимир Крылов",
                    Email = "vladimir.krylov@yandex.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "tu01vw23xy45678901234567890123456789012345678901234567890123tttt",
                    CreatedAt = new DateOnly(2024, 6, 8)
                },
                new Listener
                {
                    Name = "Ксения Захарова",
                    Email = "ksenia.zakharova@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "uv12wx34yz56789012345678901234567890123456789012345678901234uuuu",
                    CreatedAt = new DateOnly(2024, 6, 25)
                },
                new Listener
                {
                    Name = "Роман Медведев",
                    Email = "roman.medvedev@mail.ru",
                    Status = true,
                    ProfilePictureId = 1,
                    PasswordHash = "vw23xy45za67890123456789012345678901234567890123456789012345vvvv",
                    CreatedAt = new DateOnly(2024, 7, 3)
                },
                new Listener
                {
                    Name = "Виктория Алексеева",
                    Email = "vika.alexeeva@proton.me",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "wx34yz56ab78901234567890123456789012345678901234567890123456wwww",
                    CreatedAt = new DateOnly(2024, 7, 20)
                },
                new Listener
                {
                    Name = "Александр Титов",
                    Email = "alex.titov@yandex.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "xy45za67bc89012345678901234567890123456789012345678901234567xxxx",
                    CreatedAt = new DateOnly(2024, 8, 5)
                },
                new Listener
                {
                    Name = "Полина Григорьева",
                    Email = "polina.grigorieva@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "yz56ab78cd90123456789012345678901234567890123456789012345678yyyy",
                    CreatedAt = new DateOnly(2024, 8, 18)
                },
                new Listener
                {
                    Name = "Кирилл Борисов",
                    Email = "kirill.borisov@outlook.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "za67bc89de01234567890123456789012345678901234567890123456789zzzz",
                    CreatedAt = new DateOnly(2024, 9, 1)
                },
                new Listener
                {
                    Name = "Алина Ковалева",
                    Email = "alina.kovaleva@mail.ru",
                    Status = true,
                    ProfilePictureId = 1,
                    PasswordHash = "ab78cd90ef12345678901234567890123456789012345678901234567890aaab",
                    CreatedAt = new DateOnly(2024, 9, 14)
                },
                new Listener
                {
                    Name = "Денис Соловьев",
                    Email = "denis.soloviev@yandex.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "bc89de01fg23456789012345678901234567890123456789012345678901bbbc",
                    CreatedAt = new DateOnly(2024, 9, 28)
                },
                new Listener
                {
                    Name = "Светлана Павлова",
                    Email = "sveta.pavlova@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "cd90ef12gh34567890123456789012345678901234567890123456789012cccd",
                    CreatedAt = new DateOnly(2024, 10, 10)
                },
                new Listener
                {
                    Name = "Егор Семенов",
                    Email = "egor.semenov@proton.me",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "de01fg23hi45678901234567890123456789012345678901234567890123ddde",
                    CreatedAt = new DateOnly(2024, 10, 22)
                },
                new Listener
                {
                    Name = "Вероника Степанова",
                    Email = "veronika.stepanova@mail.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "ef12gh34ij56789012345678901234567890123456789012345678901234eeef",
                    CreatedAt = new DateOnly(2024, 11, 3)
                },
                new Listener
                {
                    Name = "Глеб Филиппов",
                    Email = "gleb.filippov@yandex.ru",
                    Status = true,
                    ProfilePictureId = 1,
                    PasswordHash = "fg23hi45jk67890123456789012345678901234567890123456789012345fffg",
                    CreatedAt = new DateOnly(2024, 11, 15)
                },
                new Listener
                {
                    Name = "Арина Тарасова",
                    Email = "arina.tarasova@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "gh34ij56kl78901234567890123456789012345678901234567890123456gggh",
                    CreatedAt = new DateOnly(2024, 11, 28)
                },
                new Listener
                {
                    Name = "Тимур Гусев",
                    Email = "timur.gusev@outlook.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "hi45jk67lm89012345678901234567890123456789012345678901234567hhhi",
                    CreatedAt = new DateOnly(2024, 12, 5)
                },
                new Listener
                {
                    Name = "Валерия Киселева",
                    Email = "lera.kiseleva@mail.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "ij56kl78mn90123456789012345678901234567890123456789012345678iiij",
                    CreatedAt = new DateOnly(2024, 12, 18)
                },
                new Listener
                {
                    Name = "Станислав Антонов",
                    Email = "stas.antonov@yandex.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "jk67lm89no01234567890123456789012345678901234567890123456789jjjk",
                    CreatedAt = new DateOnly(2024, 12, 30)
                },
                new Listener
                {
                    Name = "Диана Маркова",
                    Email = "diana.markova@gmail.com",
                    Status = true,
                    ProfilePictureId = 1,
                    PasswordHash = "kl78mn90op12345678901234567890123456789012345678901234567890kkkl",
                    CreatedAt = new DateOnly(2025, 1, 8)
                },
                new Listener
                {
                    Name = "Олег Денисов",
                    Email = "oleg.denisov@proton.me",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "lm89no01pq23456789012345678901234567890123456789012345678901lllm",
                    CreatedAt = new DateOnly(2025, 1, 20)
                },
                new Listener
                {
                    Name = "Кристина Власова",
                    Email = "kristina.vlasova@mail.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "mn90op12qr34567890123456789012345678901234567890123456789012mmmn",
                    CreatedAt = new DateOnly(2025, 2, 1)
                },
                new Listener
                {
                    Name = "Илья Комаров",
                    Email = "ilya.komarov@yandex.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "no01pq23rs45678901234567890123456789012345678901234567890123nnno",
                    CreatedAt = new DateOnly(2025, 2, 12)
                },
                new Listener
                {
                    Name = "Яна Воробьева",
                    Email = "yana.vorobyeva@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "op12qr34st56789012345678901234567890123456789012345678901234ooop",
                    CreatedAt = new DateOnly(2025, 2, 28)
                },
                new Listener
                {
                    Name = "Вадим Пономарев",
                    Email = "vadim.ponomarev@outlook.com",
                    Status = true,
                    ProfilePictureId = 1,
                    PasswordHash = "pq23rs45tu67890123456789012345678901234567890123456789012345pppq",
                    CreatedAt = new DateOnly(2025, 3, 10)
                },
                new Listener
                {
                    Name = "Милана Жукова",
                    Email = "milana.zhukova@mail.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "qr34st56uv78901234567890123456789012345678901234567890123456qqqr",
                    CreatedAt = new DateOnly(2025, 3, 22)
                },
                new Listener
                {
                    Name = "Георгий Ильин",
                    Email = "georgiy.ilin@yandex.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "rs45tu67vw89012345678901234567890123456789012345678901234567rrrs",
                    CreatedAt = new DateOnly(2025, 4, 3)
                },
                new Listener
                {
                    Name = "Софья Виноградова",
                    Email = "sofia.vinogradova@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "st56uv78wx90123456789012345678901234567890123456789012345678ssst",
                    CreatedAt = new DateOnly(2025, 4, 15)
                },
                new Listener
                {
                    Name = "Матвей Громов",
                    Email = "matvey.gromov@proton.me",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "tu67vw89xy01234567890123456789012345678901234567890123456789tttu",
                    CreatedAt = new DateOnly(2025, 4, 28)
                },
                new Listener
                {
                    Name = "Ева Лазарева",
                    Email = "eva.lazareva@mail.ru",
                    Status = true,
                    ProfilePictureId = 1,
                    PasswordHash = "uv78wx90yz12345678901234567890123456789012345678901234567890uuuv",
                    CreatedAt = new DateOnly(2025, 5, 8)
                },
                new Listener
                {
                    Name = "Даниил Фролов",
                    Email = "daniil.frolov@yandex.ru",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "vw89xy01za23456789012345678901234567890123456789012345678901vvvw",
                    CreatedAt = new DateOnly(2025, 5, 20)
                },
                new Listener
                {
                    Name = "Александра Зайцева",
                    Email = "sasha.zaitseva@gmail.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "wx90yz12ab34567890123456789012345678901234567890123456789012wwwx",
                    CreatedAt = new DateOnly(2025, 6, 1)
                },
                new Listener
                {
                    Name = "Богдан Макаров",
                    Email = "bogdan.makarov@outlook.com",
                    Status = false,
                    ProfilePictureId = 1,
                    PasswordHash = "xy01za23bc45678901234567890123456789012345678901234567890123xxxy",
                    CreatedAt = new DateOnly(2025, 6, 12)
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
