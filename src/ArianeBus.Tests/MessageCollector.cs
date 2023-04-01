using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus.Tests
{
	public class MessageCollector
	{
		private readonly List<Person> m_PersonList = new();
		private readonly System.Threading.ManualResetEvent m_ManualResetEvent = new(false);
		private readonly ILogger m_Logger;
		private int m_MessageCount;

		public MessageCollector(ILogger<MessageCollector> logger)
		{
			m_Logger = logger;
			m_MessageCount = 1;
		}

		public int Count
		{
			get
			{
				return m_PersonList.Count;
			}
		}

		public void Reset(int? messageCount = 1)
		{
			m_PersonList.Clear();
			m_ManualResetEvent.Reset();
			m_MessageCount = messageCount.GetValueOrDefault(1);
		}

		public IEnumerable<Person> GetList()
        {
			return m_PersonList;
        }

		public void AddPerson(Person person)
		{
			m_PersonList.Add(person);
			if (m_PersonList.Count >= m_MessageCount)
            {
				m_ManualResetEvent.Set();
			}
		}

		public async Task WaitForReceiveMessage(int millisecond)
        {
			var timeout = DateTime.Now.AddSeconds((millisecond / 1000.0) * -1);
			var success = m_ManualResetEvent.WaitOne(millisecond);
			if (!success)
            {
				m_Logger.LogWarning("Timeout detected");
            }
			var balance = Convert.ToInt32((timeout - DateTime.Now).TotalMilliseconds);
			await Task.Delay(Math.Max(0, balance));
		}
	}
}