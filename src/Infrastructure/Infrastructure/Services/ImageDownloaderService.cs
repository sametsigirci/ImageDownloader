using Domain.Interfaces;
using System.Net;

namespace Infrastructure.Services
{
    public class ImageDownloaderService : IImageDownloaderService
    {
        private readonly List<string> downloadedImages = new List<string>();
        private readonly object lockObject = new object();

        public event EventHandler<(int count, int current)> ImageDownloaded;

        public async void DownloadImages(int count, int parallelism, string savePath, CancellationTokenSource cancellationTokenSource)
        {
            using var semaphore = new SemaphoreSlim(parallelism);
            var tasks = new List<Task>();

            for (int i = 1; i <= count; i++)
            {
                semaphore.Wait();

                var task = DownloadImageAsync(i, count, savePath)
                    .ContinueWith(_ => semaphore.Release());

                tasks.Add(task);
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;
            }
            
            await Task.WhenAll(tasks);

            if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                foreach (var imageName in downloadedImages)
                {
                    var imagePath = Path.Combine(savePath, imageName);
                    if (File.Exists(imagePath))
                        File.Delete(imagePath);
                }

                Console.WriteLine("Image download canceled. Cleaned up the downloaded images.");
            }
            else
                Console.WriteLine("All images downloaded successfully.");            
        }

        private async Task DownloadImageAsync(int imageNumber, int count, string savePath)
        {
            var imageUrl = "https://picsum.photos/200/300";
            var fileName = $"{imageNumber}.png";
            var filePath = Path.Combine(savePath, fileName);

            using (var client = new WebClient())
            {
                try
                {
                    await client.DownloadFileTaskAsync(imageUrl, filePath);

                    lock (lockObject)
                    {
                        downloadedImages.Add(fileName);
                    }

                    OnImageDownloaded(count,imageNumber);
                }
                catch (WebException ex)
                {
                    Console.WriteLine($"Error downloading image {imageNumber}: {ex.Message}");
                }
            }
        }

        private void OnImageDownloaded(int count, int current)
        {
            ImageDownloaded?.Invoke(this, (count, current));
        }
    }
}
