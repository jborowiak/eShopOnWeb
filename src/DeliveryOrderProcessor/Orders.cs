using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DeliveryOrderProcessor
{
    public static class Orders
    {
        [FunctionName("ProcessOrder")]
        public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
    [CosmosDB(
        databaseName: "Orders",
        collectionName: "DeliveryOrders",
        ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut,
    ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var deliveryItem
                = JsonConvert.DeserializeObject<DeliveryItem>(requestBody);

            deliveryItem.ID = Guid.NewGuid().ToString();
            // Add a JSON document to the output container.
            await documentsOut.AddAsync(deliveryItem);

            log.LogInformation("Order stored in a delivery processr.");

            return new OkObjectResult("Order stored in a delivery processr.");
        }

    }

    internal class DeliveryItem
    {
        public string ID { get; set; }
        public Address ShippingAddress { get; set; }

        public decimal FinalPrice { get; set; }

        public List<OrderItem> Items { get; set; }
    }


    internal class Address
    {
        public string Street { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Country { get; set; }

        public string ZipCode { get; set; }

    }

    internal class OrderItem
    {
        public CatalogItemOrdered ItemOrdered { get; set; }
        public decimal UnitPrice { get; set; }
        public int Units { get; set; }

    }

    internal class CatalogItemOrdered
    {
        public int CatalogItemId { get; set; }
        public string ProductName { get; set; }
        public string PictureUri { get; set; }
    }
}
