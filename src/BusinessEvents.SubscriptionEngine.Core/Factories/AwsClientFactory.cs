using System;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.KeyManagementService;
using Amazon.Kinesis;

namespace BusinessEvents.SubscriptionEngine.Core.Factories
{
    public class AwsClientFactory
    {
        public static AmazonDynamoDBClient CreateDynamoDbClient()
        {
            var client = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));
            return client;
        }

        public static AmazonKinesisClient CreateKinesisClient()
        {
            return new AmazonKinesisClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));
        }

        public static AmazonKeyManagementServiceClient CreateAmazonKeyManagementServiceClient()
        {
            return new AmazonKeyManagementServiceClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));
        }
    }
}