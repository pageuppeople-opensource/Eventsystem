using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using Autofac;
using BusinessEvents.SubscriptionEngine.Core;
using BusinessEvents.SubscriptionEngine.DeadLetterManagement;
using BusinessEvents.SubscriptionEngine.Handlers;
using Newtonsoft.Json;
using NSubstitute;
using PageUp.Events;
using Xunit;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class HandlerTests : TestBase
    {
        private Handler CreateHandler()
        {
            var handler = new Handler(ContainerBuilder.Build());

            return handler;
        }

        [Fact]
        public async Task HandlePassesAllSnsRecordsToProcess()
        {
            // arrange
            var defaultValidEvent = JsonConvert.SerializeObject(new Event() { Messages = new[] { new Message() } });

            var testSnsEvent = new SNSEvent
            {
                Records = new List<SNSEvent.SNSRecord>
                {
                    new SNSEvent.SNSRecord {Sns = new SNSEvent.SNSMessage {Message = defaultValidEvent}},
                    new SNSEvent.SNSRecord {Sns = new SNSEvent.SNSMessage {Message = defaultValidEvent}}
                }
            };

            var serviceProcess = CreateMock<IServiceProcess>();
            var handler = CreateHandler();

            //act
            await handler.Handle(testSnsEvent);

            //assert
            await serviceProcess.ReceivedWithAnyArgs(2).Process(Arg.Any<Event>());
        }

        [Fact]
        public async Task IfRecordIsNotAnEventEnableMonitoring()
        {
            // arrange
            var testSnsEvent = new SNSEvent
            {
                Records = new List<SNSEvent.SNSRecord>
                {
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            // message is not an event.. it is just a random json object
                            Message = JsonConvert.SerializeObject(new {blah = "blahblah"})
                        }
                    },
                }
            };

            CreateMock<IServiceProcess>();
            var deadLetterService = CreateMock<IDeadLetterService>();
            
            Handler handler = CreateHandler();

            //act
            await handler.Handle(testSnsEvent);

            //assert
            await deadLetterService.ReceivedWithAnyArgs(1).Handle(Arg.Any<DeadLetterMessage>());
        }

        [Fact]
        public async Task IfRecordIsNotIsInvalidJsonEnableMonitoring()
        {
            // arrange
            var testSnsEvent = new SNSEvent
            {
                Records = new List<SNSEvent.SNSRecord>
                {
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            // message is an invalid json
                            Message = "invalid json"
                        }
                    },
                }
            };

            CreateMock<IServiceProcess>();
            var deadLetterService = CreateMock<IDeadLetterService>();

            Handler handler = CreateHandler();

            //act
            await handler.Handle(testSnsEvent);

            //assert
            await deadLetterService.ReceivedWithAnyArgs(1).Handle(Arg.Any<DeadLetterMessage>());
        }
    }
}