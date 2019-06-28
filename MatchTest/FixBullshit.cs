using MatchTracker;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MatchTest
{
	/*
	public class PlayerDataToDatabaseIndexConverter : JsonConverter<PlayerData>
	{
		public override PlayerData ReadJson( JsonReader reader , Type objectType , PlayerData existingValue , bool hasExistingValue , JsonSerializer serializer )
		{
			return null;
		}

		public override void WriteJson( JsonWriter writer , PlayerData value , JsonSerializer serializer )
		{
			//writer.WriteStartArray();
			writer.WriteValue( value.DatabaseIndex );
			//writer.WriteEndArray();
		}
	}
	*/

	public class FixBullshit
	{
		public async Task Run()
		{
			var Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
			.Build();

			BaseGameDatabase db = new FileSystemGameDatabase();
			Configuration.Bind( db.SharedSettings );
			await db.Load();


			/*
			PlayerDataToDatabaseIndexConverter converter = new PlayerDataToDatabaseIndexConverter();

			foreach( var roundData in await db.GetAllData<RoundData>() )
			{
				if( !db.Serializer.Converters.Contains( converter ) )
				{
					db.Serializer.Converters.Add( converter );
				}

				await db.SaveData( roundData );
			}


			*/
			Console.WriteLine( "Done" );


			//db.Serializer.Converters.Contains(  )



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
