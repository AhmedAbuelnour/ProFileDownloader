namespace ProFileDownloader.NetworkFile
{
    using Newtonsoft.Json;
    using System.IO;

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
        [JsonProperty]
        public int ID { get; set; }
        [JsonIgnore]
        public Stream PartialContent { get; set; }
        [JsonProperty]
        public long Start { get; set; }
        [JsonProperty]
        public long End { get; set; }
        [JsonProperty]
        public string TempFile { get; set; } = Path.GetTempFileName();
        [JsonIgnore]
        public long TotalReadBytes { get; set; } 
    }
}
