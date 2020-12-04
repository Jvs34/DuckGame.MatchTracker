﻿using DuckGame;
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
		public bool IsRecordingRound => RecorderHandler.IsRecording;
		public bool IsRecordingMatch { get; set; }
		public string ModPath { get; }
		private IConfigurationRoot Configuration { get; }

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

		public MatchData StartCollectingMatchData()
		{
			CurrentMatch = new MatchData
			{
				TimeStarted = DateTime.Now ,
				Name = GameDatabase.SharedSettings.DateTimeToString( DateTime.Now ) ,
			};

			return CurrentMatch;
		}

		public MatchData StopCollectingMatchData()
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

		public void Update() => RecorderHandler?.Update();

		#region UTILITY

		public void ShowHUDmessage( string message , float lifetime = 1f )
		{
			var cornerMessage = HUD.AddCornerMessage( HUDCorner.TopLeft , message );
			cornerMessage.slide = 1;
			cornerMessage.willDie = true;
			cornerMessage.life = lifetime;
		}

		public bool IsLevelRecordable( Level level ) => level is GameLevel;

		public void AddTeamAndPlayerData( IWinner winnerObject )
		{
			foreach( Team team in Teams.active )
			{
				winnerObject.Teams.Add( CreateTeamDataFromTeam( team , winnerObject ) );
			}

			foreach( Profile pro in Profiles.activeNonSpectators )
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

		private static MatchTracker.LevelData CreateLevelDataFromLevel( string levelId )
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
			string onlineID = profile.steamID.ToString();

			string userId = Network.isActive ? onlineID : profile.id;

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
		#endregion UTILITY

	}
	#region HOOKS
#pragma warning disable IDE0051 // Remove unused private members
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

			//regardless if the current level can be recorded or not, we're done with the current round recording so just save and stop
			if( MatchRecorderMod.Recorder?.IsRecordingRound == true )
			{
				MatchRecorderMod.Recorder?.StopRecordingRound();
			}

			//seems like we launched a match just now, start recording
			if( MatchRecorderMod.Recorder?.IsLevelRecordable( value ) == true && MatchRecorderMod.Recorder?.IsRecordingMatch == false )
			{
				MatchRecorderMod.Recorder.IsRecordingMatch = true;
				MatchRecorderMod.Recorder.StartRecordingMatch();
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
			if( MatchRecorderMod.Recorder?.IsRecordingMatch == true )
			{
				MatchRecorderMod.Recorder.IsRecordingMatch = false;
				MatchRecorderMod.Recorder.StopRecordingMatch();
			}
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

	//start recording a round
	[HarmonyPatch( typeof( VirtualTransition ) , nameof( VirtualTransition.GoUnVirtual ) )]
	internal static class VirtualTransition_GoUnVirtual
	{
		private static void Postfix()
		{
			//only bother if the current level is something we care about
			if( MatchRecorderMod.Recorder.IsLevelRecordable( Level.current ) )
			{
				MatchRecorderMod.Recorder?.StartRecordingRound();
				MatchRecorderMod.Recorder.GatherLevelData( Level.current );
			}
		}
	}

#pragma warning restore IDE0051 // Remove unused private members
	#endregion HOOKS
}