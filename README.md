###  Pro File Downloader

* It Provides High Performance in downloading by using the new API System.IO.Pipelines
* It supports Resuming 
* It supports File Segmentation 


**Getting Started**

> File Downloading, No Resuming , No File Segmentation 


```
using ProFileDownloader.FileTransferer;

static async Task Main(string[] args)
{
          Downloader downloader = new Downloader("Url", "DirectoryPath");
          await downloader.LoadRemoteFilePropertiesAsync();
          await downloader.DownloadFileAsync((e) =>
          {
                Console.WriteLine(e); // Current Progress
          });
}
```

> File Downloading, Yes Resuming , No File Segmentation 

```
using ProFileDownloader.FileTransferer;

static async Task Main(string[] args)
{
    Downloader downloader = new Downloader("Url", "DirectoryPath");
    await downloader.LoadRemoteFilePropertiesAsync();
    if (downloader.IsRemoteServerSupportResuming)
    {
        await downloader.UpdateRemoteFilePropertiesForResuming();
    }
    await downloader.DownloadFileAsync((e) =>
    {
        Console.WriteLine(e); // Current Progress
    });
}
```

> File Downloading, No Resuming , Yes File Segmentation 

```
using ProFileDownloader.FileTransferer;

static async Task Main(string[] args)
{
      FileSegmentaionDownloader fileDownloader = new FileSegmentaionDownloader("Url", "DirectoryPath");

            await fileDownloader.LoadRemoteFilePropertiesAsync();

            if (fileDownloader.IsRemoteServerSupportFileSegmentaion)
            {
                await fileDownloader.LoadFileSegmentsAsync();
               // Store this content on your own, to be used for later resuming 
                string JsonContentToSave = fileDownloader.GetBasicSegmentsInfo();

                await fileDownloader.DownloadFileSegmensAsync((e) => 
                {
                    Console.WriteLine(e);
                });
                await fileDownloader.ReconstructSegmentsAsync();
            }
            else
            {
                await fileDownloader.DownloadFileAsync((e) =>
                {
                    Console.WriteLine(e);
                });
            } 
}
```
> File Downloading, Yes Resuming , Yes File Segmentation 

```
using ProFileDownloader.FileTransferer;

static async Task Main(string[] args)
{
 FileSegmentaionDownloader fileDownloader = new FileSegmentaionDownloader("Url", "DirectoryPath");

await fileDownloader.LoadRemoteFilePropertiesAsync();

if (fileDownloader.IsRemoteServerSupportFileSegmentaion)
{

    await fileDownloader.LoadFileSegmentsForResumingAsync("Json Content That you stored");

    await fileDownloader.DownloadFileSegmensAsync((e) => 
    {
        Console.WriteLine(e);
    });

    await fileDownloader.ReconstructSegmentsAsync();

}
else
{
    await fileDownloader.DownloadFileAsync((e) =>
    {
        Console.WriteLine(e);
    });
}

}

```

