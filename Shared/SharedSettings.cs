using System;
using System.Collections.Generic;
using System.IO;
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

		public String DateTimeToString( DateTime time )
		{
			return time.ToString( timestampFormat );
		}

		public String GetRecordingFolder()
		{
#if DEBUG
			return debugBaseRecordingFolder;
#else
			return baseRecordingFolder;
#endif
		}

		//utility functions for saving, these will obviously not work on the blazor thinghie

		public void SaveMatchData( String matchName , MatchData matchData )
		{
			String matchFolder = Path.Combine( GetRecordingFolder() , matchesFolder );
			String matchPath = Path.Combine( matchFolder , matchName );
			matchPath = Path.ChangeExtension( matchPath , "json" );

			File.WriteAllText( matchPath , JsonConvert.SerializeObject( matchData , Formatting.Indented ) );
		}

		public void SaveRoundData( String roundName , RoundData roundData )
		{
			String roundFolder = Path.Combine( GetRecordingFolder() , roundsFolder );
			String roundPath = Path.Combine( Path.Combine( roundFolder , roundName ) , roundDataFile );

			File.WriteAllText( roundPath , JsonConvert.SerializeObject( roundData , Formatting.Indented ) );
		}

		public void SaveGlobalData( GlobalData globalData )
		{
			String globalDataPath = Path.Combine( GetRecordingFolder() , globalDataFile );
			File.WriteAllText( globalDataPath , JsonConvert.SerializeObject( globalData , Formatting.Indented ) );
		}
	}
}