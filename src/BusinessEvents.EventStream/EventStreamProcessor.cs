using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.KinesisEvents;
using BusinessEvents.DeadLetter;
using BusinessEvents.EventStore;
using BusinessEvents.Utilities;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.EventStream
{
    public class EventStreamProcessor : IEventStreamProcessor
    {
        private readonly IBusinessEventStore _businessEventStore;
        private readonly IDeadLetterService _deadLetterService;

        public EventStreamProcessor(IBusinessEventStore businessEventStore, IDeadLetterService deadLetterService)
        {
            _businessEventStore = businessEventStore;
            _deadLetterService = deadLetterService;
        }

        private async Task MarkAsDeadLetter(Event @event, string function, Exception exception = null)
        {
            var message = JsonConvert.SerializeObject(@event);

            var deadLetter = new DeadLetterMessage
            {
                Function = function,
                MessageId = @event?.Message?.Header?.MessageId,
                PublishedTimeStampUtc = @event?.Header.TransportTimeStamp,
                CreatedTimeStampUtc = DateTime.UtcNow,
                Domain = BusinessEventStore.GetDomain(@event),
                InstanceId = @event?.Header?.InstanceId,
                MessageType = @event?.Message?.Header?.MessageType,
                Message = message.ToCompressedBase64String(),
                Exception = exception
            };

            try
            {
                await _deadLetterService.Handle(deadLetter);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Error: MessageId: {@event?.Message?.Header?.MessageId} MarkAsDeadLetter Failed: {JsonConvert.SerializeObject(e)} DeadLetter: {JsonConvert.SerializeObject(deadLetter)}");
            }
        }

        public async Task Process(KinesisEvent kinesisEvent)
        {
            foreach(var record in kinesisEvent.Records)
            {
                using (var sr = new StreamReader(record.Kinesis.Data))
                {
                    var message = sr.ReadToEnd();

                    Event @event = null;
                    try
                    {
                        @event = JsonConvert.DeserializeObject<Event>(message);

                        if (@event?.Message == null)
                        {
                            await MarkAsDeadLetter(@event, "EventStreamProcessor", new Exception("Invalid Message in Event"));
                            continue;
                        }
                    }
                    catch (JsonException jsonException)
                    {
                        await MarkAsDeadLetter(@event, "EventStreamProcessor", jsonException);
                        continue;
                    }

                    try
                    {
                        await _businessEventStore.PutEvent(@event);
                    }
                    catch (Exception e)
                    {
                        await MarkAsDeadLetter(@event, "EventStreamProcessor", e);
                    }
                }
            };
        }
    }
}
