using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DictionaryApi.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DictionaryApi.Storage
{
    public interface IValueStorage
    {
        void Create(string key, List<object> values, int? expirationInSeconds);
        bool Append(string key, List<object> values, int? expirationInSeconds);
        void Delete(string key);
        List<object> Get(string key);
        void Cleanup();
    }

    public class ValueStorage : IValueStorage
    {
        protected readonly ConcurrentDictionary<string, ValueRecord> _values;
        private readonly ExpirationConfig _expirationConfig;
        private readonly ITimeProvider _timeProvider;

        private const string DataFileName = "data.json";

        public ValueStorage(IOptions<ExpirationConfig> expirationConfig, ITimeProvider timeProvider, IHostApplicationLifetime appLifetime)
        {
            _timeProvider = timeProvider;
            _expirationConfig = expirationConfig.Value;
            _values = new ConcurrentDictionary<string, ValueRecord>();

            Load();
            appLifetime.ApplicationStopping.Register(Persist);
        }

        public void Create(string key, List<object> values, int? expirationInSeconds)
        {
            var validatedExpiration = GetValidatedExpiration(expirationInSeconds);

            _values.AddOrUpdate(key, x => new ValueRecord
            {
                ExpirationDate = _timeProvider.GetDatetime().AddSeconds(validatedExpiration),
                Values = values,
                ExpirationInterval = validatedExpiration
            }, (x, record) =>
            {
                record.ExpirationDate = _timeProvider.GetDatetime().AddSeconds(validatedExpiration);
                record.Values = values;
                record.ExpirationInterval = validatedExpiration;
                return record;
            });
        }

        public bool Append(string key, List<object> values, int? expirationInSeconds)
        {
            var validatedExpiration = GetValidatedExpiration(expirationInSeconds);

            return _values.TryAdd(key, new ValueRecord
            {
                ExpirationDate = _timeProvider.GetDatetime().AddSeconds(validatedExpiration),
                Values = values,
                ExpirationInterval = validatedExpiration
            });
        }

        public void Delete(string key)
        {
            _values.Remove(key, out _);
        }

        public List<object> Get(string key)
        {
            var exists = _values.TryGetValue(key, out var value);
            if (!exists)
                return new List<object>();

            value.ExpirationDate = _timeProvider.GetDatetime().AddSeconds(value.ExpirationInterval);
            return value.Values;
        }

        public void Cleanup()
        {
            foreach (var key in _values.Keys)
            {
                if (_values.TryGetValue(key, out var value))
                    if (value.ExpirationDate < _timeProvider.GetDatetime())
                        _values.Remove(key, out _);
            }
        }

        public void Persist()
        {
            var data = _values
                .Select(x => new PersistenceRecord
                {
                    Key = x.Key,
                    Value = x.Value
                }).ToArray();

            var serializedData = JsonSerializer.Serialize(data);
            File.WriteAllText(DataFileName, serializedData);

        }

        public void Load()
        {
            try
            {
                var content = File.ReadAllText(DataFileName);
                var data = JsonSerializer.Deserialize<PersistenceRecord[]>(content);

                foreach (var record in data)
                {
                    _values.TryAdd(record.Key, record.Value);
                }
            }
            catch
            {
                // ignored
            }
        }

        private int GetValidatedExpiration (int? expirationInSeconds) => Math.Min(expirationInSeconds ?? _expirationConfig.DefaultExpirationInSeconds,
            _expirationConfig.MaxExpirationInSeconds);
    }
}