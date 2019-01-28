namespace ProFileDownloader.NetworkFile
{
    using System;
    using System.IO;
    using System.IO.Pipelines;
    using System.Threading;
    using System.Threading.Tasks;
    internal static class FileSegmentationExtensions
    {
        internal static async Task DownloadSegmentAsync(this FileSegment segment, Action<float> CurrentProgress, CancellationToken cancellationToken = default)
        {
            Pipe Pipeline = new Pipe();

            await Task.WhenAll(Task.Run(async () =>
            {
                int bytesRead;
                while ((bytesRead = await segment.PartialContent.ReadAsync(Pipeline.Writer.GetMemory(), cancellationToken)) > 0) // Where the downloading part is happening
                {
                    segment.TotalReadBytes += bytesRead;
                    CurrentProgress.Invoke
                    (
                         ((((segment.Start + segment.TotalReadBytes) - segment.Start) / ((float)(segment.End - segment.Start))) * 100)
                    ); // To Get the current percentage.
                    Pipeline.Writer.Advance(bytesRead);
                    var flushResult = await Pipeline.Writer.FlushAsync(cancellationToken);
                    if (flushResult.IsCanceled) break;
                    if (flushResult.IsCompleted) break;
                }
                Pipeline.Writer.Complete();
            }, cancellationToken)
                , Task.Run(async () =>
                {
                    using (Stream TempLocalFile = new FileStream(segment.TempFile, FileMode.Append, FileAccess.Write))
                    {
                        TempLocalFile.Seek(TempLocalFile.Length, SeekOrigin.Begin);
                        while (true)
                        {
                            var ReadResult = await Pipeline.Reader.ReadAsync(cancellationToken);
                            foreach (var buffer in ReadResult.Buffer)
                            {
                                await TempLocalFile.WriteAsync(buffer, cancellationToken);
                            }

                            Pipeline.Reader.AdvanceTo(ReadResult.Buffer.End);

                            if (ReadResult.IsCompleted || ReadResult.IsCanceled)
                            {
                                break;
                            }
                        }
                    }
                    Pipeline.Reader.Complete();
                }, cancellationToken));
        }
    }
}
