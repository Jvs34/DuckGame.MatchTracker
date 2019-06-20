using MatchTracker;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MatchTest
{
	public class TestFS
	{

		public async Task Run()
		{
			var Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
			.Build();

			IGameDatabase db = new FileSystemGameDatabase();

			Configuration.Bind( db.SharedSettings );

			await db.Load();

			GlobalData globalData = await db.GetData<GlobalData>();

			MatchData matchData = await db.GetData<MatchData>( ( await db.GetAll<MatchData>() ).FirstOrDefault() );

			RoundData roundData = await db.GetData<RoundData>( matchData.Rounds.FirstOrDefault() );
		}
	}
}
