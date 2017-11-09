using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using BusinessEvents.SubscriptionEngine.Core.DeadLetterManagement;
using BusinessEvents.SubscriptionEngine.Core.Factories;

namespace BusinessEvents.SubscriptionEngine.Core.QueueManagement
{
    public class DeadLetterQueue : IQueue
    {
        private Lazy<string> QueueUrl { get; set; }

        public DeadLetterQueue(string queueName)
        {
            QueueUrl = new Lazy<string>(() => TryGetQueueUrl(queueName).Result);
        }

        public async Task<string> TryGetQueueUrl(string queueName)
        {
            if (QueueUrl.IsValueCreated)
            {
                return QueueUrl.Value;
            }

            using (var sqsClient = AwsClientFactory.CreateAmazonSqsClient())
            {
                var getQueueResponse = await sqsClient.GetQueueUrlAsync(queueName);
                return getQueueResponse.QueueUrl;
            }
        }

        public async Task<string> SendMessage(string messageBody)
        {
            using (var sqsClient = AwsClientFactory.CreateAmazonSqsClient())
            {
                var response = await sqsClient.SendMessageAsync(QueueUrl.Value, messageBody);
                return response.MessageId;
            }
        }

        public async Task<List<Message>> GetMessages(int limit = 10)
        {
            using (var sqsClient = AwsClientFactory.CreateAmazonSqsClient())
            {
                var messagesResponse = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest() {MaxNumberOfMessages = limit, QueueUrl = QueueUrl.Value});
                return messagesResponse.Messages;
            }
        }

        public async Task<bool> DeleteMessage(string recieptHandle)
        {
            using (var sqsClient = AwsClientFactory.CreateAmazonSqsClient())
            {
                var messagesResponse = await sqsClient.DeleteMessageAsync(new DeleteMessageRequest() {ReceiptHandle = recieptHandle, QueueUrl = QueueUrl.Value});
                return messagesResponse.HttpStatusCode == HttpStatusCode.OK;
            }
        }
    }
}