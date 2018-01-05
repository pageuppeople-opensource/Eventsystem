using System.Threading.Tasks;
using Amazon.Lambda.KinesisEvents;

namespace BusinessEvents.EventStream
{
    public interface IEventStreamProcessor
    {
        Task Process(KinesisEvent kinesisEvent);
    }
}