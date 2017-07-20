using BusinessEvents.SubscriptionEngine.Core.Model;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public interface IServiceProcess
    {
        Response Process(Event request);
    }

    public class ServiceProcess : IServiceProcess
    {
        public Response Process(Event request)
        {
            return new Response("Go Serverless dotnetcore! Your C# function executed successfully!", request);
        }
    }
}