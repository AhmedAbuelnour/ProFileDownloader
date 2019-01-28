namespace ProFileDownloader.FileTransferer
{
    using ProFileDownloader.NetworkFile;
    using System;
    using System.IO;
    using System.IO.Pipelines;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    /// <summary>
    /// A class which represents a file downloader
    /// </summary>
    public class Downloader
    {
        internal string Url { get; set; }

        internal string DirectoryPath { get; set; }

        /// <summary>
        /// Giving a name to overwrite the remote suggested file name.
        /// </summary>
        public string SuggestedFileName { get; set; }

        internal ServerFile RemoteFileProperties { get; set; }

        internal string LocalFileFullPath { get; set; }

        /// <summary>
        /// Initialize the downloader.
        /// </summary>
        /// <param name="Url">Url of the remote file.</param>
        /// <param name="DirectoryPath">Where the local file should be saved at.</param>
        public Downloader(string Url , string DirectoryPath)
        {
            this.Url = Url;
            this.DirectoryPath = DirectoryPath;
        }
        private async Task<bool> IsResumable()
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(1, 1);
                    using (HttpResponseMessage Result = await httpClient.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (Result.StatusCode == HttpStatusCode.PartialContent)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Giving you a readable format for the remote file size.
        /// </summary>
        public string SizeInReadableFormat => RemoteFileProperties.Size.SizeSuffix();

        /// <summary>
        /// Loads the downloader with the required information about the remote file.
        /// </summary>
        /// <returns></returns>
        public async Task LoadRemoteFilePropertiesAsync()
        {
            using (HttpClient httpClient = new HttpClient())
            {
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
                    IsResumable = await IsResumable(),
                    DownloadContent = await httpResponseMessage.Content.ReadAsStreamAsync(),
                    TotalReadBytes = 0
                };
            }

            LocalFileFullPath = $"{DirectoryPath}/{SuggestedFileName ?? RemoteFileProperties.Name}";
        }

        /// <summary>
        /// Indicates if the remote server supports the file segmentation or not
        /// </summary>
        public bool IsRemoteServerSupportFileSegmentaion => RemoteFileProperties.IsResumable;
        /// <summary>
        /// Indicates if the remote server supports the file resuming or not
        /// </summary>
        public bool IsRemoteServerSupportResuming => RemoteFileProperties.IsResumable;


        /// <summary>
        /// Updates the downloader with the new changes that happened to the remote file, to match the least changes to be able to resume downloading the file.
        /// </summary>
        /// <returns>
        /// Updates the remote file properties.
        /// </returns>
        public async Task UpdateRemoteFilePropertiesForResuming()
        {
            if (IsRemoteServerSupportResuming)
            {
                using(Stream localFile = new FileStream(LocalFileFullPath, FileMode.Open, FileAccess.Read))
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
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
        public async Task DownloadFileAsync(Action<float> CurrentProgress, CancellationToken cancellationToken = default)
        {
            Pipe Pipeline = new Pipe();

            await Task.WhenAll
                (
                    Task.Run(async() => 
                    {
                        int bytesRead;
                        while ((bytesRead = await RemoteFileProperties.DownloadContent.ReadAsync(Pipeline.Writer.GetMemory(), cancellationToken)) > 0) // Where the downloading part is happening
                        {
                            CurrentProgress.Invoke(((RemoteFileProperties.TotalReadBytes += bytesRead) / (float)RemoteFileProperties.Size) * 100); // To Get the current percentage.
                            Pipeline.Writer.Advance(bytesRead);
                            var flushResult = await Pipeline.Writer.FlushAsync(cancellationToken);
                            if (flushResult.IsCanceled) break;
                            if (flushResult.IsCompleted) break;
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
                                {
                                    await LocalFile.WriteAsync(segment, cancellationToken);
                                }

                                Pipeline.Reader.AdvanceTo(ReadResult.Buffer.End);

                                if (ReadResult.IsCompleted || ReadResult.IsCanceled)
                                {
                                    break;
                                }
                            }
                        }
                        Pipeline.Reader.Complete();
                    })
                );
        }
     
    }
}
