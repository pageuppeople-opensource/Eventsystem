using System;
using System.Collections.Generic;
using System.Net.Http;

namespace BusinessEvents.SubscriptionEngine.Handlers
{
    public static class HandlerHelper
    {
        public static string GetAccountId(string functionArn)
        {
            var arnItems = functionArn.Split(':');
            return arnItems[4];
        }

        private static string TryGetDirection(IDictionary<string, string> pathParameters, string pointer)
        {
            if (!pathParameters.ContainsKey("direction"))
            {
                switch (pointer.ToLower())
                {
                    case "head":
                        return "backward";
                    case "last":
                        return "forward";
                    default:
                        return "backward";
                }
            };

            var direction = pathParameters["direction"];

            var directionIsValid = !string.IsNullOrWhiteSpace(direction)
                                   && (direction.Equals("forward", StringComparison.OrdinalIgnoreCase) ||
                                       direction.Equals("backward", StringComparison.OrdinalIgnoreCase));

            if (!directionIsValid) throw new HttpRequestException($"Unsupported parameter: {direction}.");

            return direction;
        }

        private static int TryGetPageSize(IDictionary<string, string> pathParameters)
        {
            var pageSize = 20;

            if (!pathParameters.ContainsKey("pageSize")) return pageSize;

            var paramPageSize = pathParameters["pageSize"];

            int.TryParse(paramPageSize, out pageSize);

            return pageSize;
        }
    }
}