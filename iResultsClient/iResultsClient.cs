using Azure.Core;
using Azure.Messaging.ServiceBus;
using irsdkSharp;
using irsdkSharp.Serialization;
using irsdkSharp.Serialization.Models.Fastest;
using Newtonsoft.Json;
using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace iResults
{
    public class TelemetryMessage
    {
        public string SessionId;
        public TimeSpan LapTime;
        public string Car;

        public TelemetryMessage(string sessionId, TimeSpan lapTime, string car)
        {
            SessionId = sessionId;
            LapTime = lapTime;
            Car = car;
        }
    }

    public class iResultsClient
    {
        SessionTracker sessionTracker = new SessionTracker();

        ServiceBusClient client;
        ServiceBusProcessor processor;
        ServiceBusSender sender; 

        public iResultsClient()
        {
            client = new ServiceBusClient(ConfigurationManager.ConnectionStrings["SBConnection"].ConnectionString);
            sender = client.CreateSender("iresultsqueue");
            processor = client.CreateProcessor("iresultsqueue");

            // add handler to process messages
            processor.ProcessMessageAsync += MessageHandler;

            // add handler to process any errors
            processor.ProcessErrorAsync += ErrorHandler;
        }

        public async Task Start()
        {
            sessionTracker.SessionMessageAvailable += SessionTracker_SessionMessageAvailable;

            // Debug processor
            //await processor.StartProcessingAsync();

            await sessionTracker.StartSessionTracking();
        }

        public async Task Stop()
        {
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }

        private async void SessionTracker_SessionMessageAvailable(object? sender, SessionInfoMessage msg)
        {
            await SendMessage(msg);
        }

        public async Task SendMessage(SessionInfoMessage msg)
        {
            string messageBody = JsonConvert.SerializeObject(msg);
            var message = new ServiceBusMessage(messageBody);

            Console.WriteLine($"Sent a SessionInfoMessage!\n{messageBody}");
            await sender.SendMessageAsync(message);
        }

        // handle received messages
        async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            Console.WriteLine($"Received: {body}");

            // complete the message. message is deleted from the queue. 
            await args.CompleteMessageAsync(args.Message);
        }

        // handle any errors when receiving messages
        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }
    }
}
