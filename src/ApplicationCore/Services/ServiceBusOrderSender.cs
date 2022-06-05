
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

internal class ServiceBusOrderSender
{

    // connection string to your Service Bus namespace
    private string connectionString = "";
        

    public ServiceBusOrderSender()
    {
        connectionString = Environment.GetEnvironmentVariable("ServiceBus");
    }

    // name of your Service Bus queue
    static string queueName = "orders";

    // the client that owns the connection and can be used to create senders and receivers
    static ServiceBusClient client;

    // the sender used to publish messages to the queue
    static ServiceBusSender sender;

    public async Task<bool> SendMessage(List<ReservationItem> itemToSend)
    {
        // The Service Bus client types are safe to cache and use as a singleton for the lifetime
        // of the application, which is best practice when messages are being published or read
        // regularly.
        //
        // Create the clients that we'll use for sending and processing messages.
        client = new ServiceBusClient(connectionString);
        sender = client.CreateSender(queueName);

        // create a batch 
        using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();


        var requestMessage = JsonConvert.SerializeObject(itemToSend);
        // try adding a message to the batch
        if (!messageBatch.TryAddMessage(new ServiceBusMessage(requestMessage)))
        {
            // if it is too large for the batch
            throw new System.Exception($"The message is too large to fit in the batch.");
        }

        try
        {
            // Use the producer client to send the batch of messages to the Service Bus queue
            await sender.SendMessagesAsync(messageBatch);
        }
        finally
        {
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }

        return true;
    }
}
