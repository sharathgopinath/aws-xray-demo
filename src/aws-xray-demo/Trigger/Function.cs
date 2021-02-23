using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2.Model;
using Serilog;
using Serilog.Formatting.Json;
using Microsoft.Extensions.Configuration;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace aws_xray_demo.Trigger
{
    public class Function
    {
        private IAmazonDynamoDB _dynamoDbClient;
        private ILogger _logger;
        private IConfiguration _config;

        private IDictionary<FailurePoints, bool> _failures = new Dictionary<FailurePoints, bool>
        {
            {FailurePoints.OnSave, false},
            {FailurePoints.OnPublish, false},
            {FailurePoints.OnProcess, false},
        };

        public Function()
        {
            _logger = CreateLogger();
            _config = BuildConfiguration();
        }

        /// <summary>
        /// A simple function that takes a string and does a ToUpper and saves in database
        /// </summary>
        public async Task ToUpper(string input, ILambdaContext context)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }
            SetupFailures();

            var inputToUpper = input.ToUpper();

            try
            {
                if (_failures[FailurePoints.OnSave])
                {
                    throw new InvalidOperationException("Failed on save.");
                }
                await Save(inputToUpper);

                if (_failures[FailurePoints.OnProcess])
                {
                    throw new InvalidOperationException("Failed on publish.");
                }

                //Publish(input);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw;
            }
        }

        private async Task Save(string input)
        {
            _dynamoDbClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig 
            {
                ServiceURL = _config.GetSection("DataStore:ServiceUrl").Value,
                RegionEndpoint = Amazon.RegionEndpoint.APSoutheast2
            });
            
            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    {"id", new AttributeValue{S = Guid.NewGuid().ToString()}},
                    {"text", new AttributeValue{S = input.ToUpper()}},
                    {"timestamp", new AttributeValue{S = DateTime.Now.ToString()}}
                },
                TableName = _config.GetSection("DataStore:TableName").Value
            });
        }

        private void SetupFailures()
        {
            var random = new Random();
            _failures[FailurePoints.OnSave] = random.Next(20) % 2 == 0;
            _failures[FailurePoints.OnPublish] = random.Next(20) % 2 == 0;
            _failures[FailurePoints.OnProcess] = random.Next(20) % 2 == 0;
        }

        private IConfiguration BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("Environment");
            return new ConfigurationBuilder()
                .AddJsonFile("appSettings.json")
                .AddJsonFile($"appSettings.{environment}.json", true)
                .Build();
        }

        private ILogger CreateLogger() =>
            new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "aws-xray-demo-trigger")
                .WriteTo.Console(new JsonFormatter())
                .CreateLogger();
    }

    public enum FailurePoints
    {
        OnSave,
        OnPublish,
        OnProcess
    }
}
