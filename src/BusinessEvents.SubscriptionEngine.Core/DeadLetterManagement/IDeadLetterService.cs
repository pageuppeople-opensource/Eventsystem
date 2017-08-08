using System.Threading.Tasks;

namespace BusinessEvents.SubscriptionEngine.Core.DeadLetterManagement
{
    public interface IDeadLetterService
    {
        Task Handle(DeadLetterMessage deadletter);
    }
}