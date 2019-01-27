namespace ProFileDownloader.FileTransferer
{
    using ProFileDownloader.NetworkFile;
    using System;
    using System.IO;
    using System.IO.Pipelines;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// Responsible for downloading a file in an easy way
    /// </summary>
    public class FileDownloader
    {

        private readonly Pipe Pipeline;
        /// <summary>
        /// Initialize the Pipe
        /// </summary>
        public FileDownloader()
        {
            Pipeline = new Pipe();
        }

        /// <summary>
        /// Represents your local file, which represents the downloaded file at the end.
        /// </summary>
        public string LocalFilePath { get; set; }


        /// <summary>
        /// Download a file async.
        /// </summary>
        /// <param name="fileProperties">Represents the properties of the remote file</param>
        /// <param name="CurrentProgress">Gives you real-time download Current Progress</param>
        /// <param name="cancellationToken">Responsible for cancelling the current download process</param>
        public async Task DownloadFileAsync(ServerFile fileProperties, Action<float> CurrentProgress, CancellationToken cancellationToken = default)
        {
            FileProperites localFile = FileUtilities.GetFileProperites(LocalFilePath);

            if(localFile.Length >= fileProperties.Size) // it got been downloaded before!
            {
                return;
            }
            else if(localFile.Length == 0) // Nothing has been downloaded before!
            {
                await DownloadAsync(fileProperties, CurrentProgress, cancellationToken);
            }
            else // It needs resuming
            {
                if (fileProperties.IsResumable)
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(localFile.Length, null);
                        fileProperties.TotalReadBytes = localFile.Length;
                        fileProperties.DownloadContent = await httpClient.GetStreamAsync(fileProperties.Url);
                    }

                    await DownloadAsync(fileProperties, CurrentProgress, cancellationToken);
                }
                else
                {
                    File.Delete(LocalFilePath); // Delete the file, then start downloading over
                    await DownloadAsync(fileProperties, CurrentProgress, cancellationToken);
                }
            }
        }

        private async Task DownloadAsync(ServerFile fileProperties, Action<float> CurrentProgress, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll
                     (
                             WriterFileAsync(fileProperties, CurrentProgress, cancellationToken),
                             ConstructFileAsync(cancellationToken)
                     );
        }
        private async Task WriterFileAsync(ServerFile fileProperties, Action<float> CurrentProgress, CancellationToken cancellationToken = default)
        {
            try
            {
                int bytesRead;
                while ((bytesRead = await fileProperties.DownloadContent.ReadAsync(Pipeline.Writer.GetMemory(), cancellationToken)) > 0) // Where the downloading part is happening
                {
                    CurrentProgress.Invoke(((fileProperties.TotalReadBytes += bytesRead) / (float)fileProperties.Size) * 100); // To Get the current percentage.
                    Pipeline.Writer.Advance(bytesRead);
                    var flushResult = await Pipeline.Writer.FlushAsync(cancellationToken);
                    if (flushResult.IsCanceled) break;
                    if (flushResult.IsCompleted) break;
                }
                Pipeline.Writer.Complete();
            }
            catch
            {
                throw new Exception("Downloading Process has been stoped!");
            }
        }
        private async Task ConstructFileAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using (Stream LocalFile = new FileStream(LocalFilePath, FileMode.Append, FileAccess.Write))
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
            }
            catch
            {
                throw new Exception("Writing the data to your local computer has been stoped!");
            }
        }
      
    }
}
