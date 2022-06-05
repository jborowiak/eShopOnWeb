using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Core;
using System.Net.Http;

namespace OrderItemsReserver
{
    public static class Orders
    {
        [FunctionName("ServiceBusQueueTrigger1")]
        public static async Task Run(
            [ServiceBusTrigger("orders", Connection = "jbservicebus1_RootManageSharedAccessKey_SERVICEBUS")]
            string myQueueItem,
            Int32 deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation(myQueueItem);

            var orderItems
                = JsonConvert.DeserializeObject<List<OrderItem>>(myQueueItem);

            await CreateBlob(Guid.NewGuid().ToString(), orderItems, log);

            log.LogInformation("Order stored in a warehouse.");

           
        }

        public static async Task CreateBlob(string name, List<OrderItem> content, ILogger log)
        {
            var connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");

            log.LogInformation(connectionString);
            var containerName = "orders";

            BlobClientOptions blobClientOptions = new BlobClientOptions
            {
                Retry =
                {
                    Mode = RetryMode.Exponential,
                    MaxRetries = 3,
                    Delay = TimeSpan.FromSeconds(5),
                    MaxDelay = TimeSpan.FromSeconds(10)
                }
            };

            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString, blobClientOptions);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blob = containerClient.GetBlobClient(name + ".json");

                await blob.UploadAsync(
                    new MemoryStream(
                        Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(content))));
            }
            catch(Exception ex)
            {
                HttpClient client = new HttpClient();
                var httpContent = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
                await client.PostAsync(
                    "https://prod-16.westeurope.logic.azure.com:443/workflows/ae34745292a440b297d56e28f9a16b43/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=St5Db7JBFMziFM-mTuqJOuxhq9Peoeo3wGfUDR3jr8o",
                    httpContent);
            }
           
        }
    }

    public class OrderItem
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
