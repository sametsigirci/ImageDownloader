namespace Domain.Events
{
    public delegate void ImageDownloadedEventHandler(object sender, ImageDownloadedEventArgs e);

    public class ImageDownloadedEventArgs : EventArgs
    {
        public string FileName { get; set; }
    }
}
