using Application.Services;
using Domain.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp
{
    public class Program
    {
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        public static void Main(string[] args)
        {
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Cancellation requested. Cleaning up...");
                CancellationTokenSource.Cancel();
                e.Cancel = true;
            };

            var serviceProvider = ConfigureServices();
            using (var scope = serviceProvider.CreateScope())
            {
                var inputReaderService = scope.ServiceProvider.GetRequiredService<IInputReader>();
                var input= inputReaderService.ReadInputFromJson("Input.json");

                var downloadService = scope.ServiceProvider.GetRequiredService<IImageDownloaderService>();

                downloadService.ImageDownloaded += ImageDownloaded;

                downloadService.DownloadImages(input.Count, input.Parallelism,input.SavePath,CancellationTokenSource);

                Console.WriteLine("Download completed. Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void ImageDownloaded(object? sender, (int count, int current) downloaded)
        {
            Console.Write($"\rProgress: {downloaded.current}/{downloaded.count}");
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddScoped<IImageDownloaderService, ImageDownloaderService>();
            services.AddScoped<IInputReader, InputReaderService>();

            return services.BuildServiceProvider();
        }

    }

}
