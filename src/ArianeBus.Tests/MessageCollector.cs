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
		private readonly ILogger _logger;
		private int _messageCount;
		public bool _exitRequested { get; set; } = false;

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
			_exitRequested = false;
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
				_exitRequested = true;
			}
		}

		public async Task WaitForReceiveMessage(int millisecond)
        {
			var timeout = DateTime.Now.AddMilliseconds(millisecond);
			while (DateTime.Now < timeout)
			{
				await Task.Yield();
				if (_exitRequested)
				{
					break;
				}
			}
		}
	}
}