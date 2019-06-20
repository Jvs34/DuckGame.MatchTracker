using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	public static class DatabaseExtensions
	{
		public static HashSet<Type> DatabaseTypes { get; } = new HashSet<Type>();
		public static Dictionary<Type , PropertyInfo> AllTypesOfDatabaseEntry { get; } = new Dictionary<Type , PropertyInfo>();

		static DatabaseExtensions()
		{
			foreach( var type in typeof( DatabaseExtensions ).Assembly.GetTypes().Where( type => type.GetInterfaces().Contains( typeof( IDatabaseEntry ) ) ) )
			{
				DatabaseTypes.Add( type );
			}

			//now try to get which properties of globaldata have the generic attribute
			var globalDataType = typeof( GlobalData );

			foreach( var dbEntry in globalDataType.GetProperties().Where( x => x.PropertyType.IsGenericType && x.PropertyType.BaseType == typeof( DatabaseEntries ) ) )
			{
				//get the generic argument from this property's type
				Type propertyType = dbEntry.PropertyType;

				Type dbEntryType = propertyType.GenericTypeArguments.FirstOrDefault();

				if( dbEntryType == null || !DatabaseTypes.Contains( dbEntryType ) )
				{
					continue;
				}

				AllTypesOfDatabaseEntry.Add( dbEntryType , dbEntry );
			}
		}


		public static async Task<Dictionary<string , Dictionary<string , IDatabaseEntry>>> GetBackup( this IGameDatabase db )
		{
			var mainCollection = new Dictionary<string , Dictionary<string , IDatabaseEntry>>();

			foreach( var dbEntryType in DatabaseTypes )
			{
				var collection = new Dictionary<string , IDatabaseEntry>();

				mainCollection.Add( dbEntryType.Name , collection );
			}

			var globalData = await db.GetData<GlobalData>();
			mainCollection [nameof( GlobalData )] [globalData.DatabaseIndex] = globalData;


			foreach( var roundName in await db.GetAll<RoundData>() )
			{
				mainCollection [nameof( RoundData )] [roundName] = await db.GetData<RoundData>( roundName );
			}

			foreach( var matchName in await db.GetAll<MatchData>() )
			{
				mainCollection [nameof( MatchData )] [matchName] = await db.GetData<MatchData>( matchName );
			}

			foreach( var levelName in await db.GetAll<LevelData>() )
			{
				mainCollection [nameof( LevelData )] [levelName] = await db.GetData<LevelData>( levelName );
			}

			foreach( var tagName in await db.GetAll<TagData>() )
			{
				mainCollection [nameof( TagData )] [tagName] = await db.GetData<TagData>( tagName );
			}


			return mainCollection;
		}

		public static async Task IterateOverAllRoundsOrMatches( this IGameDatabase db , bool matchOrRound , Func<IWinner , Task<bool>> callback )
		{
			if( callback == null )
				return;

			List<Task> tasks = new List<Task>();

			var tokenSource = new CancellationTokenSource();

			foreach( string matchOrRoundName in matchOrRound ? (DatabaseEntries) await db.GetAll<MatchData>() : await db.GetAll<RoundData>() )
			{
				tasks.Add( IteratorTask( db , matchOrRound , callback , tasks , tokenSource , matchOrRoundName ) );
			}

			await Task.WhenAll( tasks );
		}

		private static async Task IteratorTask( IGameDatabase db , bool matchOrRound , Func<IWinner , Task<bool>> callback , List<Task> tasks , CancellationTokenSource tokenSource , string matchOrRoundName )
		{
			if( tokenSource.IsCancellationRequested )
			{
				return;
			}

			IWinner iterateItem = matchOrRound ?
				await db.GetData<MatchData>( matchOrRoundName ) as IWinner :
				await db.GetData<RoundData>( matchOrRoundName ) as IWinner;

			if( !await callback( iterateItem ) )
			{
				tokenSource.Cancel();

				//immediately clear the tasks list so we don't await anything for no reason anymore
				//the tasks may still run but they won't get any further than the cancellation request check
				tasks.Clear();
			}
		}

		public static async Task AddTag( this IGameDatabase db , string unicode , string fancyName , ITagsList tagsList = null )
		{
			//always get globalData

			GlobalData globalData = await db.GetData<GlobalData>();
			string emojiDatabaseIndex = string.Join( " " , Encoding.UTF8.GetBytes( unicode ) );

			//now check if we exist
			TagData tagData = await db.GetData<TagData>( emojiDatabaseIndex );

			if( tagData == null )
			{
				tagData = new TagData()
				{
					Name = emojiDatabaseIndex ,
					Emoji = unicode ,
					FancyName = fancyName ,
				};

				await db.SaveData( tagData );
			}

			if( !globalData.Tags.Contains( emojiDatabaseIndex ) )
			{
				globalData.Tags.Add( emojiDatabaseIndex );
				await db.SaveData( globalData );
			}

			if( tagsList?.Tags.Contains( emojiDatabaseIndex ) == false )
			{
				tagsList.Tags.Add( emojiDatabaseIndex );
			}
		}

		public static async Task<DatabaseEntries<T>> GetAll<T>( this IGameDatabase db , string globalDataName = "" ) where T : IDatabaseEntry
		{
			DatabaseEntries<T> databaseIndexes = new DatabaseEntries<T>();
			var globalData = await db.GetData<GlobalData>( globalDataName );

			//TODO: find a way to automate this, maybe with reflection?
			if( globalData != null && typeof( T ) != typeof( GlobalData ) )
			{
				if( AllTypesOfDatabaseEntry.TryGetValue( typeof( T ) , out var returnProp ) )
				{
					DatabaseEntries<T> returnedEntries = (DatabaseEntries<T>) returnProp.GetValue( globalData );

					databaseIndexes.AddRange( returnedEntries );
				}

				return databaseIndexes;
			}
			else if( globalData != null )
			{
				databaseIndexes.Add( globalData.DatabaseIndex );
			}

			return databaseIndexes;
		}
	}
}
