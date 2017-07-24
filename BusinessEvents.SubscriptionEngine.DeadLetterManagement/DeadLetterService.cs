using System.Threading.Tasks;

namespace BusinessEvents.SubscriptionEngine.DeadLetterManagement
{
    public class DeadLetterService : IDeadLetterService
    {
        public Task Handle(DeadLetterMessage snsMessage)
        {
            return null;
        }
    }
}
