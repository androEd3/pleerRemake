using Microsoft.Extensions.DependencyInjection;
using pleer.Models.IA;
using System.Windows;

namespace pleer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);

            Services = services.BuildServiceProvider();
        }

        public static IServiceProvider Services { get; private set; }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMusicService, InternetArchiveService>();
        }
    }
}