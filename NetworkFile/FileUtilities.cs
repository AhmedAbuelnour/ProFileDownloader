namespace ProFileDownloader.NetworkFile
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides Utility Methods for dealing with files.
    /// </summary>
    public static class FileUtilities
    {
        internal async static Task<bool> IsResumable(string Url)
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
        #region extension to MIME type list
        private static readonly IDictionary<string, string> MediaTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
        {".asf", "video/x-ms-asf"},
        {".asx", "video/x-ms-asf"},
        {".avi", "video/x-msvideo"},
        {".bin", "application/octet-stream"},
        {".cco", "application/x-cocoa"},
        {".crt", "application/x-x509-ca-cert"},
        {".css", "text/css"},
        {".deb", "application/octet-stream"},
        {".der", "application/x-x509-ca-cert"},
        {".dll", "application/octet-stream"},
        {".dmg", "application/octet-stream"},
        {".ear", "application/java-archive"},
        {".eot", "application/octet-stream"},
        {".exe", "application/octet-stream"},
        {".flv", "video/x-flv"},
        {".gif", "image/gif"},
        {".hqx", "application/mac-binhex40"},
        {".htc", "text/x-component"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".img", "application/octet-stream"},
        {".iso", "application/octet-stream"},
        {".jar", "application/java-archive"},
        {".jardiff", "application/x-java-archive-diff"},
        {".jng", "image/x-jng"},
        {".jnlp", "application/x-java-jnlp-file"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".mml", "text/mathml"},
        {".mng", "video/x-mng"},
        {".mov", "video/quicktime"},
        {".mp3", "audio/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpg", "video/mpeg"},
        {".msi", "application/octet-stream"},
        {".msm", "application/octet-stream"},
        {".msp", "application/octet-stream"},
        {".pdb", "application/x-pilot"},
        {".pdf", "application/pdf"},
        {".pem", "application/x-x509-ca-cert"},
        {".pl", "application/x-perl"},
        {".pm", "application/x-perl"},
        {".png", "image/png"},
        {".prc", "application/x-pilot"},
        {".ra", "audio/x-realaudio"},
        {".rar", "application/x-rar-compressed"},
        {".rpm", "application/x-redhat-package-manager"},
        {".rss", "text/xml"},
        {".run", "application/x-makeself"},
        {".sea", "application/x-sea"},
        {".shtml", "text/html"},
        {".sit", "application/x-stuffit"},
        {".swf", "application/x-shockwave-flash"},
        {".tcl", "application/x-tcl"},
        {".tk", "application/x-tcl"},
        {".txt", "text/plain"},
        {".war", "application/java-archive"},
        {".wbmp", "image/vnd.wap.wbmp"},
        {".wmv", "video/x-ms-wmv"},
        {".xml", "text/xml"},
        {".xpi", "application/x-xpinstall"},
        {".zip", "application/zip"}
        };
        #endregion
        internal static string GetFileExtension(this string MediaType) => MediaTypeMappings.FirstOrDefault(x => x.Value == MediaType).Key;



        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        internal static string SizeSuffix(this long value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }




        internal static FileProperites GetFileProperites(string LocalFilePath)
        {
            if (File.Exists(LocalFilePath))
            {
                using (Stream localFile = new FileStream(LocalFilePath, FileMode.Open, FileAccess.Read))
                {
                    return new FileProperites
                    {
                        FilePath = LocalFilePath,
                        Length = localFile.Length,
                        FullName = Path.GetFileName(LocalFilePath)
                    };
                }
            }
            else
            {
                throw new Exception("No such file is found with the provided path, make sure to provide a correct path");
            }
        }

        /// <summary>
        /// Gets the properties of the remote file
        /// </summary>
        /// <param name="Url">The location of the remote file.</param>
        /// <returns>Properties of the remote file.</returns>
        public static async Task<ServerFile> GetServerFilePropertiesAsync(string Url)
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
                return new ServerFile
                {
                    Url = Url,
                    Name = Regex.Match(httpResponseMessage.Content.Headers?.ContentDisposition?.FileName ?? httpResponseMessage.RequestMessage.RequestUri.Segments.LastOrDefault(), "[A-Za-z0-9\\s_-]+").Value,
                    MediaType = httpResponseMessage.Content.Headers.ContentType.MediaType,
                    Size = httpResponseMessage.Content.Headers.ContentLength.GetValueOrDefault(),
                    Extension = httpResponseMessage.Content.Headers.ContentType.MediaType.GetFileExtension(),
                    IsResumable = await IsResumable(Url),
                    DownloadContent = await httpResponseMessage.Content.ReadAsStreamAsync(),
                    TotalReadBytes = 0
                };
            }
        }



    }
}
