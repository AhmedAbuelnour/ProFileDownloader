namespace ProFileDownloader.NetworkFile
{
    using System.IO;
    using System.Text.Json.Serialization;

    public class ServerSegment
    {
        public long Start { get; set; }
        public long End { get; set; }
        public long Size { get; set; }
        [JsonIgnore] internal Stream DownloadContent { get; set; }
        public long TotalReadBytes { get; set; }
        public string LocalTempFileLocation { get; set; }
    }

    internal class ServerFile
    {
        internal string Name { get;  set; }
        internal string Extension { get;  set; }
        internal long Size { get;  set; }
        internal string MediaType { get;  set; }
        internal Stream DownloadContent { get;  set; }
        internal bool IsResumable { get;  set; }
        internal long TotalReadBytes { get; set; }
    }

    public struct SegmentOptions
    {
        public int SegmentNumbers { get; set; }

        public bool SupportSegmentation { get; set; }
    }

    internal class FileSegment
    {
        public int ID { get; set; }
        [JsonIgnore] public Stream PartialContent { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public string TempFile { get; set; } = Path.GetTempFileName();
        public long TotalReadBytes { get; set; } 
    }

    public struct DownloadInfo
    {
        public float CurrentPercentage { get; set; }
        public string DownloadSpeed { get; set; }
        public string DownloadedProgress { get; set; }
    }
}
