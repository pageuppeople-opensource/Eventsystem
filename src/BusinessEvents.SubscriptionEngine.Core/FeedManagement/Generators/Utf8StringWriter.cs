using System.IO;
using System.Text;

namespace BusinessEvents.SubscriptionEngine.Core.FeedManagement.Generators
{
    internal class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
