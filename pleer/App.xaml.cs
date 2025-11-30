using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using pleer.Models.Jamendo;
using pleer.Resources.Windows;
using System.Diagnostics;
using System.Windows;

namespace pleer
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        // api-ключ Jamendo
        private const string JAMENDO_CLIENT_ID = "99575e94";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnLastWindowClose;

            try
            {
                ConfigureServices();
                var mainWindow = Services.GetRequiredService<ListenerMainWindow>();

                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка запуска приложения:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Shutdown();
            }
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddMemoryCache();

            // Проверь что ключ правильный
            Debug.WriteLine($"API Key: {JAMENDO_CLIENT_ID}");

            services.AddSingleton<IMusicService>(sp =>
            {
                var cache = sp.GetRequiredService<IMemoryCache>();
                var service = new JamendoService(JAMENDO_CLIENT_ID, cache);
                Debug.WriteLine("JamendoService создан");
                return service;
            });

            // Window
            services.AddSingleton<ListenerMainWindow>();

            Services = services.BuildServiceProvider();
        }
    }

}