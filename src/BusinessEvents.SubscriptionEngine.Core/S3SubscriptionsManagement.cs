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
        private static readonly string BucketName = Environment.GetEnvironmentVariable("SUBSCRIPTION_CONFIG_BUCKET");

        private const string FileName = "subscription-management/subscriptions.json";

        private static List<Subscription> subscriptions;
        private static DateTime lastModified = DateTime.MinValue;

        public static async Task<List<Subscription>> GetSubscriptions(AWSCredentials awsCredentials = null)
        {
            Console.WriteLine($"BucketName: {BucketName}");

            using (var client = CreateClient(awsCredentials))
            {
                try
                {
                    var modifiedAt = await GetLastModified(client);

                    if (modifiedAt > lastModified)
                    {
                        var content = await GetContent(client);
                        subscriptions = JsonConvert.DeserializeObject<List<Subscription>>(content);
                        lastModified = modifiedAt;
                    }
                }
                catch (AmazonS3Exception exception)
                {
                    if (exception.StatusCode != System.Net.HttpStatusCode.NotFound) throw;

                    if (exception.ErrorCode == "NoSuchKey") await CreateS3Item(client);

                    else await CreateS3BucketAndItem(client);

                    var content = await GetContent(client);
                    subscriptions = JsonConvert.DeserializeObject<List<Subscription>>(content);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"GetSubscription: Error: {exception}");
                }
            }

            Console.WriteLine($"GetSubscription: Found {subscriptions.Count} subscriptions");
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
                BucketRegion = GetS3Region()
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
                RegionEndpoint = GetRegionEndpoint()
            };
            return awsCredentials == null ? new AmazonS3Client(s3Config) : new AmazonS3Client(awsCredentials, s3Config);
        }

        private static RegionEndpoint GetRegionEndpoint()
        {
            switch(Environment.GetEnvironmentVariable("DATA_CENTER"))
            {
                case "dc0":
                case "dc2":
                {
                    return RegionEndpoint.APSoutheast2;
                }
                case "dc3":
                case "dc7":
                {
                    return RegionEndpoint.EUWest1;
                }
                case "dc4":
                {
                    return RegionEndpoint.USEast1;
                }
                case "dc5":
                case "dc6":
                {
                    return RegionEndpoint.APSoutheast1;
                }
                default: return RegionEndpoint.APSoutheast2;
            }
        }

        private static S3Region GetS3Region()
        {
            switch(Environment.GetEnvironmentVariable("DATA_CENTER"))
            {
                case "dc0":
                case "dc2":
                {
                    return S3Region.APS2;
                }
                case "dc3":
                case "dc7":
                {
                    return S3Region.EU;
                }
                case "dc4":
                {
                    return S3Region.US;
                }
                case "dc5":
                case "dc6":
                {
                    return S3Region.APS1;
                }
                default: return S3Region.APS2;;
            }
        }
    }
}
