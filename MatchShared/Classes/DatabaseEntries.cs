using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public class DatabaseEntries<T> : DatabaseEntries where T: IDatabaseEntry
	{
	}

	public class DatabaseEntries : List<string>
	{

	}
}
