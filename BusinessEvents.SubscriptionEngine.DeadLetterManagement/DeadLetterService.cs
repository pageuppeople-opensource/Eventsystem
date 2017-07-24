using System;
using System.Threading.Tasks;
using BusinessEvents.SubscriptionEngine.Core;

namespace BusinessEvents.SubscriptionEngine.DeadLetterManagement
{
    public class DeadLetterService : IDeadLetterService
    {
        public Task Handle(DeadLetterMessage snsMessage)
        {
            throw new System.NotImplementedException();
        }
    }
}
