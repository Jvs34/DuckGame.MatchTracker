using DuckGame;
using Harmony;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MatchRecorder
{
	public class MatchRecorderHandler
	{
		private IRecorder RecorderHandler { get; }
		public BotSettings BotSettings { get; } = new BotSettings();
		public OBSSettings OBSSettings { get; } = new OBSSettings();
		public MatchData CurrentMatch { get; private set; }
		public RoundData CurrentRound { get; private set; }
		public IGameDatabase GameDatabase { get; }
		public bool IsRecording => RecorderHandler.IsRecording;
		public string ModPath { get; }
		private IConfigurationRoot Configuration { get; }

		/// <summary>
		/// Only used if on the discord steam branch, or the discord version itself I assume?
		/// </summary>
		private static readonly PropertyInfo onlineIDField = typeof( Profile ).GetProperty( "onlineID" );

		public MatchRecorderHandler( string modPath )
		{
			ModPath = modPath;
			GameDatabase = new FileSystemGameDatabase();

			Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( modPath , "Settings" ) )
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

			RecorderHandler = new ObsRecorder( this );
		}

		//only record game levels for now since we're kind of tied to the gounvirtual stuff
		public bool IsLevelRecordable( Level level )
		{
			return level is GameLevel;
		}

		public RoundData StartCollectingRoundData( DateTime startTime )
		{
			Level lvl = Level.current;

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

		public void StartRecording()
		{
			HUD.CloseCorner( HUDCorner.TopLeft );
			RecorderHandler?.StartRecording();
		}

		public void AddTeamAndPlayerData( IWinner winnerObject )
		{
			foreach( Team team in Teams.active )
			{
				winnerObject.Teams.Add( CreateTeamDataFromTeam( team , winnerObject ) );
			}

			foreach( Profile pro in Profiles.active )
			{
				PlayerData ply = CreatePlayerDataFromProfile( pro , winnerObject );
				winnerObject.Players.Add( ply.DatabaseIndex );
			}

			foreach( TeamData teamData in winnerObject.Teams )
			{
				Team team = Teams.active.Find( x => x.name == teamData.HatName );
				if( team != null )
				{
					foreach( Profile pro in team.activeProfiles )
					{
						teamData.Players.Add( CreatePlayerDataFromProfile( pro , winnerObject ).DatabaseIndex );
					}
				}
			}
		}

		public RoundData StopCollectingRoundData( DateTime endTime )
		{
			if( CurrentRound == null )
			{
				return null;
			}

			AddTeamAndPlayerData( CurrentRound );

			Team winner = null;

			if( GameMode.lastWinners.Count > 0 )
			{
				winner = GameMode.lastWinners.First()?.team;
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

		public void StopRecording()
		{
			if( CurrentRound != null )
			{
				var cornerMessage = HUD.AddCornerMessage( HUDCorner.TopLeft , $"Recorded {CurrentRound.Name}" );
				cornerMessage.slide = 1;
			}

			RecorderHandler?.StopRecording();


		}

		public void TryCollectingMatchData()
		{
			//try saving the match if there's one and it's got at least one round
			if( CurrentMatch?.Rounds.Count > 0 )
			{
				StopCollectingMatchData();
			}

			//try starting to collect match data regardless, it'll only be saved if there's at least one round later on
			StartCollectingMatchData();
		}

		public void Update()
		{
#if DEBUGSHIT
			if( Keyboard.Down( Keys.LeftShift ) && Keyboard.Pressed( Keys.D0 ) )
			{
				var levels = Content.GetLevels( "deathmatch" , LevelLocation.Content );
				//TryTakingScreenshots();
			}
#endif


			RecorderHandler?.Update();
		}

		public void TryTakingScreenshots()
		{
			//get all the levels that are currently saved in the database and make a thumbnail out of it

			foreach( var levelID in GameDatabase.GetAll<MatchTracker.LevelData>().Result )
			{
				string levelPreviewFile = GameDatabase.SharedSettings.GetLevelPreviewPath( levelID );

				if( File.Exists( levelPreviewFile ) )
				{
					continue;
				}

				var bitmap = TakeScreenshot( levelID );

				if( bitmap != null )
				{
					using( var fileStream = File.Create( levelPreviewFile ) )
					{
						bitmap.Save( fileStream , System.Drawing.Imaging.ImageFormat.Png );
					}
				}

			}
		}

		private System.Drawing.Bitmap TakeScreenshot( string levelID )
		{
			System.Drawing.Bitmap screenshot = null;
			/*
			DuckGame.LevelData levelData = Content.GetLevel( levelID );

			if( levelData != null )
			{
				var rtTest = Content.GeneratePreview( levelData , true );

				var imageData = rtTest.GetData();

				int w = rtTest.width;
				int h = rtTest.height;

				screenshot = new System.Drawing.Bitmap( w , h , System.Drawing.Imaging.PixelFormat.Format32bppArgb );

				for( int x = 0; x < w; x++ )
				{
					for( int y = 0; y < h; y++ )
					{
						int arrayIndex = ( y * w ) + x;
						DuckGame.Color c = imageData [arrayIndex];
						screenshot.SetPixel( x , y , System.Drawing.Color.FromArgb( c.a , c.r , c.g , c.b ) );
					}
				}

			}
			*/
			return screenshot;
		}

		/*
		private void TryTakingScreenshots()
		{
			string levelPath = Path.Combine( GameDatabase.SharedSettings.GetRecordingFolder() , GameDatabase.SharedSettings.LevelsPreviewFolder );

			var levels = Content.GetLevels( "deathmatch" , LevelLocation.Content );

			foreach( string levelid in levels )
			{
				DuckGame.LevelData levelData = Content.GetLevel( levelid , LevelLocation.Content );

				var rtTest = Content.GeneratePreview( levelid , null , true );

				var imageData = rtTest.GetData();

				int w = rtTest.width;
				int h = rtTest.height;

				System.Drawing.Bitmap pic = new System.Drawing.Bitmap( w , h , System.Drawing.Imaging.PixelFormat.Format32bppArgb );

				for( int x = 0; x < w; x++ )
				{
					for( int y = 0; y < h; y++ )
					{
						int arrayIndex = ( y * w ) + x;
						DuckGame.Color c = imageData [arrayIndex];
						pic.SetPixel( x , y , System.Drawing.Color.FromArgb( c.a , c.r , c.g , c.b ) );
					}
				}

				pic.Save( Path.Combine( levelPath , $"{levelid}.png" ) , System.Drawing.Imaging.ImageFormat.Png );
			}
		}
		*/

		public void GatherLevelData( Level level )
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

		/*
		public void GatherLevelData()
		{
			MatchTracker.GlobalData globalData = GameDatabase.GetData<MatchTracker.GlobalData>().Result;

			var deathmatchLevels = Content.GetLevels( "deathmatch" , LevelLocation.Content );

			//
			//go through each level and see if its info has been added to the database

			foreach( string levelId in deathmatchLevels )
			{
				MatchTracker.LevelData mtLevelData = globalData.Levels.FirstOrDefault( x => x.LevelName == levelId );

				if( mtLevelData == null )
				{
					mtLevelData = CreateLevelDataFromLevel( levelId );
					if( mtLevelData != null )
					{
						globalData.Levels.Add( mtLevelData );
					}
				}

			}

			GameDatabase.SaveData( globalData );
		}
		*/

		private MatchTracker.LevelData CreateLevelDataFromLevel( string levelId )
		{
			DuckGame.LevelData dgLevelData = Content.GetLevel( levelId );

			return dgLevelData is null ? null : new MatchTracker.LevelData()
			{
				LevelName = levelId ,
				IsOnlineMap = dgLevelData.metaData.online ,
				FilePath = dgLevelData.GetPath() ,
				IsCustomMap = dgLevelData.GetLocation() != LevelLocation.Content ,
				Author = dgLevelData.workshopData?.author ,
				Description = dgLevelData.workshopData?.description
			};
		}

		private PlayerData CreatePlayerDataFromProfile( Profile profile , IWinner winnerObject )
		{

			string discordOrSteamID = onlineIDField != null
				? onlineIDField.GetValue( profile ).ToString()
				: profile.steamID.ToString();

			string userId = Network.isActive ? discordOrSteamID : profile.id;

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

		private TeamData CreateTeamDataFromTeam( Team team , IWinner winnerObject )
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

		private MatchData StartCollectingMatchData()
		{
			CurrentMatch = new MatchData
			{
				TimeStarted = DateTime.Now ,
				Name = GameDatabase.SharedSettings.DateTimeToString( DateTime.Now ) ,
			};

			return CurrentMatch;
		}

		private MatchData StopCollectingMatchData()
		{
			if( CurrentMatch == null )
			{
				return null;
			}

			CurrentMatch.TimeEnded = DateTime.Now;

			AddTeamAndPlayerData( CurrentMatch );

			Team winner = null;

			if( Teams.winning.Count > 0 )
			{
				winner = Teams.winning.FirstOrDefault();
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

		public void StartFrame()
		{
			if( RecorderHandler.IsRecording )
			{
				RecorderHandler.StartFrame();
			}
		}

		public void OnTextureDraw( Tex2D texture , DuckGame.Vec2 position , DuckGame.Rectangle? sourceRectangle , DuckGame.Color color , float rotation , DuckGame.Vec2 origin , DuckGame.Vec2 scale , int effects , Depth depth = default( Depth ) )
		{
			if( !RecorderHandler.IsRecording || Graphics.currentLayer == Layer.Console || Graphics.currentLayer == Layer.HUD )
			{
				return;
			}

			RecorderHandler.OnTextureDraw( texture , position , sourceRectangle , color , rotation , origin , scale , effects , depth );
		}

		internal void EndFrame()
		{
			if( RecorderHandler.IsRecording )
			{
				RecorderHandler.EndFrame();
			}
		}

		public int OnStartStaticDraw()
		{
			if( RecorderHandler.IsRecording )
			{
				return RecorderHandler.OnStartStaticDraw();
			}

			return 0;
		}

		public void OnFinishStaticDraw()
		{
			if( RecorderHandler.IsRecording )
			{
				RecorderHandler.OnFinishStaticDraw();
			}
		}

		public void OnStaticDraw( int id )
		{
			if( RecorderHandler.IsRecording )
			{
				RecorderHandler.OnStaticDraw( id );
			}
		}

		public void OnStartDrawingObject( object obj )
		{
			if( RecorderHandler.IsRecording )
			{
				RecorderHandler.OnStartDrawingObject( obj );
			}
		}

		public void OnFinishDrawingObject( object obj )
		{
			if( RecorderHandler.IsRecording )
			{
				RecorderHandler.OnFinishDrawingObject( obj );
			}
		}
	}

	#region HOOKS

	//save the video and stop recording
	[HarmonyPatch( typeof( Level ) , "set_current" )]
	internal static class Level_SetCurrent
	{
		//changed the Postfix to a Prefix so we can get the Level.current before it's changed to the new one
		//as we use it to check if the nextlevel is going to be a GameLevel if this one is a RockScoreboard, then we try collecting matchdata again
		private static void Prefix( Level value )
		{

			if( Level.current is null && value != null )
			{
				//at the game startup, make screenshots of whatever map we need a preview of
				//MatchRecorderMod.Recorder?.TryTakingScreenshots();
			}


			//regardless if the current level can be recorded or not, we're done with the current recording so just save and stop
			if( MatchRecorderMod.Recorder.IsRecording )
			{
				MatchRecorderMod.Recorder?.StopRecording();
			}

			//only really useful in multiplayer, since continuing a match from the endgame screen doesn't trigger ResetMatchStuff on other clients
			//so unfortunately we have to do this hack
			if( Network.isActive && Network.isClient && !Level.core.gameInProgress )
			{
				Level oldValue = Level.current;
				Level newValue = value;
				if( oldValue is RockScoreboard && MatchRecorderMod.Recorder.IsLevelRecordable( newValue ) )
				{
					MatchRecorderMod.Recorder?.TryCollectingMatchData();
				}
			}
		}
	}

	//this is called once when a new match starts, but not if you're a client and in multiplayer and the host decides to continue from the endgame screen instead of going
	//back to lobby first
	[HarmonyPatch( typeof( Main ) , "ResetMatchStuff" )]
	internal static class Main_ResetMatchStuff
	{
		private static void Prefix()
		{
			MatchRecorderMod.Recorder?.TryCollectingMatchData();
		}
	}

	[HarmonyPatch( typeof( Level ) , nameof( Level.UpdateCurrentLevel ) )]
	internal static class UpdateLoop
	{
		private static void Prefix()
		{
			MatchRecorderMod.Recorder?.Update();
		}
	}

	//start recording
	[HarmonyPatch( typeof( VirtualTransition ) , nameof( VirtualTransition.GoUnVirtual ) )]
	internal static class VirtualTransition_GoUnVirtual
	{
		private static void Postfix()
		{
			//only bother if the current level is something we care about
			if( MatchRecorderMod.Recorder.IsLevelRecordable( Level.current ) )
			{
				MatchRecorderMod.Recorder?.StartRecording();
				MatchRecorderMod.Recorder.GatherLevelData( Level.current );
			}
		}
	}

#if REPLAYRECORDER
	[HarmonyPatch( typeof( Level ) , nameof( Level.DrawCurrentLevel ) )]
	internal static class OnNewRenderingFrame
	{
		private static void Prefix() => MatchRecorderMod.Recorder?.StartFrame();

		private static void Postfix() => MatchRecorderMod.Recorder?.EndFrame();
	}

	[HarmonyPatch()]
	internal static class OnRender
	{
		static MethodBase TargetMethod()
		{
			return Array.Find( typeof( Graphics ).GetMethods() , x =>
			{
				if( x.Name == nameof( Graphics.Draw ) )
				{
					ParameterInfo [] parameters = x.GetParameters();
					if( parameters.Length > 2 && parameters [0].ParameterType == typeof( Tex2D ) && parameters [1].ParameterType == typeof( DuckGame.Vec2 ) )
					{
						return true;
					}
				}

				return false;
			} );
		}

		private static void Prefix( Tex2D texture , DuckGame.Vec2 position , DuckGame.Rectangle? sourceRectangle , DuckGame.Color color , float rotation , DuckGame.Vec2 origin , DuckGame.Vec2 scale , int effects , Depth depth = default( Depth ) )
		{
			MatchRecorderMod.Recorder?.OnTextureDraw( texture , position , sourceRectangle , color , rotation , origin , scale , (int) effects , depth );
		}
	}

	[HarmonyPatch( typeof( SpriteMap ) , nameof( SpriteMap.UltraCheapStaticDraw ) )]
	internal static class OnStaticDraw
	{
		private static bool _drawing = false;
		private static FieldInfo _batchItemField = typeof( SpriteMap ).GetField( "_batchItem" , BindingFlags.NonPublic | BindingFlags.Instance );
		private static PropertyInfo _validProperty = typeof( SpriteMap ).GetProperty( "valid" , BindingFlags.NonPublic | BindingFlags.Instance );

		private static Dictionary<SpriteMap , int> _currentBatches = new Dictionary<SpriteMap , int>();

		private static void Prefix( SpriteMap __instance )
		{
			var batchItem = (MTSpriteBatchItem) _batchItemField.GetValue( __instance );
			var valid = (bool) _validProperty.GetValue( __instance );

			if( batchItem != null )
			{
				MatchRecorderMod.Recorder?.OnStaticDraw( _currentBatches [__instance] );
				return;
			}

			if( !valid || MatchRecorderMod.Recorder == null )
			{
				return;
			}

			int id = MatchRecorderMod.Recorder.OnStartStaticDraw();
			_currentBatches [__instance] = id;
			_drawing = true;
		}

		private static void Postfix( SpriteMap __instance )
		{
			if( !_drawing )
			{
				return;
			}

			_drawing = false;
			MatchRecorderMod.Recorder?.OnFinishStaticDraw();
		}
	}

	[HarmonyPatch( typeof( Thing ) , nameof( Thing.DoDraw ) )]
	internal static class OnDoDraw
	{
		private static void Prefix( Thing __instance )
		{
			MatchRecorderMod.Recorder?.OnStartDrawingObject( __instance );
		}

		private static void Postfix( Thing __instance )
		{
			MatchRecorderMod.Recorder?.OnFinishDrawingObject( __instance );
		}
	}
#endif

	#endregion HOOKS
}