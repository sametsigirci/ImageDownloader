using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;

namespace TestApp
{
    public class Program
    {
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private static readonly object ConsoleLock = new object();
        private static int _downloadedCount = 0;

        public static void Main()
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
                StartImageDownload(input.Count, input.Parallelism, input.SavePath).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static async Task StartImageDownload(int count, int parallelism, string savePath)
        {
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            var downloadedImages = new ConcurrentDictionary<int, string>();
            
            var tasks = new List<Task>();
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(500);
                var index = i;
                var task = Task.Run(async () =>
                {

                    var imageName = $"{index + 1}.png";
                    var imageUrl = "https://picsum.photos/200/300";
                    await DownloadImage(imageUrl, Path.Combine(savePath, imageName));

                    downloadedImages.TryAdd(index, imageName);

                    lock (ConsoleLock)
                    {
                        //Console.SetCursorPosition(0, index);
                        Console.Write($"\rImage {index + 1}/{count} downloaded");
                    }

                    Interlocked.Increment(ref _downloadedCount);
                });

                tasks.Add(task);

                if (tasks.Count == parallelism)
                {
                    var completedTask = await Task.WhenAny(tasks);
                    tasks.Remove(completedTask);
                }

                if (CancellationTokenSource.Token.IsCancellationRequested)
                    break;
            }

            await Task.WhenAll(tasks);

            if (CancellationTokenSource.Token.IsCancellationRequested)
            {
                foreach (var imageName in downloadedImages.Values)
                {
                    var imagePath = Path.Combine(savePath, imageName);
                    if (File.Exists(imagePath))
                        File.Delete(imagePath);
                }

                Console.WriteLine("Image download canceled. Cleaned up the downloaded images.");
            }
            else
            {
                Console.WriteLine("All images downloaded successfully.");
            }
        }

        private static async Task DownloadImage(string imageUrl, string savePath)
        {
            using (var webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(new Uri(imageUrl), savePath);
            }
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
}