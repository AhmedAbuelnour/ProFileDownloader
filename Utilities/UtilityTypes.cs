namespace ProFileDownloader.Utilities
{
    using System.IO;
    using System.Text.Json.Serialization;

    /// <summary>
    /// 
    /// </summary>
    public class ServerSegment
    {
        /// <summary>
        /// Segment's Start position
        /// </summary>
        public long Start { get; set; }
        /// <summary>
        /// Segment's End position
        /// </summary>
        public long End { get; set; }
        /// <summary>
        /// Segment's Size
        /// </summary>
        public long Size { get; set; }
        [JsonIgnore] internal Stream DownloadContent { get; set; }
        /// <summary>
        /// Total bytes that have been read until now
        /// </summary>
        public long TotalReadBytes { get; set; }
        /// <summary>
        /// Temp file location
        /// </summary>
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
    internal class FileSegment
    {
        public int ID { get; set; }
        [JsonIgnore] public Stream PartialContent { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public string TempFile { get; set; } = Path.GetTempFileName();
        public long TotalReadBytes { get; set; } 
    }

    /// <summary>
    /// Represents an instance info for the current downloading file.
    /// </summary>
    public struct DownloadInfo
    {
        /// <summary>
        /// Current Percentage of the current ongoing downloading file
        /// </summary>
        public float CurrentPercentage { get; set; }
        /// <summary>
        /// Current downloading speed of the current ongoing file
        /// </summary>
        public string DownloadSpeed { get; set; }
        /// <summary>
        /// Current Progress of how much has been downloaded of the ongoing file.
        /// </summary>
        public string DownloadedProgress { get; set; }
    }
}
