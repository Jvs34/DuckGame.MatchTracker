using MatchTracker;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchTest
{
	public class FixBullshit
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

			await db.SaveData( globalData );

			async Task<bool> touchMatchOrRound( IWinner matchOrRound )
			{
				if( matchOrRound is MatchData matchData )
				{
					await db.SaveData( matchData );
				}
				else if( matchOrRound is RoundData roundData )
				{
					await db.SaveData( roundData );
				}

				return true;
			}

			await db.IterateOverAllRoundsOrMatches( true , touchMatchOrRound );
			await db.IterateOverAllRoundsOrMatches( false , touchMatchOrRound );
		}
	}
}
