using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using Autofac;
using BusinessEvents.SubscriptionEngine.Core;
using BusinessEvents.SubscriptionEngine.Handlers;
using Newtonsoft.Json;
using NSubstitute;
using PageUp.Events;
using Xunit;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class HandlerTests : TestBase
    {
        private readonly ContainerBuilder containerBuilder = new ContainerBuilder();

        private T CreateMock<T>() where T: class
        {
            var instance = Substitute.For<T>();
            containerBuilder.RegisterInstance<T>(instance);

            return instance;
        }

        private Handler CreateHandler()
        {
            var handler = new Handler(containerBuilder.Build());

            return handler;
        }

        [Fact]
        public async Task HandlePassesAllSnsRecordsToProcess()
        {
            // arrange
            var testSnsEvent = new SNSEvent
            {
                Records = new List<SNSEvent.SNSRecord>
                {
                    new SNSEvent.SNSRecord {Sns = new SNSEvent.SNSMessage {Message = ""}},
                    new SNSEvent.SNSRecord {Sns = new SNSEvent.SNSMessage {Message = ""}}
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
    }
}