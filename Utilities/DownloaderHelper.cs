namespace ProFileDownloader.Utilities
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    internal static class DownloaderHelper
    {
        internal static async Task<bool> IsResumable(string Url)
        {
            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(1, 1);
            using HttpResponseMessage Result = await httpClient.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);
            return Result.StatusCode == HttpStatusCode.PartialContent;
        }
    }
}
