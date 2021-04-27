using System;
using DictionaryApi.Storage;

namespace DictionaryApiTests
{
    public class TestTimeProvider : ITimeProvider
    {
        public DateTime DateTime { get; set; }

        public DateTime GetDatetime()
        {
            return DateTime;
        }
    }
}