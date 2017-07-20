using PageUp.Events;
using Serverless.Dotnet.Core.Model;

namespace Serverless.Dotnet.Core
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