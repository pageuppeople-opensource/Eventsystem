using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Castle.Core.Logging;
using Newtonsoft.Json;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class S3SubscriptionsManagement
    {
        private static readonly string BucketName = $"pageup-integration-{Environment.GetEnvironmentVariable("DATA_CENTER")?.ToLower() ?? "dc0"}";

        private const string FileName = "subscription-management/subscriptions.json";

        private static List<Subscription> subscriptions;
        private static DateTime lastModified = DateTime.MinValue;

        public static async Task<List<Subscription>> GetSubscriptions(AWSCredentials awsCredentials = null)
        {
            using (var client = CreateClient(awsCredentials))
            {
                try
                {
                    Console.WriteLine($"S3Subscription: Last modified date {lastModified}");

                    var modifiedAt = await GetLastModified(client);

                    Console.WriteLine($"S3Subscription: S3 modified at {modifiedAt}");

                    if (modifiedAt > lastModified)
                    {
                        var content = await GetContent(client);
                        subscriptions = JsonConvert.DeserializeObject<List<Subscription>>(content);

                        lastModified = modifiedAt;
                        Console.WriteLine($"S3Subscription: S3 content at first read {content}");
                    }
                }
                catch (AmazonS3Exception exception)
                {
                    if (exception.StatusCode != System.Net.HttpStatusCode.NotFound) throw;

                    if (exception.ErrorCode == "NoSuchKey") await CreateS3Item(client);

                    else await CreateS3BucketAndItem(client);

                    var content = await GetContent(client);
                    subscriptions = JsonConvert.DeserializeObject<List<Subscription>>(content);

                    Console.WriteLine($"S3Subscription: S3 content after reading the bucket /item is {content}");

                }
                catch (Exception exception)
                {
                    Console.WriteLine($"S3Subscription: Error received when ready s3 for subscriptions: {exception}");
                }
            }

            Console.WriteLine($"S3Subscriptions: returning subsciptions {JsonConvert.SerializeObject(subscriptions)}");

            return subscriptions;
        }

        private static async Task<string> GetContent(AmazonS3Client client)
        {
            string content;

            using (var response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = BucketName,
                Key = FileName
            }))
            using (var stream = response.ResponseStream)
            using (var reader = new StreamReader(stream))
            {
                content = reader.ReadToEnd();
                lastModified = response.LastModified;
            }

            return content;
        }

        private static async Task<DateTime> GetLastModified(AmazonS3Client client)
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = BucketName,
                Key = FileName
            };

            var response = await client.GetObjectMetadataAsync(request);

            return response.LastModified;
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
            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.APSoutheast2
            };
            return awsCredentials == null ? new AmazonS3Client(s3Config) : new AmazonS3Client(awsCredentials, s3Config);
        }
    }
}
