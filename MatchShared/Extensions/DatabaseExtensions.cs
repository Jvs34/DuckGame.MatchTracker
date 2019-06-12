using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchTracker
{
	public static class DatabaseExtensions
	{
		public static async Task<Dictionary<string , Dictionary<string , IDatabaseEntry>>> GetBackup( this IGameDatabase db )
		{
			var mainCollection = new Dictionary<string , Dictionary<string , IDatabaseEntry>>()
			{
				[nameof( GlobalData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( MatchData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( RoundData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( LevelData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( TagData )] = new Dictionary<string , IDatabaseEntry>() ,
			};

			var globalData = await db.GetData<GlobalData>();

			mainCollection [nameof( GlobalData )] [globalData.DatabaseIndex] = globalData;

			foreach( var roundName in globalData.Rounds )
			{
				RoundData roundData = await db.GetData<RoundData>( roundName );

				mainCollection [nameof( RoundData )] [roundData.DatabaseIndex] = roundData;
			}

			foreach( var matchName in globalData.Matches )
			{
				MatchData matchData = await db.GetData<MatchData>( matchName );
				mainCollection [nameof( MatchData )] [matchData.DatabaseIndex] = matchData;
			}

			foreach( var levelName in globalData.Levels )
			{
				LevelData levelData = await db.GetData<LevelData>( levelName );
				mainCollection [nameof( LevelData )] [levelData.DatabaseIndex] = levelData;
			}

			foreach( var tagName in globalData.Tags )
			{
				TagData tagData = await db.GetData<TagData>( tagName );
				mainCollection [nameof( TagData )] [tagData.DatabaseIndex] = tagData;
			}


			return mainCollection;
		}

		public static async Task IterateOverAllRoundsOrMatches( this IGameDatabase db , bool matchOrRound , Func<IWinner , Task<bool>> callback )
		{
			if( callback == null )
				return;

			GlobalData globalData = await db.GetData<GlobalData>();

			List<Task> tasks = new List<Task>();

			foreach( string matchOrRoundName in matchOrRound ? globalData.Matches : globalData.Rounds )
			{
				Func<Task> newTask = async () =>
				{
					IWinner iterateItem = matchOrRound ?
						await db.GetData<MatchData>( matchOrRoundName ) as IWinner :
						await db.GetData<RoundData>( matchOrRoundName ) as IWinner;

					await callback( iterateItem );
				};

				tasks.Add( newTask() );
			}

			await Task.WhenAll( tasks );
		}

		public static async Task AddTag( this IGameDatabase db , string unicode , ITagsList tagsList = null , IDatabaseEntry databaseEntry = null )
		{
			//always get globalData

			GlobalData globalData = await db.GetData<GlobalData>();
			string emojiDatabaseIndex = string.Join( " " , Encoding.UTF8.GetBytes( unicode ) );

			//now check if we exist
			TagData tagData = await db.GetData<TagData>( emojiDatabaseIndex );

			//we don't exist, 
			if( tagData == null )
			{
				tagData = new TagData()
				{
					Name = emojiDatabaseIndex ,
					Emoji = unicode ,
				};

				await db.SaveData( tagData );
			}

			if( !globalData.Tags.Contains( emojiDatabaseIndex ) )
			{
				globalData.Tags.Add( emojiDatabaseIndex );
				await db.SaveData( globalData );
			}

			if( tagsList != null && databaseEntry != null && !tagsList.Tags.Contains( emojiDatabaseIndex ) )
			{
				tagsList.Tags.Add( emojiDatabaseIndex );
				await db.SaveData( databaseEntry );
			}
		}
	}
}
