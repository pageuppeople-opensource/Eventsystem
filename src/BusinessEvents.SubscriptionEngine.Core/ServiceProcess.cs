using System;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public interface IServiceProcess
    {
        void Process(Event request);
    }

    public class ServiceProcess : IServiceProcess
    {
        public void Process(Event @event)
        {
            Console.WriteLine($"processed message - {JsonConvert.SerializeObject(@event)}");
        }
    }
}