using System;
using System.Collections.Generic;

namespace DictionaryApi.Storage
{
    public class ValueRecord
    {
        public int ExpirationInterval { get; set; }
        public DateTime ExpirationDate { get; set; }
        public List<object> Values { get; set; }
    }
}
