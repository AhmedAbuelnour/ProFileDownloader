###  Pro File Downloader

* It Provides High Performance in downloading by using the new API System.IO.Pipelines
* It supports Resuming
* It supports File Segmentation
* Gives you higher control over the segments downloading 
* Cross platform - .NET Core 3.0 
* Higher speed Json Serializer and Deserializer 
* Downloading Now Shows: 
    1. Current % 
    2. Current Speed 
    3. How Much has been downloaded out of what is left

> What is New in V6?
 * Support Streaming files Uploading/Downloading at the same time Sender/Recevier respectively by using 
  [FileStreamer](https://github.com/AhmedAbuelnour/ProFileDownloader/blob/master/FileStreamer.exe)
 * Limitedless file size uploading/downloading
 
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
using ProFileDownloader.FileNetwork;

static async Task Main(string[] args)
{
    FileSegmentaionManager fileDownloader = new FileSegmentaionManager("Url", "DirectoryPath");

    await fileDownloader.LoadRemoteFilePropertiesAsync(); Load the file Property to be ready for segmentation process.

    await fileDownloader.GenerateSegmentsAsync(); // Generate segments 
 
    // Note: fileDownloader.BasicSegmentsInfo : Is a property which has the JSON data of the generated Segments, Store it somewhere.
    
    Parallel.ForEach(fileDownloader.SegmentDownloaders, // to take the advantages of Segmentaions 
    (e) =>
    {
        e.DownloadSegment((x) =>
        {
            Console.WriteLine($"{x.CurrentPercentage} - {x.DownloadSpeed} - {x.DownloadedProgress}");
        });
    });

     await fileDownloader.ReconstructSegmentsAsync(); // Rebuild the segments to one single file.            
                
 }
```
> File Downloading, Yes Resuming , Yes File Segmentation 

```
using ProFileDownloader.FileNetwork;

static async Task Main(string[] args)
{
    FileSegmentaionManager fileDownloader = new FileSegmentaionManager("Url", "DirectoryPath");

    await fileDownloader.LoadRemoteFilePropertiesAsync(); Load the file Property to be ready for segmentation process.

    await fileDownloader.UploadGeneratedSegmentsForResuimgAsync("Json Content of the segments (BasicSegmentsInfo) "));
   
    Parallel.ForEach(fileDownloader.SegmentDownloaders, // to take the advantages of Segmentaions 
    (e) =>
    {
        e.DownloadSegment((x) =>
        {
            Console.WriteLine($"{x.CurrentPercentage} - {x.DownloadSpeed} - {x.DownloadedProgress}");
        });
    });

   await fileDownloader.ReconstructSegmentsAsync(); // Rebuild the segments to one single file.  
     
}

```


> File Uploading

```
using ProFileDownloader.FileNetwork;

static async Task Main(string[] args)
{
    FileTransfer fileTransfer = new FileTransfer();

    await fileTransfer.ConnectSenderAsync();

    Console.WriteLine("Enter the receiver user id:");

    string UserID = Console.ReadLine();

    await fileTransfer.SendStreamingAsync(@"File Path", UserID);

}

```

> File Downloading

```
using ProFileDownloader.FileNetwork;

static async Task Main(string[] args)
{
    FileTransfer fileTransfer = new FileTransfer();

    string UserID =  await fileTransfer.ConnectReceiverAsync();

    Console.WriteLine($"Your Receving ID Is: {UserID}");

    await fileTransfer.ReceiveStreamingAsync(@"File Path, where you want to download it.");
}

```
