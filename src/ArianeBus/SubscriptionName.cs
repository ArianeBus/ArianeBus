using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

public record SubscriptionName
{
    public SubscriptionName(string value)
    {
        this.Value = value;
    }
    public string Value { get; init; }

	public override string ToString()
	{
		return $"{Value}";
	}

}
