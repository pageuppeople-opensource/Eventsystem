using System.Threading.Tasks;
using BusinessEvents.SubscriptionEngine.Core;

namespace BusinessEvents.SubscriptionEngine.DeadLetterManagement
{
    public interface IDeadLetterService
    {
        Task Handle(DeadLetterMessage snsMessage);
    }
}