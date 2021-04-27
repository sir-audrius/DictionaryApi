using System;
using System.Collections.Generic;
using DictionaryApi.Requests;
using DictionaryApi.Storage;
using Microsoft.AspNetCore.Mvc;

namespace DictionaryApi
{
    [ApiController]
    [Route("[controller]")]
    public class ValueController : ControllerBase
    {
        private readonly IValueStorage _valueStorage;

        public ValueController(IValueStorage valueStorage)
        {
            _valueStorage = valueStorage;
        }

        [HttpGet("/{key}")]
        public IEnumerable<object> Get(string key)
        {
            return _valueStorage.Get(key);
        }

        [HttpPost("/{key}")]
        public ActionResult Create(string key, [FromBody]CreateRequest request)
        {
            _valueStorage.Create(key, request.Values, request.ExpirationInSeconds);
            return Created(new Uri(key, UriKind.Relative), _valueStorage.Get(key));
        }

        [HttpPut("/{key}")]
        public ActionResult Append(string key, [FromBody]AppendRequest request)
        {
            if (!_valueStorage.Append(key, request.Values, request.ExpirationInSeconds))
                return Conflict();
            return Created(new Uri(key, UriKind.Relative), _valueStorage.Get(key));
        }

        [HttpDelete("/{key}")]
        public ActionResult Delete(string key)
        {
            _valueStorage.Delete(key);
            return Ok();
        }
    }
}