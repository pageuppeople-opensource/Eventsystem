using System.Threading.Tasks;

namespace BusinessEvents.SubscriptionEngine.Core.FeedManagement
{
    public interface IFeedService
    {
        Task<string> CreateFeed(string stream, string pointer, string direction, string pageSize);
    }
}