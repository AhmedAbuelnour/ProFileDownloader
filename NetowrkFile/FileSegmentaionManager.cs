using ProFileDownloader.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProFileDownloader.NetowrkFile
{
    /// <summary>
    /// A class which represents segments downloader
    /// </summary>
    public class FileSegmentaionManager
    {
        string Url { get; set; }
        string DirectoryPath { get; set; }
        string SuggestedFileName { get; set; }
        string LocalFileFullPath { get; set; }
        int SegmentNumbers = 0;
        ServerFile RemoteFileProperties { get; set; }
        /// <summary>
        /// Represent the segments, which are downloadable.
        /// </summary>
        public List<SegmentDownloader> SegmentDownloaders { get; set; }
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
        /// Initialize the segment downloader manager.
        /// </summary>
        /// <param name="url">The file url</param>
        /// <param name="directoryPath">where you want to store your file</param>
        /// <param name="segmentNumbers">How many segments</param>
        /// <param name="suggestedName">the file name and its extension</param>
        public FileSegmentaionManager(string url, string directoryPath, int segmentNumbers = 8, string suggestedName = default)
        {
            Url = url;
            DirectoryPath = directoryPath;
            SuggestedFileName = suggestedName;
            SegmentNumbers = segmentNumbers;
        }



       /// <summary>
       /// It fills SegmentDownloads Property with the segments.
       /// </summary>
        public async Task GenerateSegmentsAsync()
        {
            IEnumerable<(long Start, long End)> SegmentPosition(long ContentLength, int ChunksNumber)
            {
                long PartSize = (long)Math.Ceiling(ContentLength / (double)ChunksNumber);
                for (var i = 0; i < ChunksNumber; i++)
                    yield return (i * PartSize + Math.Min(1, i), Math.Min((i + 1) * PartSize, ContentLength));
            }
            async Task<List<SegmentDownloader>> LoadSegnmentsAsync()
            {
                List<SegmentDownloader> segments = new List<SegmentDownloader>(SegmentNumbers);

                foreach (var (Start, End) in SegmentPosition(RemoteFileProperties.Size, SegmentNumbers))
                {
                    SegmentDownloader item =  new SegmentDownloader(Url, Start, End);
                    await item.LoadSegmentPropertiesAsync();
                    segments.Add(item);
                }
                return segments;
            }

            SegmentDownloaders = await LoadSegnmentsAsync();
        }


        /// <summary>
        /// Update the segments info, to carry on where they left.
        /// </summary>
        /// <param name="JsonContent">The application stored (BasicSegmentsInfo) json value, which has some basic info about the segments.</param>
        /// <returns></returns>
        public async Task UpdateGeneratedSegmentsForResuimgAsync(string JsonContent)
        {
            var Serversegs = JsonSerializer.Deserialize<IList<ServerSegment>>(JsonContent);

            SegmentDownloaders = new List<SegmentDownloader>(Serversegs.Count);

            foreach (var seg in Serversegs.OrderBy(e=> e.Start))
            {
                var item = new SegmentDownloader(Url,seg);
                await  item.UpdateSegmentPropertiesForResumingAsync();
                SegmentDownloaders.Add(item);
            }
        }


        /// <summary>
        /// Reconstract the segments from the temp files that got created.
        /// </summary>
        /// <returns>A complete file in the directory that you specified in the constructor.</returns>
        public async Task ReconstructSegmentsAsync()
        {
            using (Stream localFileStream = new FileStream(LocalFileFullPath, FileMode.Create, FileAccess.ReadWrite))
            {
                foreach (var Segment in SegmentDownloaders.OrderBy(x => x.DownloadRange.Start))
                {
                    localFileStream.Seek(Segment.DownloadRange.Start, SeekOrigin.Begin);

                    using (Stream tempStream = new FileStream(Segment.RemoteSegmentProperties.LocalTempFileLocation, FileMode.Open, FileAccess.Read))
                    {
                        await tempStream.CopyToAsync(localFileStream);
                    }
                }
            }
            // Delete all the Temp files, after the reconstraction process.
            foreach (var Segment in SegmentDownloaders)
            {
                File.Delete(Segment.RemoteSegmentProperties.LocalTempFileLocation);
            }
        }



        /// <summary>
        /// A JSON content, which has some basic info to make the application can resotre the segments incase of any connection lose
        /// </summary>
        public string BasicSegmentsInfo => JsonSerializer.Serialize(SegmentDownloaders.Select(x=> x.RemoteSegmentProperties));


      }
}



