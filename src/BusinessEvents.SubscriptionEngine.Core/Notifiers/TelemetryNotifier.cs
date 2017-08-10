using System.Threading.Tasks;
using PageUp.Events;
using PageUp.Telemetry;

namespace BusinessEvents.SubscriptionEngine.Core.Notifiers
{
    public class TelemetryNotifier : INotifier
    {
        public Task Notify(Subscription subscriber, Event @event)
        {
            return Task.Factory.StartNew(() =>
            {
                var telemetryService = new TelemetryService();
                telemetryService.LogTelemetry(@event.Header.InstanceId, "business-event", $"business-event-{@event.Message.Header.MessageType}" ,
                new { MessageHeader = @event.Message.Header, EventHeader = @event.Header });
            });
        }
    }
}