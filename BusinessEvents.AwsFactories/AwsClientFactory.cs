//using System;
//using Amazon;
//using Amazon.DynamoDBv2;
//using Amazon.KeyManagementService;
//using Amazon.Kinesis;
//using Amazon.SQS;
//
//namespace BusinessEvents.SubscriptionEngine.Core.Factories
//{
//    public class AwsClientFactory
//    {
//        public static AmazonKinesisClient CreateKinesisClient()
//        {
//            return new AmazonKinesisClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));
//        }
//
//        public static AmazonKeyManagementServiceClient CreateAmazonKeyManagementServiceClient()
//        {
//            return new AmazonKeyManagementServiceClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));
//        }
//
//        public static AmazonSQSClient CreateAmazonSqsClient()
//        {
//            return new AmazonSQSClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));
//        }
//    }
//}