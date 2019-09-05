using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ProFileDownloader.NetowrkFile
{
    /// <summary>
    /// A class which represents a file transfer between two nodes.
    /// </summary>
    public class FileTransfer
    {
        private readonly HubConnection hubConnection;
        /// <summary>
        /// Initialize the transfer connection.
        /// </summary>
        public FileTransfer()
        {
            hubConnection = new HubConnectionBuilder().WithUrl("https://prodownloader.azurewebsites.net/downloaderhub").WithAutomaticReconnect().AddMessagePackProtocol().Build();
        }

        /// <summary>
        /// Used to connect to the server as a sender
        /// </summary>
        /// <returns></returns>
        public async Task ConnectSenderAsync()
        {
            await hubConnection.StartAsync();
        }

        /// <summary>
        /// Used to connect to the server as a receiver
        /// </summary>
        /// <returns>A connection id to be shared with the sender, so the sender can only transfer the data to a specific user</returns>
        public async Task<string> ConnectReceiverAsync()
        {
            await hubConnection.StartAsync();
            return hubConnection.ConnectionId;
        }

        /// <summary>
        /// Start Streaming the data as a sender to the server.
        /// </summary>
        /// <param name="fileName">The path of the file, that you want to transfer</param>
        /// <param name="userID">The connection id, that the recevier shared with the sender</param>
        /// <returns></returns>
        public async Task SendStreamingAsync(string fileName, string userID)
        {
            async IAsyncEnumerable<byte[]> GetDataStream()
            {
                using Stream LocalFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                //4kb per chunk
                byte[] memory = new byte[4096];  // Init with backing buffer. Otherwise you are trying to read 0 bytes into a zero sized buffer. 

                int bytesRead;

                while ((bytesRead = await LocalFile.ReadAsync(memory)) > 0)
                {
                    yield return memory;
                }
            }

            await  hubConnection.InvokeAsync("SendData", GetDataStream(), userID);
        }

        /// <summary>
        /// Start Receving the streamed data from the server, then it is automatically close the connection.
        /// </summary>
        /// <param name="fileName">The path which you want to store the streamed data into.</param>
        /// <returns></returns>
        public async Task ReceiveStreamingAsync(string fileName)
        {
            await Task.Run(() =>
            {
                bool UndergoingReceiving = true;
                Stream LocalFile = new FileStream(fileName, FileMode.Create, FileAccess.Write); ;
                hubConnection.On<byte[]>("GetData", async (e) =>
                {
                    // Do Whatever you want with the streamed Value to the client. 
                    await LocalFile.WriteAsync(e);

                    // Also Check for the end of the streaming, so you can dispose 
                    if (e.Length == 1 && e[0] == 0)
                    {
                        LocalFile.Close();

                        UndergoingReceiving = false;
                    }
                });
                while (UndergoingReceiving) ;
            });
            await hubConnection.StopAsync();
        }
    }
}
