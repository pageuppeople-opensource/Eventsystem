using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;

namespace BusinessEvents.DeadLetter
{
    public class DeadLetterService : IDeadLetterService
    {
        private readonly Lazy<AmazonSimpleNotificationServiceClient> _snsClient;

        public DeadLetterService()
        {
            _snsClient = new Lazy<AmazonSimpleNotificationServiceClient>(() => new AmazonSimpleNotificationServiceClient());
        }

        public Task Handle(DeadLetterMessage deadletter)
        {
            return Task.Factory.StartNew(async () =>
            {
                var topicArnResponse = _snsClient.Value.CreateTopicAsync(new CreateTopicRequest(Environment.GetEnvironmentVariable("DLQ_SNS_Topic"))).Result;
                var request = new PublishRequest(topicArnResponse.TopicArn, JsonConvert.SerializeObject(deadletter));
                await _snsClient.Value.PublishAsync(request);
            });
        }
    }
}
