using System.Collections.Generic;

namespace Microsoft.Identity.Json.Linq.JsonPath
{
    internal class RootFilter : PathFilter
    {
        public static readonly RootFilter Instance = new RootFilter();

        private RootFilter()
        {
        }

        public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, bool errorWhenNoMatch)
        {
            return new[] { root };
        }
    }
}