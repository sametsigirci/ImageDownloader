using System.Management;

namespace Domain.Interfaces
{
    public interface IImageDownloaderService
    {
        event EventHandler<(int count,int current)> ImageDownloaded;
        void DownloadImages(int count, int parallelism, string savePath, CancellationTokenSource cancellationTokenSource);
    }
}
