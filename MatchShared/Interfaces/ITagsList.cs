using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public interface ITagsList
	{
		DatabaseEntries<TagData> Tags { get; set; }
	}
}
