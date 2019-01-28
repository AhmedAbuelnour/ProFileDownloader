using Newtonsoft.Json;
using ProFileDownloader.NetworkFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ProFileDownloader.FileTransferer
{
    /// <summary>
    /// A class which represents segments downloader
    /// </summary>
    public class FileSegmentaionDownloader : Downloader
    {
        /// <summary>
        /// Initialize the segment downloader.
        /// </summary>
        /// <param name="Url">Url of the remote file.</param>
        /// <param name="DirectoryPath">Where the local file should be saved at.</param>
        public FileSegmentaionDownloader(string Url, string DirectoryPath) : base(Url, DirectoryPath)
        {
            FileSegments = new List<FileSegment>(Environment.ProcessorCount);
            FileSegmentaionTasks = new List<Task>(Environment.ProcessorCount);
        }

        internal IList<FileSegment> FileSegments { get; set; }

        internal IList<Task> FileSegmentaionTasks { get; set; }

        private IEnumerable<(long Start, long End)> SegmentPosition(long ContentLength, int ChunksNumber = 8)
        {
            long PartSize = (long)Math.Ceiling(ContentLength / (double)ChunksNumber);
            for (var i = 0; i < ChunksNumber; i++)
                yield return (i * PartSize + Math.Min(1, i), Math.Min((i + 1) * PartSize, ContentLength));
        }


        // 1. Gets the file segments and load them.
        /// <summary>
        /// Load the segments of the remote file.
        /// </summary>
        /// <returns>List of segments are stored</returns>
        public async Task LoadFileSegmentsAsync()
        {
            int Count = 0;
            foreach (var (Start, End) in SegmentPosition(RemoteFileProperties.Size, Environment.ProcessorCount))
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(Start, End);

                    FileSegments.Add(new FileSegment
                    {
                        ID = ++Count,
                        PartialContent = await httpClient.GetStreamAsync(Url),
                        Start = Start,
                        End = End,
                        TotalReadBytes = 0
                    });
                }
            }
        }

        // 1.1 Get The Collection Of Paths of the temp files, Store it, in whatever place you like, to reade it again later.
        /// <summary>
        /// Gets the segments basic data, to be stored on your own, for later resuming needs.
        /// </summary>
        /// <returns>Json Content, which is need to be stored in somewhere on your own.</returns>
        public string GetBasicSegmentsInfo()=>
             JsonConvert.SerializeObject(FileSegments);


        // 1.2 Load the segments info for resuming

        /// <summary>
        /// Load the Segments data from the JsonContent
        /// </summary>
        /// <param name="JsonContent">Json Content, which you got from the <see cref="GetBasicSegmentsInfo()"/>  </param>
        /// <returns>reloaded segments to be able to resumed</returns>
        public async Task LoadFileSegmentsForResumingAsync(string JsonContent)
        {
            foreach (var fileSegment in JsonConvert.DeserializeObject<IList<FileSegment>>(JsonContent))
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    using (Stream tempfile = new FileStream(fileSegment.TempFile, FileMode.Open, FileAccess.Read))
                    {
                        httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(fileSegment.Start + tempfile.Length, fileSegment.End);
                        FileSegments.Add(new FileSegment
                        {
                            ID = fileSegment.ID,
                            Start = fileSegment.Start,
                            End = fileSegment.End,
                            TempFile = fileSegment.TempFile,
                            TotalReadBytes = tempfile.Length,
                            PartialContent = await httpClient.GetStreamAsync(Url)
                        });
                    }
                }
            }
        }



        // 2. Start Downloading the segments
        /// <summary>
        /// Start downloading the segments
        /// </summary>
        /// <param name="CurrentProgress">Gets the current downloading process</param>
        /// <param name="cancellationToken">A token to cancel the downloading process if you want.</param>
        /// <returns></returns>
        public async Task DownloadFileSegmensAsync(Action<float> CurrentProgress, CancellationToken cancellationToken = default)
        {
            float[] Percentage = new float[Environment.ProcessorCount];
            foreach (var segment in FileSegments)
            {
                FileSegmentaionTasks.Add(segment.DownloadSegmentAsync((e)=> {

                    Percentage[segment.ID - 1] = e;

                    CurrentProgress.Invoke(Percentage.Average());
                }, cancellationToken));
            }
            await Task.WhenAll(FileSegmentaionTasks);
        }


        // 3. Reconstracut the segments
        /// <summary>
        /// Reconstract the segments from the temp files that got created.
        /// </summary>
        /// <returns>A complete file in the directory that you specified in the constructor.</returns>
        public async Task ReconstructSegmentsAsync()
        {
            using (Stream localFileStream = new FileStream(LocalFileFullPath, FileMode.Create, FileAccess.ReadWrite))
            {
                foreach (var Segment in FileSegments.OrderBy(x => x.ID))
                {
                    localFileStream.Seek(Segment.Start, SeekOrigin.Begin);

                    using (Stream tempStream = new FileStream(Segment.TempFile, FileMode.Open, FileAccess.Read))
                    {
                        await tempStream.CopyToAsync(localFileStream);
                    }
                }
            }
            // Delete all the Temp files, after the reconstraction process.
            foreach (var Segment in FileSegments)
            {
                File.Delete(Segment.TempFile);
            }
        }



    }
}



