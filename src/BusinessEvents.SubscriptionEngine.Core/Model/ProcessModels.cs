using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core.Model
{
    public class Response
    {
        public string Message { get; set; }
        public Event Request { get; set; }

        public Response(string message, Event request)
        {
            Message = message;
            Request = request;
        }
    }
}