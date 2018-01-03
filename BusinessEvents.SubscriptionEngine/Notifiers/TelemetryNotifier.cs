using System;
using System.Threading.Tasks;
using PageUp.Events;
using PageUp.Telemetry;

namespace BusinessEvents.SubscriptionEngine.Notifiers
{
    public class TelemetryNotifier : INotifier
    {
        private readonly TelemetryService telemetryService;

        public TelemetryNotifier()
        {
            telemetryService = new TelemetryService();
        }
        
        public Task Notify(Subscription subscriber, Event @event)
        {
            return Task.Factory.StartNew(() =>
            {
                Console.WriteLine($"MessageId: {@event.Message.Header.MessageId} Event: {@event.Message.Header.MessageType} Subscriber: {subscriber.Type}:{subscriber.Endpoint}");
                telemetryService.LogTelemetry(@event.Header.InstanceId, "business-event", @event.Message.Header.MessageType ,
                new { MessageHeader = @event.Message.Header, EventHeader = @event.Header });
            });
        }
    }
}
