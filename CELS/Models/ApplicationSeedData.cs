using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Amazon.DynamoDBv2.Model;
using System.Threading;
using Amazon.S3;
using Amazon.S3.Model;

namespace CELS.Models
{
    public class ApplicationSeedData
    {
        private static RegionEndpoint awsRegion = RegionEndpoint.USEast1;
        public static BasicAWSCredentials credentials;

        public static void SetAWSCredentials(BasicAWSCredentials _credentials) => credentials = _credentials;

        public static async Task EnsureBucketsExistAsync()
        {
            // instantiate S3 client
            AmazonS3Client s3Client = new AmazonS3Client(credentials, awsRegion);

            // create S3 buckets
            //await CreateBucket(s3Client, "celsresumesmarvin");
            //await CreateBucket(s3Client, "celsphotosmarvin");
            await CreateBucket(s3Client, "celsresumes");
            await CreateBucket(s3Client, "celsphotos");
        }

        public static async Task EnsureDynamoDBTablesCreated()
        {
            // instantiate DynamoDB client
            AmazonDynamoDBClient dynamoDBClient = new AmazonDynamoDBClient(credentials, awsRegion);

            // create DynamoDB tables
            await CreateDynamoDBTable(dynamoDBClient, "Appointments", "AppointmentId");
            await CreateDynamoDBTable(dynamoDBClient, "Comments", "CommentId");
        }
        private static async Task CreateBucket(AmazonS3Client client, string bucketName)
        {
            PutBucketRequest request = new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true,
                CannedACL = S3CannedACL.PublicRead
            };
            await client.PutBucketAsync(request);
        }

        private static async Task CreateDynamoDBTable(AmazonDynamoDBClient client, string tableName, string hashKey)
        {
            CreateTableRequest request = new CreateTableRequest
            {
                TableName = tableName,
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = hashKey,
                        AttributeType = "S"
                    }
                },
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = hashKey,
                        KeyType = "HASH"
                    }
                },
                BillingMode = BillingMode.PROVISIONED,
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 2,
                    WriteCapacityUnits = 1
                }
            };

            try { await client.CreateTableAsync(request); }
            // catch (ResourceInUseException) { }
            catch (Exception) { }
        }
    }
}
