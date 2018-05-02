using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MatchTracker
{
	//a settings class that will be used for all match programs and website with blazor
	//this is something that once setup really shouldn't be touched again
	public class SharedSettings
	{
		//global paths
		public String baseRecordingFolder;
		public String debugBaseRecordingFolder; //still debating over this one

		//local paths
		public String roundsFolder = "rounds";
		public String matchesFolder = "matches";
		public String timestampFormat = "yyyy-MM-dd HH-mm-ss";

		public String roundDataFile = "rounddata.json";
		public String roundVideoFile = "video.mp4";
		public String globalDataFile = "data.json";
	}
}