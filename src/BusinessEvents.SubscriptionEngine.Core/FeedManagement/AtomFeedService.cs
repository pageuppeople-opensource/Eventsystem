using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessEvents.SubscriptionEngine.Core.DataStore;
using AtomGenerator = BusinessEvents.SubscriptionEngine.Core.FeedManagement.Generators.AtomGenerator;

namespace BusinessEvents.SubscriptionEngine.Core.FeedManagement
{
    public class AtomFeedService : IFeedService
    {
        private readonly IBusinessEventStore _businessEventStore;

        public AtomFeedService(IBusinessEventStore businessEventStore)
        {
            _businessEventStore = businessEventStore;
        }

        public async Task<string> CreateFeed(string stream, string pointer, string direction, string pageSize)
        {
            // todo: validate pointer, direction and pageSize
            var asc = (pointer == "last") || (direction == "forward");
            var messageId = (pointer != "last" && pointer != "head") ? pointer : "";

            var items = await _businessEventStore.QueryByMessageType(stream, Convert.ToInt32(pageSize), asc, messageId);

            var feed = new Feed
            {
                Title = "Business Events",
                Author = new Author
                {
                    Name = "PageUp",
                    Email = "glofish@pageuppeople.com"
                },
                Copyright = "2017 @ PageUp",
                Description = "PageUp Business Events",
                Language = "en",
                UpdatedDate = DateTime.Now,
                Link = new Uri("https://api.dc0.pageuppeople.com/event/stream"),
                FeedEntries = new List<FeedEntry>(),
            };

            foreach (var item in items)
            {
                var feedEntry = new FeedEntry()
                {
                    Link = new Uri($"https://47c3hhw3bc.execute-api.ap-southeast-2.amazonaws.com/v1/event/{item["MessageId"].S}"),
                    PublishDate = DateTime.Parse(item["PublishedTimeStampUtc"].S),
                    Title = $"MessageType: {item["MessageType"].S}\nMessageId: {item["MessageId"].S}"
                };

                feed.FeedEntries.Add(feedEntry);
            }

            var atomGenerator = new AtomGenerator(feed);
            return atomGenerator.Process();
        }

//        public async Task<string> CreateFeed2(string stream, string pointer, string direction, string pageSize)
//        {
//            // todo: validate pointer, direction and pageSize
//            var asc = (pointer == "last") || (direction == "forward");
//            var messageId = (pointer != "last" && pointer != "head") ? pointer : "";
//
//            var items = await _businessEventStore.QueryByMessageType(stream, Convert.ToInt32(pageSize), asc, messageId);
//
//            var feed = new AtomFeed
//            {
//                BaseUri = new Uri("https://47c3hhw3bc.execute-api.ap-southeast-2.amazonaws.com/v1/events"),
//                Title = new AtomTextConstruct($"Business Events - {stream}"),
//                UpdatedOn = DateTime.UtcNow
//            };
//
//            // add link navigations here
//
//            var entries = new List<AtomEntry>();
//            foreach (var item in items)
//            {
//                var entry = new AtomEntry();
//                entry.Links.Add(new AtomLink(new Uri($"https://47c3hhw3bc.execute-api.ap-southeast-2.amazonaws.com/v1/event/{item["MessageId"].S}")));
//                entry.PublishedOn = DateTime.Parse(item["PublishedTimeStampUtc"].S);
//                entry.Title = new AtomTextConstruct($"MessageType: {item["MessageType"].S}\nMessageId: {item["MessageId"].S}");
//                entries.Add(entry);
//            }
//            feed.Entries = entries;
//
//            var headLink = new AtomLink
//            {
//                Uri = new Uri($"https://47c3hhw3bc.execute-api.ap-southeast-2.amazonaws.com/v1/events/{stream}/head/forward/{pageSize}"),
//                Relation = "first"
//            };
//
//            feed.Links.Add(headLink);
//
//            AtomGenerator atomGenerator = new AtomGenerator();
//            feed.Generator = atomGenerator;
//        }
    }
}