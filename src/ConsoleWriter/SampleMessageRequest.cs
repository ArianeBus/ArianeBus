using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleWriter;

public class SampleMessageRequest 
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public int RowNumber { get; set; }

	public override string ToString()
	{
		return $"{MessageId}.{RowNumber}";
	}
}
