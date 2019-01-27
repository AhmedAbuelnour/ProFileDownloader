# ProFileDownloader-PFD
Provide an easy and professional way to download files.

# How To Get Started!
```
using ProFileDownloader.NetworkFile;
using ProFileDownloader.FileTransferer;
namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            FileDownloader fileDownloader = new FileDownloader
            {
                LocalFilePath = "fileName.extension"
            };

            ServerFile serverFile = await      FileUtilities.GetServerFilePropertiesAsync("Url");

            await fileDownloader.DownloadFileAsync(serverFile, (e) =>
            {
                Console.WriteLine($"{e}");
            });
         }
     }
}

```
