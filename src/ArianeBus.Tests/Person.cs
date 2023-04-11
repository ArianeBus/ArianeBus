using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArianeBus.Tests 
{
	public class Person 
	{
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
		public string LastName { get; set; } = null!;
		public bool IsProcessed { get; set; } = false;
        public string FromQueue { get; set; } = null!;
		public string FromSubscription { get; set; } = null!;
        public string FromMessageReader { get; set; } = null!;
        public static Person CreateTestPerson()
		{
			var person = new Person();
			person.FirstName = Guid.NewGuid().ToString();
			person.LastName = Guid.NewGuid().ToString();
			person.IsProcessed = false;
			return person;
		}
	}
}
