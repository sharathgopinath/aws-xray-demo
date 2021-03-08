using System;
using Amazon.Lambda.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Formatting.Json;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace aws_xray_demo.Process
{
    public class Function
    {
        private ILogger _logger;
        private IConfiguration _config;

        public Function()
        {
            AWSSDKHandler.RegisterXRayForAllServices();
            _logger = CreateLogger();
            _config = BuildConfiguration();
        }

        public void Process(SQSEvent sqsEvent, ILambdaContext context)
        {
            _logger.Information($"Beginning to process {sqsEvent.Records.Count} records...");
            
            try
            {
                foreach(var record in sqsEvent.Records)
                {
                    _logger.Information($"Record body: {record.Body}");
                    var messageBody = JsonConvert.DeserializeObject<MessageBody>(record.Body);

                    if (messageBody.FailureOnProcess == true)
                    {
                        throw new InvalidOperationException("Failed on process.");
                    }

                    // Process
                    _logger.Information($"Message processed - {messageBody.InputToUpper}.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw;
            }
            
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
                .Enrich.WithProperty("Application", "aws-xray-demo-process")
                .WriteTo.Console(new JsonFormatter())
                .CreateLogger();
    }

    public class MessageBody
    {
        public string InputToUpper { get; set; }
        public bool FailureOnProcess { get; set; }
    }
}