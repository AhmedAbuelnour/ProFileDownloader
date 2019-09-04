namespace ProFileDownloader.NetowrkFile
{
    using ProFileDownloader.Utilities;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipelines;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    /// <summary>
    /// A class which represents a file downloader
    /// </summary>
    public class FileDownloader
    {
        string Url { get; set; }
        string DirectoryPath { get; set; }
        string SuggestedFileName { get; set; }
        ServerFile RemoteFileProperties { get; set; }
        string LocalFileFullPath { get; set; }
        Stopwatch SWatch { get; set; } = new Stopwatch();    // The stopwatch which we will be using to calculate the download speed
        /// <summary>
        /// Initialize the downloader.
        /// </summary>
        /// <param name="url">Url of the remote file.</param>
        /// <param name="directoryPath">Where the local file should be saved at.</param>
        /// <param name="suggestedName">Giving a name and extension to overwrite the remote suggested file name.</param>
        public FileDownloader(string url, string directoryPath, string suggestedName = default)
        {
            Url = url;
            DirectoryPath = directoryPath;
            SuggestedFileName = suggestedName;
        }
        /// <summary>
        /// Giving you a readable format for the remote file size.
        /// </summary>
        public string SizeInReadableFormat => RemoteFileProperties?.Size.SizeSuffix() ?? throw new Exception("You have to Call LoadRemoteFilePropertiesAsync(), to load the file properties first");
        /// <summary>
        /// Indicates if the remote server supports the file segmentation or not
        /// </summary>
        public bool IsRemoteServerSupportFileSegmentaion => RemoteFileProperties?.IsResumable ?? throw new Exception("You have to Call LoadRemoteFilePropertiesAsync(), to load the file properties first");
        /// <summary>
        /// Indicates if the remote server supports the file resuming or not
        /// </summary>
        public bool IsRemoteServerSupportResuming => RemoteFileProperties?.IsResumable ?? throw new Exception("You have to Call LoadRemoteFilePropertiesAsync(), to load the file properties first");
        /// <summary>
        /// Loads the downloader with the required information about the remote file.
        /// </summary>
        /// <returns></returns>
        public async Task LoadRemoteFilePropertiesAsync()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                if (string.IsNullOrWhiteSpace(Url)) throw new ArgumentNullException("Url", "Can't let Url to be empty!");

                // Validation for https and http
                if (!(Url.StartsWith("https://") || Url.StartsWith("http://"))) throw new Exception("Only Support Http, Https protocols");

                // Sends Http Get Request
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);

                if (httpResponseMessage.IsSuccessStatusCode == false)
                {
                    throw new Exception(httpResponseMessage.ReasonPhrase);
                }
                RemoteFileProperties = new ServerFile
                {
                    Name = httpResponseMessage.Content.Headers?.ContentDisposition?.FileName ?? httpResponseMessage.RequestMessage.RequestUri.Segments.LastOrDefault(),
                    MediaType = httpResponseMessage.Content.Headers.ContentType.MediaType,
                    Size = httpResponseMessage.Content.Headers.ContentLength.GetValueOrDefault(),
                    Extension = httpResponseMessage.Content.Headers.ContentType.MediaType.GetFileExtension(),
                    IsResumable = await DownloaderHelper.IsResumable(Url),
                    DownloadContent = await httpResponseMessage.Content.ReadAsStreamAsync(),
                    TotalReadBytes = 0
                };
            }

            LocalFileFullPath = $"{DirectoryPath}/{SuggestedFileName ?? RemoteFileProperties.Name}";
        }
        /// <summary>
        /// Updates the downloader with the new changes that happened to the remote file, to match the least changes to be able to resume downloading the file.
        /// </summary>
        /// <returns>
        /// Updates the remote file properties.
        /// </returns>
        public async Task UpdateRemoteFilePropertiesForResumingAsync()
        {
            if (IsRemoteServerSupportResuming)
            {
                using (Stream localFile = new FileStream(LocalFileFullPath, FileMode.Open, FileAccess.Read))
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(10);
                        httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(localFile.Length, null);
                        RemoteFileProperties.TotalReadBytes = localFile.Length;
                        RemoteFileProperties.DownloadContent = await httpClient.GetStreamAsync(Url);
                    }
                }
            }
        }
        /// <summary>
        /// Download the remote file.
        /// </summary>
        /// <param name="CurrentProgress">Gets the current downloading process</param>
        /// <param name="cancellationToken">A token to cancel the downloading process if you want.</param>
        /// <returns></returns>
        public void DownloadFile(Action<DownloadInfo> CurrentProgress, CancellationToken cancellationToken = default)
        {
            Pipe Pipeline = new Pipe();

            SWatch.Start();

            Task.WaitAll(
                Task.Run(async () =>
                {
                    int bytesRead;
                    while ((bytesRead = await RemoteFileProperties.DownloadContent.ReadAsync(Pipeline.Writer.GetMemory(), cancellationToken)) > 0) // Where the downloading part is happening
                    {
                        CurrentProgress.Invoke(new DownloadInfo
                        {
                            CurrentPercentage = ((RemoteFileProperties.TotalReadBytes += bytesRead) / (float)RemoteFileProperties.Size) * 100, // Gets the Current Percentage
                            DownloadSpeed = Convert.ToInt64((RemoteFileProperties.TotalReadBytes / SWatch.Elapsed.TotalSeconds)).SizeSuffix(), // Get The Current Speed
                            DownloadedProgress = string.Format("{0} MB's / {1} MB's", (RemoteFileProperties.TotalReadBytes / 1024d / 1024d).ToString("0.00"), (RemoteFileProperties.Size / 1024d / 1024d).ToString("0.00")) // Get How much has been downloaded
                        });

                        Pipeline.Writer.Advance(bytesRead);
                        var flushResult = await Pipeline.Writer.FlushAsync(cancellationToken);
                        if (flushResult.IsCanceled || flushResult.IsCompleted) break;
                    }
                    Pipeline.Writer.Complete();
                }), 
                Task.Run(async () =>
                {
                    using (Stream LocalFile = new FileStream(LocalFileFullPath, FileMode.Append, FileAccess.Write))
                    {
                        LocalFile.Seek(LocalFile.Length, SeekOrigin.Begin);
                        while (true)
                        {
                            var ReadResult = await Pipeline.Reader.ReadAsync(cancellationToken);
                            foreach (var segment in ReadResult.Buffer)
                                await LocalFile.WriteAsync(segment, cancellationToken);

                            Pipeline.Reader.AdvanceTo(ReadResult.Buffer.End);

                            if (ReadResult.IsCompleted || ReadResult.IsCanceled) break;

                        }
                    }
                    Pipeline.Reader.Complete();
                }));

            SWatch.Stop();
            SWatch.Reset();
        }
    }
}
