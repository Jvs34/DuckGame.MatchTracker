using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//this is what accessed by the website, so it will list the name of the matches that were tracked
	public class GlobalData : IDatabaseEntry
	{
		public string DatabaseIndex => nameof( GlobalData );
	}
}