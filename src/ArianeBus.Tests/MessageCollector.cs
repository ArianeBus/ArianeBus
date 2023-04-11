using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus.Tests
{
	public class MessageCollector
	{
		private readonly ConcurrentDictionary<Guid, Person> _personList = new();
		private readonly System.Threading.ManualResetEvent _manualResetEvent = new(false);
		private readonly ILogger _logger;
		private int _messageCount;

		public MessageCollector(ILogger<MessageCollector> logger)
		{
			_logger = logger;
			_messageCount = 1;
		}

		public int Count
		{
			get
			{
				return _personList.Count;
			}
		}

		public void Reset(int? messageCount = 1)
		{
			_personList.Clear();
			_manualResetEvent.Reset();
			_messageCount = messageCount.GetValueOrDefault(1);
		}

		public IEnumerable<Person> GetList()
        {
			return _personList.Select(i => i.Value);
        }

		public void AddPerson(Person person)
		{
			if (!_personList.TryAdd(Guid.NewGuid(), person))
			{
				throw new Exception("try to add person failed");
			}
			if (_personList.Count >= _messageCount)
            {
				_manualResetEvent.Set();
			}
		}

		public async Task WaitForReceiveMessage(int millisecond)
        {
			var timeout = DateTime.Now.AddSeconds((millisecond / 1000.0) * -1);
			var success = _manualResetEvent.WaitOne(millisecond);
			if (!success)
            {
				_logger.LogWarning("Timeout detected");
				return;
            }
			var balance = Convert.ToInt32((timeout - DateTime.Now).TotalMilliseconds);
			await Task.Delay(Math.Max(0, balance));
		}
	}
}