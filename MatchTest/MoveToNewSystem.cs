using MatchTracker;
using System.IO;
using System.Threading.Tasks;

namespace MatchTest
{
	public class MoveToNewSystem
	{

		public async Task Run()
		{
			string path = @"E:\DuckGameRecordings";


			/*
			var matchesFiles = Directory.EnumerateFiles( Path.Combine( path , nameof( MatchData ) ) );

			foreach( var matchFile in matchesFiles )
			{
				//create a directory for it with it's name then rename it to data.json
				var databaseIndex = Path.GetFileNameWithoutExtension( matchFile );

				Directory.CreateDirectory( Path.Combine( path , nameof( MatchData ) , databaseIndex ) );

				File.Move( matchFile , Path.Combine( path , nameof( MatchData ) , databaseIndex , "data.json" ) );
			}
			*/

			var roundFiles = Directory.EnumerateFiles( Path.Combine( path , nameof( RoundData ) ) , "*.json" , SearchOption.AllDirectories );

			foreach( var roundFile in roundFiles )
			{
				var newPath = Path.GetDirectoryName( roundFile );

				File.Move( roundFile , Path.Combine( newPath , "data.json" ) );
			}
		}
	}
}
