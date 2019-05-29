using DuckGame;
using Harmony;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MatchRecorder
{
	public class MatchRecorderHandler
	{
		private IRecorder RecorderHandler { get; }
		public BotSettings BotSettings { get; }
		public MatchData CurrentMatch { get; private set; }
		public RoundData CurrentRound { get; private set; }
		public IGameDatabase GameDatabase { get; }
		public bool IsRecording => RecorderHandler.IsRecording;
		public string MatchesFolder { get; }
		public string ModPath { get; }
		public string RoundsFolder { get; }
		private IConfigurationRoot Configuration { get; }

		public MatchRecorderHandler( string modPath )
		{
			ModPath = modPath;
			GameDatabase = new FileSystemGameDatabase();
			BotSettings = new BotSettings();

			Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( modPath , "Settings" ) )
#if DEBUG
				.AddJsonFile( "shared_debug.json" )
#else
				.AddJsonFile( "shared.json" )
#endif
				.AddJsonFile( "bot.json" )
			.Build();

			Configuration.Bind( GameDatabase.SharedSettings );
			Configuration.Bind( BotSettings );

			RoundsFolder = Path.Combine( GameDatabase.SharedSettings.GetRecordingFolder() , GameDatabase.SharedSettings.RoundsFolder );
			MatchesFolder = Path.Combine( GameDatabase.SharedSettings.GetRecordingFolder() , GameDatabase.SharedSettings.MatchesFolder );

			if( !Directory.Exists( RoundsFolder ) )
				Directory.CreateDirectory( RoundsFolder );

			if( !Directory.Exists( MatchesFolder ) )
				Directory.CreateDirectory( MatchesFolder );

			if( !File.Exists( GameDatabase.SharedSettings.GetGlobalPath() ) )
			{
				GameDatabase.SaveGlobalData( new MatchTracker.GlobalData() ).Wait();
			}

#if DEBUG
			RecorderHandler = new ReplayRecorder( this );
#else
			RecorderHandler = new ObsRecorder( this );
#endif


		}

		//only record game levels for now since we're kind of tied to the gounvirtual stuff
		public bool IsLevelRecordable( Level level )
		{
			return level is GameLevel;
		}

		public void FixDuckPersonaPaths()
		{
			FieldInfo textureNameBinding = typeof( Tex2D ).GetField( "_textureName" , BindingFlags.NonPublic | BindingFlags.Instance );

			foreach( DuckPersona persona in Persona.all )
			{
				//persona.skipSprite.color = new DuckGame.Color( persona.color );
				textureNameBinding.SetValue( persona.skipSprite.texture , "skipSign" );
				textureNameBinding.SetValue( persona.arrowSprite.texture , "startArrow" );
				textureNameBinding.SetValue( persona.fingerPositionSprite.texture , "fingerPositions" );
				textureNameBinding.SetValue( persona.featherSprite.texture , "feather" );
				textureNameBinding.SetValue( persona.crowdSprite.texture , "seatDuck" );
				textureNameBinding.SetValue( persona.sprite.texture , "duck" );
				textureNameBinding.SetValue( persona.armSprite.texture , "duckArms" );
				textureNameBinding.SetValue( persona.quackSprite.texture , "quackduck" );
				textureNameBinding.SetValue( persona.controlledSprite.texture , "controlledDuck" );
				textureNameBinding.SetValue( persona.defaultHead.texture , "hats/default" );
			}

		}

		public RoundData StartCollectingRoundData( DateTime startTime )
		{
			Level lvl = Level.current;

			CurrentRound = new RoundData()
			{
				MatchName = CurrentMatch?.Name ,
				LevelName = lvl.level ,
				Players = new List<PlayerData>() ,
				TimeStarted = startTime ,
				IsCustomLevel = false ,
				RecordingType = RecorderHandler.ResultingRecordingType ,
			};

			CurrentRound.Name = GameDatabase.SharedSettings.DateTimeToString( CurrentRound.TimeStarted );

			if( lvl is GameLevel gl )
			{
				CurrentRound.IsCustomLevel = gl.isCustomLevel;
			}

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
				winnerObject.Players.Add( ply );
			}

			foreach( TeamData teamData in winnerObject.Teams )
			{
				Team team = Teams.active.Find( x => x.name == teamData.HatName );
				if( team != null )
				{
					foreach( Profile pro in team.activeProfiles )
					{
						teamData.Players.Add( CreatePlayerDataFromProfile( pro , winnerObject ) );
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

			GameDatabase.SaveRoundData( GameDatabase.SharedSettings.DateTimeToString( CurrentRound.TimeStarted ) , CurrentRound ).Wait();

			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;
			globalData.Rounds.Add( CurrentRound.Name );
			GameDatabase.SaveGlobalData( globalData ).Wait();

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
#if DEBUG
			if( Keyboard.Down( Keys.LeftShift ) && Keyboard.Pressed( Keys.D0 ) )
			{
				var levels = Content.GetLevels( "deathmatch" , LevelLocation.Content );
				TryTakingScreenshots();
			}
#endif


			RecorderHandler?.Update();
		}

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

		public void GatherLevelData()
		{
			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;

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

			GameDatabase.SaveGlobalData( globalData );
		}

		private MatchTracker.LevelData CreateLevelDataFromLevel( string levelId )
		{
			DuckGame.LevelData dgLevelData = Content.GetLevel( levelId );
			if( dgLevelData != null )
			{
				return new MatchTracker.LevelData()
				{
					LevelName = levelId ,
					IsOnlineMap = dgLevelData.metaData.online ,
					FilePath = dgLevelData.GetPath() ,
					IsCustomMap = dgLevelData.GetLocation() != LevelLocation.Content ,
					Author = dgLevelData.workshopData?.author ,
					Description = dgLevelData.workshopData?.description
				};
			}

			return null;
		}

		private PlayerData CreatePlayerDataFromProfile( Profile profile , IWinner winnerObject )
		{
			string userId = Network.isActive ? profile.steamID.ToString() : profile.id;

			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;

			PlayerData pd = globalData.Players.Find( x => x.UserId == userId );

			if( pd == null )
			{
				pd = new PlayerData
				{
					UserId = userId ,
					Name = profile.name ,
				};

				//search for this profile on the globaldata, if it's there fill in the rest of the info
				foreach( var ply in globalData.Players )
				{
					if( pd.Equals( ply ) )
					{
						pd.DiscordId = ply.DiscordId;
						pd.NickName = ply.NickName;
						break;
					}
				}
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
			};

			CurrentMatch.Name = GameDatabase.SharedSettings.DateTimeToString( CurrentMatch.TimeStarted );

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

			GameDatabase.SaveMatchData( GameDatabase.SharedSettings.DateTimeToString( CurrentMatch.TimeStarted ) , CurrentMatch ).Wait();

			//also add this match to the globaldata as well
			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;
			globalData.Matches.Add( CurrentMatch.Name );

			//try adding the players from the matchdata into the globaldata

			foreach( PlayerData ply in CurrentMatch.Players )
			{
				if( !globalData.Players.Any( p => p.UserId == ply.UserId ) )
				{
					globalData.Players.Add( ply );
				}
			}

			GameDatabase.SaveGlobalData( globalData ).Wait();

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
				// MatchRecorderMod.Recorder?.FixDuckPersonaPaths();
				MatchRecorderMod.Recorder?.GatherLevelData();
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
			}
		}
	}

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
		static private bool _drawing = false;
		static private FieldInfo _batchItemField = typeof( SpriteMap ).GetField( "_batchItem" , BindingFlags.NonPublic | BindingFlags.Instance );
		static private PropertyInfo _validProperty = typeof( SpriteMap ).GetProperty( "valid" , BindingFlags.NonPublic | BindingFlags.Instance );

		static private Dictionary<SpriteMap , int> _currentBatches = new Dictionary<SpriteMap , int>();

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
				return;

			int id = MatchRecorderMod.Recorder.OnStartStaticDraw();
			_currentBatches [__instance] = id;
			_drawing = true;
		}

		private static void Postfix( SpriteMap __instance )
		{
			if( !_drawing )
				return;

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

	#endregion HOOKS
}