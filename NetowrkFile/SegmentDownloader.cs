namespace ProFileDownloader.NetowrkFile
{
    using ProFileDownloader.Utilities;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipelines;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    /// <summary>
    /// A class which represents a file downloader
    /// </summary>
    public class SegmentDownloader
    {
        internal bool IsDownloaded { get; set; }
        string Url { get; set; }
        internal ServerSegment RemoteSegmentProperties { get; set; }
        internal (long Start, long End) DownloadRange { get; set; }
        Stopwatch SWatch { get; set; } = new Stopwatch();    // The stopwatch which we will be using to calculate the download speed

        /// <summary>
        /// Initialize the downloader.
        /// </summary>
        /// <param name="url">Url of the remote file.</param>
        /// <param name="start">Start Segment's Position</param>
        /// <param name="end">End Segment's Position</param>
        public SegmentDownloader(string url, long start, long end)
        {
            Url = url;
            DownloadRange = (start, end);
        }

        /// <summary>
        /// Initialize the downloader.
        /// </summary>
        /// <param name="url">Url of the remote file.</param>
        /// <param name="serverSegment">Load the Segment info</param>
        public SegmentDownloader(string url, ServerSegment serverSegment)
        {
            Url = url;
            RemoteSegmentProperties = serverSegment;
            DownloadRange = (serverSegment.Start, serverSegment.End);
        }
        /// <summary>
        /// Giving you a readable format for the remote file size.
        /// </summary>
        public string SizeInReadableFormat => RemoteSegmentProperties?.Size.SizeSuffix() ?? throw new Exception("You have to Call LoadRemoteFilePropertiesAsync(), to load the file properties first");

        /// <summary>
        /// Loads the downloader with the required information about the remote file.
        /// </summary>
        /// <returns></returns>
        internal async Task LoadSegmentPropertiesAsync()
        {

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                if (string.IsNullOrWhiteSpace(Url)) throw new ArgumentNullException("Url", "Can't let Url to be empty!");

                // Validation for https and http
                if (!(Url.StartsWith("https://") || Url.StartsWith("http://"))) throw new Exception("Only Support Http, Https protocols");

                httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(DownloadRange.Start, DownloadRange.End);

                // Sends Http Get Request
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);

                if (httpResponseMessage.IsSuccessStatusCode == false)
                {
                    throw new Exception(httpResponseMessage.ReasonPhrase);
                }

                RemoteSegmentProperties = new ServerSegment
                {
                    Size = httpResponseMessage.Content.Headers.ContentLength.GetValueOrDefault(),
                    DownloadContent = await httpResponseMessage.Content.ReadAsStreamAsync(),
                    TotalReadBytes = 0,
                    LocalTempFileLocation = Path.GetTempFileName(),
                    Start = DownloadRange.Start,
                    End = DownloadRange.End
                };

            }
        }


        /// <summary>
        /// Updates the downloader with the new changes that happened to the remote file, to match the least changes to be able to resume downloading the file.
        /// </summary>
        /// <returns>
        /// Updates the remote file properties.
        /// </returns>
        internal async Task UpdateSegmentPropertiesForResumingAsync()
        {
            using (Stream localFile = new FileStream(RemoteSegmentProperties.LocalTempFileLocation, FileMode.Open, FileAccess.Read))
            {
                if((localFile.Length >= DownloadRange.End)) // The segment is fully downloaded before.
                {
                    IsDownloaded = true;
                    return;
                }
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(localFile.Length  + DownloadRange.Start, DownloadRange.End);
                    RemoteSegmentProperties.TotalReadBytes = localFile.Length;
                    RemoteSegmentProperties.DownloadContent = await httpClient.GetStreamAsync(Url);
                }
            }
        }
        /// <summary>
        /// Download the remote file.
        /// </summary>
        /// <param name="CurrentProgress">Gets the current downloading process</param>
        /// <param name="cancellationToken">A token to cancel the downloading process if you want.</param>
        /// <returns></returns>
        public void DownloadSegment(Action<DownloadInfo> CurrentProgress, CancellationToken cancellationToken = default)
        {
            if (!IsDownloaded)
            {
                Pipe Pipeline = new Pipe();

                SWatch.Start();

                Task.WaitAll(
                    Task.Run(async () =>
                    {
                        int bytesRead;
                        while ((bytesRead = await RemoteSegmentProperties.DownloadContent.ReadAsync(Pipeline.Writer.GetMemory(), cancellationToken)) > 0) // Where the downloading part is happening
                    {
                            CurrentProgress.Invoke(new DownloadInfo
                            {
                                CurrentPercentage = ((RemoteSegmentProperties.TotalReadBytes += bytesRead) / (float)RemoteSegmentProperties.Size) * 100, // Gets the Current Percentage
                            DownloadSpeed = Convert.ToInt64((RemoteSegmentProperties.TotalReadBytes / SWatch.Elapsed.TotalSeconds)).SizeSuffix(), // Get The Current Speed
                            DownloadedProgress = string.Format("{0} MB's / {1} MB's", (RemoteSegmentProperties.TotalReadBytes / 1024d / 1024d).ToString("0.00"), (RemoteSegmentProperties.Size / 1024d / 1024d).ToString("0.00")) // Get How much has been downloaded
                        });

                            Pipeline.Writer.Advance(bytesRead);
                            var flushResult = await Pipeline.Writer.FlushAsync(cancellationToken);
                            if (flushResult.IsCanceled || flushResult.IsCompleted) break;
                        }
                        Pipeline.Writer.Complete();
                    }),
                    Task.Run(async () =>
                    {
                        using (Stream LocalFile = new FileStream(RemoteSegmentProperties.LocalTempFileLocation, FileMode.Append, FileAccess.Write))
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
}
