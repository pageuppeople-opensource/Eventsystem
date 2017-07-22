using System;
using System.Collections.Generic;
using Amazon.Lambda.SNSEvents;
using Autofac;
using BusinessEvents.SubscriptionEngine.Core;
using BusinessEvents.SubscriptionEngine.Handlers;
using NSubstitute;
using PageUp.Events;
using Xunit;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class HandlerTests : TestBase
    {

        private IContainer CreateContainer(Action<ContainerBuilder> containerBuilderAction)
        {
            var containerBuilder = new ContainerBuilder();
            
            containerBuilderAction(containerBuilder);
            
            return containerBuilder.Build();
        }

        private Handler CreateHandler(IServiceProcess serviceProcess, ISubscriptionsManager subscriptionManager)
        {
            var container = CreateContainer(delegate (ContainerBuilder builder)
            {
                builder.RegisterInstance(serviceProcess);
                builder.RegisterInstance(subscriptionManager);
            });

            var handler = new Handler(container);
            return handler;
        }

        [Fact]
        public void HandlePassesAllSnsRecordsToProcess()
        {
            // arrange
            var testSnsEvent = new SNSEvent();
            testSnsEvent.Records = new List<SNSEvent.SNSRecord>
            {
                new SNSEvent.SNSRecord { Sns = new SNSEvent.SNSMessage { Message = ""} },
                new SNSEvent.SNSRecord { Sns = new SNSEvent.SNSMessage { Message = ""} }
            };

            var serviceProcess = Substitute.For<IServiceProcess>();
            var subscriptionManager = Substitute.For<ISubscriptionsManager>();
            Handler handler = CreateHandler(serviceProcess, subscriptionManager);

            //act
            handler.Handle(testSnsEvent);

            //assert
            serviceProcess.ReceivedWithAnyArgs(2).Process(Arg.Any<Event>());
        }
    }
}