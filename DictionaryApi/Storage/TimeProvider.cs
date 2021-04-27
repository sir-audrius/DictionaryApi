using System;

namespace DictionaryApi.Storage
{
    public interface ITimeProvider
    {
        DateTime GetDatetime();
    }

    public class TimeProvider : ITimeProvider
    {
        public DateTime GetDatetime()
        {
            return DateTime.UtcNow;
        }
    }
}