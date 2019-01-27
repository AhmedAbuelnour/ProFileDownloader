namespace ProFileDownloader.NetworkFile
{
    using System.IO;

    /// <summary>
    /// Represents the Remote File
    /// </summary>
    public class ServerFile
    {
        /// <summary>
        /// Location of the remote file
        /// </summary>
        public string Url { get; internal set; }
        /// <summary>
        /// Name of the remote file.
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Extension type of remote file.
        /// </summary>
        public string Extension { get; internal set; }
        /// <summary>
        /// Size in bytes of the remote file.
        /// </summary>
        public long Size { get; internal set; }
        /// <summary>
        /// Size in a readable format of the remote file.
        /// </summary>
        public string ReadableSize => Size.SizeSuffix();
        /// <summary>
        /// Media type of the remote file.
        /// </summary>
        public string MediaType { get; internal set; }
        /// <summary>
        /// The content stream to download of the remote file.
        /// </summary>
        public Stream DownloadContent { get; internal set; }
        /// <summary>
        /// Indicate if the server does support resuming for the remote file.
        /// </summary>
        public bool IsResumable { get; internal set; }
        /// <summary>
        /// Represents the total bytes read or downloaded from the remote file.
        /// </summary>
        public long TotalReadBytes { get; set; }
    }

    internal class FileProperites
    {
        public string FilePath { get; set; }
        public long Length { get; set; }
        public string FullName { get; set; }
    }
}
