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

			/*
			foreach( var playerData in globalData.Players )
			{
				await db.Add( playerData );
				await db.SaveData( playerData );
			}
			*/

			/*
			await db.Add<MatchData>( globalData.Matches.ToArray() );
			await db.Add<RoundData>( globalData.Rounds.ToArray() );
			await db.Add<LevelData>( globalData.Levels.ToArray() );
			await db.Add<TagData>( globalData.Tags.ToArray() );
			*/
			

			//var backup = await db.GetBackup();

			//var allTags = await db.GetAll<TagData>();
			/*
			foreach( var tagName in globalData.Tags )
			{
				var tagData = await db.GetData<TagData>( tagName );
				await db.SaveData( tagData );
			}

			foreach( var levelName in globalData.Levels )
			{
				var levelData = await db.GetData<LevelData>( levelName );
				await db.SaveData( levelData );
			}
			*/


			/*
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
			*/
		}
	}
}
