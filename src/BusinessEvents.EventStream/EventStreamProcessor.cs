using System;
using System.IO;
using System.Threading.Tasks;

namespace BusinessEvents.EventStream
{
    public class EventStreamProcessor
    {
        private readonly IBusinessEventStore _businessEventStore;

        public EventStreamProcessor(IBusinessEventStore businessEventStore)
        {
            _businessEventStore = businessEventStore;
        }
        
        public Task<bool> Process()
        {

            var businessEventStore = Container.Resolve<IBusinessEventStore>();

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
                            await MarkAsDeadLetter(@event, context.FunctionName, new Exception("Invalid Message in Event"));
                            logger.Log($"Error: Invalid Message: {JsonConvert.SerializeObject(@event)}");
                            continue;
                        }
                    }
                    catch (JsonException jsonException)
                    {
                        logger.Log($"Error: JsonConvert.DeserializeObject: {JsonConvert.SerializeObject(jsonException)}");
                        await MarkAsDeadLetter(@event, context.FunctionName, jsonException);
                        continue;
                    }

                    try
                    {
                        if (@event.Message.Header.Metadata != null && @event.Message.Header.Metadata.ContainsKey("OrderIndex") &&
                            @event.Message.Header.Metadata != null && @event.Message.Header.Metadata.ContainsKey("BatchId") &&
                            @event.Message.Header.Metadata != null && @event.Message.Header.Metadata.ContainsKey("Total"))
                            logger.Log($"BatchId: {@event.Message.Header.Metadata["BatchId"]} Total: {@event.Message.Header.Metadata["Total"]} OrderIndex: {@event.Message.Header.Metadata["OrderIndex"]} MessageId: {@event.Message.Header.MessageId}");
                        else
                            logger.Log($"MessageId: {@event.Message.Header.MessageId}");

                        await businessEventStore.PutEvent(@event);
                    }
                    catch (Exception e)
                    {
                        await MarkAsDeadLetter(@event, context.FunctionName, e);
                        logger.LogLine($"Error: {JsonConvert.SerializeObject(e)}");
                    }
                }
            };

            watch.Stop();
            logger.Log($"Kinesis Events Processed  {kinesisEvent.Records.Count} Time taken: {(watch.ElapsedMilliseconds/1000)} secs");
        }
    }
}