using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;

namespace v2
{
    public class Program
    {
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private static readonly object ConsoleLock = new object();
        private static int _downloadedCount = 0;

        public static async Task Main()
        {
            try
            {
                Console.CancelKeyPress += (s, e) =>
                {
                    Console.WriteLine("Cancellation requested. Cleaning up...");
                    CancellationTokenSource.Cancel();
                    e.Cancel = true;
                };

                var input = ReadInputFromJson("Input.json");

                var imageDownloader = new ImageDownloader();
                await imageDownloader.StartDownloadAsync(input.Count, input.Parallelism, input.SavePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            Console.ReadLine();
        }

        private static Input ReadInputFromJson(string inputFilePath)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException("Input.json file not found.");

            var json = File.ReadAllText(inputFilePath);
            return JsonConvert.DeserializeObject<Input>(json);
        }

        private class Input
        {
            public int Count { get; set; }
            public int Parallelism { get; set; }
            public string SavePath { get; set; }
        }
    }

    public class ImageDownloader
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly object _consoleLock = new object();
        private int _downloadedCount = 0;

        public async Task StartDownloadAsync(int count, int parallelism, string savePath)
        {
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            var downloadedImages = new ConcurrentDictionary<int, string>();
            var tasks = new List<Task>();

            for (int i = 1; i <= count; i++)
            {

                var index = i;
                var task = DownloadImageAsync(index, savePath, downloadedImages);

                if (tasks.Count == parallelism)
                {
                    //var completedTask = await Task.WhenAny(tasks);
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                    await Task.Delay(5000);
                }
                else
                    tasks.Add(task);

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;
            }

            await Task.WhenAll(tasks);

            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await CleanUpDownloadedImagesAsync(downloadedImages, savePath);
                Console.WriteLine("Image download canceled. Cleaned up the downloaded images.");
            }
            else
            {
                Console.WriteLine("All images downloaded successfully.");
            }
        }

        private async Task DownloadImageAsync(int index, string savePath, ConcurrentDictionary<int, string> downloadedImages)
        {
            var imageName = $"{index}.png";
            var imageUrl = "https://picsum.photos/200/300";
            await DownloadImageAndSaveAsync(imageUrl, Path.Combine(savePath, imageName));

            downloadedImages.TryAdd(index, imageName);

            lock (_consoleLock)
            {
                //Console.SetCursorPosition(0, index);
                Console.Write($"\rImage {index}/{downloadedImages.Count} downloaded");
            }

            Interlocked.Increment(ref _downloadedCount);
        }

        private async Task DownloadImageAndSaveAsync(string imageUrl, string savePath)
        {
            using (var webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(new Uri(imageUrl), savePath);
            }
        }

        private async Task CleanUpDownloadedImagesAsync(ConcurrentDictionary<int, string> downloadedImages, string savePath)
        {
            foreach (var imageName in downloadedImages.Values)
            {
                var imagePath = Path.Combine(savePath, imageName);
                if (File.Exists(imagePath))
                    File.Delete(imagePath);
            }
        }
    }
}
