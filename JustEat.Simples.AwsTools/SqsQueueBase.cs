﻿using System;
using System.Collections.Generic;
using Amazon.SQS;
using Amazon.SQS.Model;
using NLog;
using Newtonsoft.Json.Linq;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public abstract class SqsQueueBase
    {
        public string Arn { get; protected set; }
        public string Url { get; protected set; }
        public AmazonSQS Client { get; private set; }
        public string QueueNamePrefix { get; protected set; }
        private static readonly Logger Log = LogManager.GetLogger("JustEat.Simples.NotificationStack");

        public SqsQueueBase(AmazonSQS client)
        {
            Client = client;
        }

        public abstract bool Exists();

        public void Delete()
        {
            Arn = null;
            Url = null;

            if (!Exists())
                return;

            var result = Client.DeleteQueue(new DeleteQueueRequest().WithQueueUrl(Url));
            //return result.IsSetResponseMetadata();
            Arn = null;
            Url = null;
        }

        protected void SetArn()
        {
            Arn = GetAttrs(new[] { "QueueArn" }).QueueARN;
        }

        protected GetQueueAttributesResult GetAttrs(IEnumerable<string> attrKeys)
        {
            var request = new GetQueueAttributesRequest().WithQueueUrl(Url);
            foreach (var key in attrKeys)
                request.WithAttributeName(key);

            var result = Client.GetQueueAttributes(request);

            return result.GetQueueAttributesResult;
        }

        public void AddPermission(SnsTopicBase snsTopic)
        {
            Client.SetQueueAttributes(new SetQueueAttributesRequest().WithQueueUrl(Url).WithPolicy(GetQueueSubscriptionPilocy(snsTopic)));
            Log.Info(string.Format("Added Queue permission for SNS topic to publish to Queue: {0}, Topic: {1}", Arn, snsTopic.Arn));
        }

        public bool HasPermission(SnsTopicBase snsTopic)
        {
            var policyResponse = Client.GetQueueAttributes(new GetQueueAttributesRequest().WithQueueUrl(Url).WithAttributeName(new[] { "Policy" }));
            if (policyResponse.IsSetResponseMetadata())
            {
                return policyResponse.GetQueueAttributesResult.Policy == null || policyResponse.GetQueueAttributesResult.Policy.Contains(snsTopic.Arn);
            }
            return false;
        }

        protected string GetQueueSubscriptionPilocy(SnsTopicBase topic)
        {
            return @"{
                                                      ""Version"": ""2012-10-17"",
                                                      ""Id"": ""Sns_Subsciption_Policy"",
                                                      ""Statement"": 
                                                        {
                                                           ""Sid"":""Send_Message"",
                                                           ""Effect"": ""Allow"",
                                                           ""Principal"": {
                                                                ""AWS"": ""*""
                                                             },
                                                            ""Action"": ""SQS:SendMessage"",
                                                            ""Resource"": """ + Arn + @""",
                                                            ""Condition"" : {
															   ""ArnEquals"" : {
																  ""aws:SourceArn"":""" + topic.Arn + @"""
															   }
															}
                                                         }
                                                    }";
        }
    }
}
