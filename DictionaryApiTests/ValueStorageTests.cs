using System;
using System.Collections.Generic;
using DictionaryApi.Config;
using DictionaryApi.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DictionaryApiTests
{
    [TestClass]
    public class ValueStorageTests
    {
        private const string Key1 = "123";
        private const string Key2 = "654";
        private readonly DateTime _startTime = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        private readonly ExpirationConfig _expirationConfig = new ExpirationConfig
            {DefaultExpirationInSeconds = 10, MaxExpirationInSeconds = 20, CleanupPeriodInSeconds = 30};

        private TestValueStorage GetValueStorage()
        {
            var timeProvider = new TestTimeProvider
            {
                DateTime = _startTime
            };

            return new TestValueStorage(Options.Create(_expirationConfig), timeProvider, Mock.Of<IHostApplicationLifetime>());
        }

        [TestMethod]
        public void Get_ReturnsEmptyCollection()
        {
            Assert.IsTrue(GetValueStorage().Get(Key1).Count == 0);
        }

        [TestMethod]
        public void Get_ReturnsRecordWhenAdded()
        {
            var storage = GetValueStorage();
            storage.Create(Key1, new List<object> {"sample", "value"}, null);
            Assert.IsTrue(storage.Get(Key1).Count == 2);
            Assert.IsTrue((string) storage.Get(Key1)[0] == "sample");
            Assert.IsTrue((string) storage.Get(Key1)[1] == "value");
        }

        [TestMethod]
        public void Get_ReturnsDiRecordWhenAdded()
        {
            var storage = GetValueStorage();
            storage.Create(Key1, new List<object> {"sample"}, null);
            storage.Create(Key2, new List<object> {"value"}, null);

            Assert.IsTrue(storage.Get(Key1).Count == 1);
            Assert.IsTrue(storage.Get(Key2).Count == 1);

            Assert.IsTrue((string) storage.Get(Key1)[0] == "sample");
            Assert.IsTrue((string) storage.Get(Key2)[0] == "value");
        }

        [TestMethod]
        public void Create_OverridesValue()
        {
            var storage = GetValueStorage();
            storage.Create(Key1, new List<object> {"sample"}, null);
            storage.Create(Key1, new List<object> {"value"}, null);

            Assert.IsTrue(storage.Get(Key1).Count == 1);
            Assert.IsTrue((string) storage.Get(Key1)[0] == "value");
        }

        [TestMethod]
        public void Create_DefaultExpirationIsSet()
        {
            var storage = GetValueStorage();
            storage.Create(Key1, new List<object> {"sample"}, null);

            Assert.IsTrue(storage.GetRecord(Key1).ExpirationInterval == _expirationConfig.DefaultExpirationInSeconds);
        }

        [TestMethod]
        public void Create_ExpirationIsSet()
        {
            var storage = GetValueStorage();
            storage.Create(Key1, new List<object> { "sample" }, 20);

            Assert.IsTrue(storage.GetRecord(Key1).ExpirationInterval == 20);
        }

        [TestMethod]
        public void Create_ExpirationIsLimited()
        {
            var storage = GetValueStorage();
            storage.Create(Key1, new List<object> { "sample" }, 100);

            Assert.IsTrue(storage.GetRecord(Key1).ExpirationInterval == _expirationConfig.MaxExpirationInSeconds);
        }

        [TestMethod]
        public void Get_ExtendsExpiration()
        {
            var storage = GetValueStorage();
            storage.Create(Key1, new List<object> { "sample" }, 100);
            storage.TimeProvider.DateTime = _startTime.AddDays(1);
            storage.Get(Key1);

            var record = storage.GetRecord(Key1);
            Assert.IsTrue(record.ExpirationDate == _startTime.AddDays(1).AddSeconds(record.ExpirationInterval));
        }

        [TestMethod]
        public void Cleanup_RemovesOldRecords()
        {
            var storage = GetValueStorage();
            storage.Create(Key1, new List<object> { "sample" }, 100);
            storage.TimeProvider.DateTime = _startTime.AddDays(1);
            storage.Cleanup();

            var record = storage.GetRecord(Key1);
            Assert.IsTrue(record == null);
        }

        [TestMethod]
        public void Cleanup_DoesNotRemoveFreshRecord()
        {
            var storage = GetValueStorage();
            storage.Create(Key1, new List<object> { "sample" }, 100);
            storage.Cleanup();

            var record = storage.GetRecord(Key1);
            Assert.IsTrue(record != null);
        }


        [TestMethod]
        public void Append_DoesNotOverridesValue()
        {
            var storage = GetValueStorage();
            storage.Append(Key1, new List<object> {"sample"}, null);
            storage.Append(Key1, new List<object> {"value"}, null);

            Assert.IsTrue(storage.Get(Key1).Count == 1);
            Assert.IsTrue((string) storage.Get(Key1)[0] == "sample");
        }

        [TestMethod]
        public void Delete_DeletesRecord()
        {
            var storage = GetValueStorage();
            storage.Create(Key1, new List<object> {"sample", "value"}, null);
            storage.Delete(Key1);

            Assert.IsTrue(storage.Get(Key1).Count == 0);
        }

        public class TestValueStorage : ValueStorage
        {
            public TestTimeProvider TimeProvider { get; set; }
            public TestValueStorage(IOptions<ExpirationConfig> expirationConfig, TestTimeProvider timeProvider,
                IHostApplicationLifetime appLifetime) : base(expirationConfig, timeProvider, appLifetime)
            {
                TimeProvider = timeProvider;
            }


            public ValueRecord GetRecord(string key)
            {
                _values.TryGetValue(key, out var value);
                return value;
            }
        }
    }
}