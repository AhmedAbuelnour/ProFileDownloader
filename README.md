###  Pro File Downloader

* It Provides High Performance in downloading by using the new API System.IO.Pipelines
* It supports Resuming
* It supports File Segmentation
* It Shows Now: 
    1. Current % 
    2. Current Speed 
    3. How Much has been downloaded out of what is left

** Getting Started**

> File Downloading, No Resuming, No File Segmentation


```

static async Task Main(string[] args)
{
    Downloader downloader = new Downloader("Url", "DirectoryPath");
    await downloader.LoadRemoteFilePropertiesAsync();
    downloader.DownloadFile((e) =>
    {
       Console.WriteLine($"{e.CurrentPercentage} - {e.DownloadSpeed} - {e.DownloadedProgress}");
    });
}
```

> File Downloading, Yes Resuming , No File Segmentation 

```

static async Task Main(string[] args)
{
    Downloader downloader = new Downloader("Url", "DirectoryPath");
    await downloader.LoadRemoteFilePropertiesAsync();

    await downloader.UpdateRemoteFilePropertiesForResumingAsync();
   
    downloader.DownloadFile((e) =>
    {
       Console.WriteLine($"{e.CurrentPercentage} - {e.DownloadSpeed} - {e.DownloadedProgress}");
    });
}
```

> File Downloading, No Resuming , Yes File Segmentation 

```
using ProFileDownloader.FileTransferer;

static async Task Main(string[] args)
{
    FileSegmentaionManager fileDownloader = new FileSegmentaionManager("Url", "DirectoryPath");

    await fileDownloader.LoadRemoteFilePropertiesAsync(); Load the file Property to be ready for segmentation process.

    await fileDownloader.GenerateSegmentsAsync(); // Generate segments 
 
    // Note: fileDownloader.BasicSegmentsInfo : Is a property which has the JSON data of the generated Segments, Store it somewhere.
    
     foreach (var item in fileDownloader.SegmentDownloaders) // to take the advantages of Segmentaions do it in Parallel.
     {
           item.DownloadSegment((e) =>
           {
                  Console.WriteLine($"{e.CurrentPercentage} - {e.DownloadSpeed} - {e.DownloadedProgress}");
           });
     }

     await fileDownloader.ReconstructSegmentsAsync(); // Rebuild the segments to one single file.            
                
 }
```
> File Downloading, Yes Resuming , Yes File Segmentation 

```
using ProFileDownloader.FileTransferer;

static async Task Main(string[] args)
{
    FileSegmentaionManager fileDownloader = new FileSegmentaionManager("Url", "DirectoryPath");

    await fileDownloader.LoadRemoteFilePropertiesAsync(); Load the file Property to be ready for segmentation process.

    await fileDownloader.UploadGeneratedSegmentsForResuimgAsync("Json Content of the segments (BasicSegmentsInfo) "));
   
    foreach (var item in fileDownloader.SegmentDownloaders) // to take the advantages of Segmentaions do it in Parallel.
    {
           item.DownloadSegment((e) =>
           {
                  Console.WriteLine($"{e.CurrentPercentage} - {e.DownloadSpeed} - {e.DownloadedProgress}");
           });
    }

   await fileDownloader.ReconstructSegmentsAsync(); // Rebuild the segments to one single file.  
     
}

```
