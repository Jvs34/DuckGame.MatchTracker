using MatchTracker;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace MatchRecorder
{
	public class MatchRecorderServer : IDisposable
	{
		private bool disposedValue;
		private IRecorder RecorderHandler { get; }
		public BotSettings BotSettings { get; } = new BotSettings();
		public OBSSettings OBSSettings { get; } = new OBSSettings();
		public MatchData CurrentMatch { get; private set; }
		public RoundData CurrentRound { get; private set; }
		public IGameDatabase GameDatabase { get; }
		public bool IsRecordingRound => RecorderHandler.IsRecording;
		public bool IsRecordingMatch { get; set; }
		public string SettingsPath { get; }
		private IConfigurationRoot Configuration { get; }

		public MatchRecorderServer( string settingsPath )
		{
			SettingsPath = settingsPath;
			GameDatabase = new FileSystemGameDatabase();

			Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( settingsPath , "Settings" ) )
#if DEBUG
				.AddJsonFile( "shared_debug.json" )
#else
				.AddJsonFile( "shared.json" )
#endif
				.AddJsonFile( "bot.json" )
				.AddJsonFile( "obs.json" )
			.Build();

			Configuration.Bind( GameDatabase.SharedSettings );
			Configuration.Bind( BotSettings );
			Configuration.Bind( OBSSettings );

			RecorderHandler = new ObsLocalRecorder( this );
		}

		public void StartRecordingRound()
		{
			RecorderHandler?.StartRecordingRound();
		}

		public void StopRecordingRound()
		{
			if( CurrentRound != null )
			{
				ShowHUDmessage( $"Recorded {CurrentRound.Name}" );
			}

			RecorderHandler?.StopRecordingRound();
		}

		internal void StartRecordingMatch()
		{
			if( CurrentRound != null )
			{
				ShowHUDmessage( $"Recorded {CurrentRound.Name}" );
			}
			RecorderHandler?.StartRecordingMatch();
		}

		internal void StopRecordingMatch()
		{
			if( CurrentMatch != null )
			{
				ShowHUDmessage( $"Recorded Match{CurrentMatch.Name}" );
			}

			RecorderHandler?.StopRecordingMatch();
		}

		public RoundData StartCollectingRoundData( DateTime startTime )
		{
			DuckGame.Level lvl = DuckGame.Level.current;

			CurrentRound = new RoundData()
			{
				MatchName = CurrentMatch?.Name ,
				LevelName = lvl.level ,
				TimeStarted = startTime ,
				Name = GameDatabase.SharedSettings.DateTimeToString( startTime ) ,
				RecordingType = RecorderHandler.ResultingRecordingType ,
			};

			if( CurrentMatch != null )
			{
				CurrentMatch.Rounds.Add( GameDatabase.SharedSettings.DateTimeToString( CurrentRound.TimeStarted ) );
			}

			return CurrentRound;
		}

		public RoundData StopCollectingRoundData( DateTime endTime )
		{
			if( CurrentRound == null )
			{
				return null;
			}

			AddTeamAndPlayerData( CurrentRound );

			DuckGame.Team winner = null;

			if( DuckGame.GameMode.lastWinners.Count > 0 )
			{
				winner = DuckGame.GameMode.lastWinners.First()?.team;
			}

			if( winner != null )
			{
				CurrentRound.Winner = CreateTeamDataFromTeam( winner , CurrentRound );
			}

			CurrentRound.TimeEnded = endTime;

			GameDatabase.SaveData( CurrentRound ).Wait();

			GameDatabase.Add( CurrentRound ).Wait();

			RoundData newRoundData = CurrentRound;

			CurrentRound = null;

			return newRoundData;
		}

		public MatchData StartCollectingMatchData( DateTime time )
		{
			CurrentMatch = new MatchData
			{
				TimeStarted = time ,
				Name = GameDatabase.SharedSettings.DateTimeToString( time ) ,
			};

			return CurrentMatch;
		}

		public MatchData StopCollectingMatchData( DateTime time )
		{
			if( CurrentMatch == null )
			{
				return null;
			}

			CurrentMatch.TimeEnded = time;

			AddTeamAndPlayerData( CurrentMatch );

			DuckGame.Team winner = null;

			if( DuckGame.Teams.winning.Count > 0 )
			{
				winner = DuckGame.Teams.winning.FirstOrDefault();
			}

			if( winner != null )
			{
				CurrentMatch.Winner = CreateTeamDataFromTeam( winner , CurrentMatch );
			}

			GameDatabase.SaveData( CurrentMatch ).Wait();
			GameDatabase.Add( CurrentMatch ).Wait();

			MatchData newMatchData = CurrentMatch;

			CurrentMatch = null;

			return newMatchData;
		}

		public void Update() => RecorderHandler?.Update();

		#region UTILITY

		public void ShowHUDmessage( string message , float lifetime = 1f )
		{
			var cornerMessage = DuckGame.HUD.AddCornerMessage( DuckGame.HUDCorner.TopLeft , message );
			cornerMessage.slide = 1;
			cornerMessage.willDie = true;
			cornerMessage.life = lifetime;
		}

		public bool IsLevelRecordable( DuckGame.Level level ) => level is DuckGame.GameLevel;

		public void AddTeamAndPlayerData( IWinner winnerObject )
		{
			foreach( DuckGame.Team team in DuckGame.Teams.active )
			{
				winnerObject.Teams.Add( CreateTeamDataFromTeam( team , winnerObject ) );
			}

			foreach( DuckGame.Profile pro in DuckGame.Profiles.activeNonSpectators )
			{
				PlayerData ply = CreatePlayerDataFromProfile( pro , winnerObject );
				winnerObject.Players.Add( ply.DatabaseIndex );
			}

			foreach( TeamData teamData in winnerObject.Teams )
			{
				DuckGame.Team team = DuckGame.Teams.active.Find( x => x.name == teamData.HatName );
				if( team != null )
				{
					foreach( DuckGame.Profile pro in team.activeProfiles )
					{
						teamData.Players.Add( CreatePlayerDataFromProfile( pro , winnerObject ).DatabaseIndex );
					}
				}
			}
		}

		public void GatherLevelData( DuckGame.Level level )
		{
			string levelID = level.level;

			MatchTracker.LevelData levelData = GameDatabase.GetData<MatchTracker.LevelData>( levelID ).Result;

			if( levelData == null )
			{
				levelData = CreateLevelDataFromLevel( levelID );

				if( levelData != null )
				{
					GameDatabase.SaveData( levelData ).Wait();
					GameDatabase.Add( levelData ).Wait();
				}
			}
		}

		private static MatchTracker.LevelData CreateLevelDataFromLevel( string levelId )
		{
			DuckGame.LevelData dgLevelData = DuckGame.Content.GetLevel( levelId );

			return dgLevelData is null ? null : new MatchTracker.LevelData()
			{
				LevelName = levelId ,
				IsOnlineMap = dgLevelData.metaData.online ,
				FilePath = dgLevelData.GetPath() ,
				IsCustomMap = dgLevelData.GetLocation() != DuckGame.LevelLocation.Content ,
				Author = dgLevelData.workshopData?.author ,
				Description = dgLevelData.workshopData?.description
			};
		}

		private PlayerData CreatePlayerDataFromProfile( DuckGame.Profile profile , IWinner winnerObject )
		{
			string onlineID = profile.steamID.ToString();

			string userId = DuckGame.Network.isActive ? onlineID : profile.id;

			PlayerData pd = GameDatabase.GetData<PlayerData>( userId ).Result;

			if( pd == null )
			{
				pd = GameDatabase.GetAllData<PlayerData>().Result.Find( x => x.DiscordId.ToString().Equals( userId ) );
			}

			if( pd == null )
			{
				pd = new PlayerData
				{
					UserId = userId ,
					Name = profile.name ,
				};

				//last resort, create it now

				GameDatabase.Add( pd ).Wait();
				GameDatabase.SaveData( pd ).Wait();
			}

			return pd;
		}

		private TeamData CreateTeamDataFromTeam( DuckGame.Team team , IWinner winnerObject )
		{
			//try to find a teamobject that's already there
			TeamData td = null;

			if( winnerObject != null )
			{
				td = winnerObject.Teams.Find( x => x.HatName == team.name );
			}

			if( td == null )
			{
				td = new TeamData()
				{
					HasHat = team.hasHat ,
					Score = team.score ,
					HatName = team.name ,
					IsCustomHat = team.customData != null ,
				};
			}

			return td;
		}

		protected virtual void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					// TODO: dispose managed state (managed objects)
					GameDatabase?.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}
		#endregion UTILITY
	}

}