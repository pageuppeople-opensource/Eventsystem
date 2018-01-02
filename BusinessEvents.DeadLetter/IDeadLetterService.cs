using System.Threading.Tasks;

namespace BusinessEvents.DeadLetter
{
    public interface IDeadLetterService
    {
        Task Handle(DeadLetterMessage deadletter);
    }
}