using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public interface IDatabaseEntry
	{
		[JsonIgnore]
		string DatabaseIndex { get; }
	}
}
