using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class S3SubscriptionsManagement
    {
        private static AmazonS3Client client;
        private static readonly string BucketName = $"subscription-management-{Environment.GetEnvironmentVariable("AWS_REGION")?.ToLower() ?? "ap-southeast-2"}";
        private static readonly string FileName = "subscriptions.json";

        public static async Task<List<Subscription>> GetSubscriptions(AWSCredentials awsCredentials = null)
        {
            var request = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = FileName
            };
         
            string content = "";

            using (client = CreateClient(awsCredentials))
            {
                try
                {
                    using (var response = await client.GetObjectAsync(request))
                    using (var stream = response.ResponseStream)
                    using (var reader = new StreamReader(stream))
                    {
                        content = reader.ReadToEnd();
                    }
                }
                catch (AmazonS3Exception exception)
                {
                    if (exception.StatusCode != System.Net.HttpStatusCode.NotFound) throw;

                    if (exception.ErrorCode == "NoSuchKey")
                        await CreateS3Item(client);
                    else await CreateS3BucketAndItem(client);
                }
            }   

            return JsonConvert.DeserializeObject<List<Subscription>>(content);
        }

        private static async Task CreateS3BucketAndItem(AmazonS3Client client)
        {
            var createBucketRequest = new PutBucketRequest
            {
                BucketName = BucketName,
                BucketRegion = S3Region.APS2
            };
           
            await client.PutBucketAsync(createBucketRequest);
            await CreateS3Item(client);   
        }

        private static async Task CreateS3Item(AmazonS3Client client)
        {
            var createItemRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = FileName,
                ContentType = "application/json",
                ContentBody = "[]"
            };
           
            await client.PutObjectAsync(createItemRequest);
        }

        private static AmazonS3Client CreateClient(AWSCredentials awsCredentials)
        {
            var s3config = new AmazonS3Config()
            {
                RegionEndpoint = RegionEndpoint.APSoutheast2
            };
            return awsCredentials == null ? new AmazonS3Client(s3config) : new AmazonS3Client(awsCredentials, s3config);
        }
    }
}
