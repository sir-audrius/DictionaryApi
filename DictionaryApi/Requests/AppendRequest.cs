using System.Collections.Generic;

namespace DictionaryApi.Requests
{
    public class AppendRequest
    {
        public int? ExpirationInSeconds { get; set; }
        public List<object> Values { get; set; }
    }
}
