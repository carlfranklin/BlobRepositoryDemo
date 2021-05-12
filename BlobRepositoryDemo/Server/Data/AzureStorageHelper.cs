using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;


namespace BlobRepositoryDemo.Server.Data
{
    public class AzureStorageHelper
    {
        CloudBlobContainer container = null;
        string AzureBlobStorageConnectionString = "";

        public AzureStorageHelper(string azureBlobStorageConnectionString)
        {
            AzureBlobStorageConnectionString = azureBlobStorageConnectionString;
        }

        public delegate void UploadEventHandler(object sender, UploadEventArgs e);

        public event UploadEventHandler UploadStatus;

        private delegate void raiseUploadEventSafelyDelgate(int PercentComplete);

        private void raiseUploadEventSafely(int BytesSent, int TotalBytes)
        {
            var arg = new UploadEventArgs();
            arg.BytesSent = BytesSent;
            arg.TotalBytes = TotalBytes;
            UploadStatus?.Invoke(this, arg);
        }

        public async Task UploadFileInChunks(string containerName, string sourceFilename, string destFileName)
        {
            using Stream stream = System.IO.File.OpenRead(sourceFilename);

            int size = 1000000;
            await OpenContianer(containerName);

            CloudBlockBlob blob = container.GetBlockBlobReference(destFileName);

            // local variable to track the current number of bytes read into buffer
            int bytesRead;
            int totalBytesRead = 0;

            // track the current block number as the code iterates through the file
            int blockNumber = 0;

            // Create list to track blockIds, it will be needed after the loop
            List<string> blockList = new List<string>();

            do
            {
                // increment block number by 1 each iteration
                blockNumber++;

                // set block ID as a string and convert it to Base64 which is the required format
                string blockId = $"{blockNumber:0000000}";
                string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

                // create buffer and retrieve chunk
                byte[] buffer = new byte[size];
                bytesRead = await stream.ReadAsync(buffer, 0, size);
                totalBytesRead += bytesRead;

                // Upload buffer chunk to Azure
                await blob.PutBlockAsync(base64BlockId, new MemoryStream(buffer, 0, bytesRead), null);

                raiseUploadEventSafely(totalBytesRead, Convert.ToInt32(stream.Length));

                // add the current blockId into our list
                blockList.Add(base64BlockId);

                // While bytesRead == size it means there is more data left to read and process
            } while (bytesRead == size);

            // add the blockList to the Azure which allows the resource to stick together the chunks
            await blob.PutBlockListAsync(blockList);

        }

        public async Task DownloadFile(string containerName, string sourceFilename, string destFileName)
        {
            await OpenContianer(containerName);

            CloudBlockBlob blob = container.GetBlockBlobReference(sourceFilename);

            await blob.DownloadToFileAsync(destFileName, FileMode.Create);

        }

        async Task OpenContianer(string containerName)
        {
            try
            {

                CloudStorageAccount storageAccount;
                if (CloudStorageAccount.TryParse(AzureBlobStorageConnectionString, out storageAccount))
                {
                    // Create the container and return a container client object
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    container = cloudBlobClient.GetContainerReference(containerName);
                    if (!container.Exists())
                    {
                        await container.CreateAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
        }

        public string GetUniqueFileName(string fileNameWithoutExtension)
        {
            fileNameWithoutExtension = fileNameWithoutExtension.Trim();

            // look backwords from the right for the first non-number
            int len = fileNameWithoutExtension.Length;
            for (int i = len - 1; i > 0; i--)
            {
                if (!IsNumeric(fileNameWithoutExtension.Substring(i, 1)))
                {
                    if (i < len - 1)
                    {
                        // we have a number;
                        string num = fileNameWithoutExtension.Substring(i + 1);
                        return fileNameWithoutExtension.Substring(0, i + 1) +
                            (Convert.ToInt32(num) + 1).ToString();
                    }
                    else
                    {
                        return fileNameWithoutExtension + "1";
                    }
                }
            }
            // we got all the way through. This filaname is all numbers. Add a _1 to the end.
            return fileNameWithoutExtension + "_1";
        }

        bool IsNumeric(string text)
        {
            int l = text.Length;
            string numbers = "01234567890-.,";

            for (int i = 0; i < l; i++)
            {
                if (!numbers.Contains(text.Substring(i, 1)))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class UploadEventArgs : EventArgs
    {
        public int BytesSent { get; set; }
        public int TotalBytes { get; set; }
    }
}