using System.Threading.Tasks;
using PageUp.Telemetry;

namespace BusinessEvents.SubscriptionEngine.Core.DeadLetterManagement
{
    public class DeadLetterService : IDeadLetterService
    {
        public Task Handle(DeadLetterMessage deadletter)
        {
            return Task.Factory.StartNew(() =>
            {
                var telemetryService = new TelemetryService();
                telemetryService.LogTelemetry("0", "business-event", "deadletter", deadletter);
            });
        }
    }
}
