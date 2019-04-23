﻿using DSharpPlus;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;

/*
Goes through all the folders, puts all rounds and matches into data.json
Also returns match/round data from the timestamped name and whatnot
*/

namespace MatchUploader
{
	public sealed class MatchUploaderHandler
	{
		private readonly BotSettings botSettings;
		private readonly Branch currentBranch;
		private readonly Repository databaseRepository;
		private readonly DiscordClient discordClient;
		private readonly IGameDatabase gameDatabase;
		private readonly string settingsFolder;
		private readonly UploaderSettings uploaderSettings;
		private PendingUpload currentVideo;
		private YouTubeService youtubeService;
		private CalendarService calendarService;

		private IConfigurationRoot Configuration { get; }
		private JsonSerializerSettings JsonSettings { get; }

		public MatchUploaderHandler( string [] args )
		{
			gameDatabase = new GameDatabase();
			gameDatabase.LoadGlobalDataDelegate += LoadDatabaseGlobalDataFile;
			gameDatabase.LoadMatchDataDelegate += LoadDatabaseMatchDataFile;
			gameDatabase.LoadRoundDataDelegate += LoadDatabaseRoundDataFile;
			gameDatabase.SaveGlobalDataDelegate += SaveDatabaseGlobalDataFile;
			gameDatabase.SaveMatchDataDelegate += SaveDatabaseMatchDataFile;
			gameDatabase.SaveRoundDataDelegate += SaveDatabaseRoundataFile;

			JsonSettings = new JsonSerializerSettings()
			{
				PreserveReferencesHandling = PreserveReferencesHandling.Objects ,
			};

			uploaderSettings = new UploaderSettings();
			botSettings = new BotSettings();

			settingsFolder = Path.Combine( Directory.GetCurrentDirectory() , "Settings" );
			Configuration = new ConfigurationBuilder()
				.SetBasePath( settingsFolder )
				.AddJsonFile( "shared.json" )
				.AddJsonFile( "uploader.json" )
				.AddJsonFile( "bot.json" )
				.AddCommandLine( args )
			.Build();

			Configuration.Bind( gameDatabase.SharedSettings );
			Configuration.Bind( uploaderSettings );
			Configuration.Bind( botSettings );

			if( Repository.IsValid( gameDatabase.SharedSettings.GetRecordingFolder() ) )
			{
				Console.WriteLine( "Loaded {0}" , gameDatabase.SharedSettings.GetRecordingFolder() );
				databaseRepository = new Repository( gameDatabase.SharedSettings.GetRecordingFolder() );
				currentBranch = databaseRepository.Branches.First( branch => branch.IsCurrentRepositoryHead );
			}

			if( !string.IsNullOrEmpty( botSettings.discordToken ) )
			{
				discordClient = new DiscordClient( new DiscordConfiguration()
				{
					AutoReconnect = true ,
					TokenType = TokenType.Bot ,
					Token = botSettings.discordToken ,
				} );
			}
		}

		public async Task AddRoundToPlaylist( RoundData roundData , MatchData matchData , List<PlaylistItem> playlistItems )
		{
			if( matchData.YoutubeUrl == null || roundData.YoutubeUrl == null )
			{
				Console.WriteLine( $"Could not add round {roundData.Name} to playlist because either match url or round url are missing" );
				await Task.CompletedTask;
				return;
			}

			int roundIndex = matchData.Rounds.IndexOf( roundData.Name );

			try
			{
				if( !playlistItems.Any( x => x.Snippet.ResourceId.VideoId == roundData.YoutubeUrl ) )
				{
					Console.WriteLine( $"Could not find {roundData.Name} on playlist {matchData.Name}, adding" );
					PlaylistItem roundPlaylistItem = await GetPlaylistItemForRound( roundData );
					roundPlaylistItem.Snippet.Position = roundIndex + 1;
					roundPlaylistItem.Snippet.PlaylistId = matchData.YoutubeUrl;
					await youtubeService.PlaylistItems.Insert( roundPlaylistItem , "snippet" ).ExecuteAsync();
				}
			}
			catch( Google.GoogleApiException e )
			{
				Console.WriteLine( e.Message );
			}
		}

		public async Task CleanPlaylists()
		{
			var allplaylists = await GetAllPlaylists();
			foreach( var playlist in allplaylists )
			{
				List<Task> playlistTasks = new List<Task>();

				var allvideos = await GetAllPlaylistItems( playlist.Id );
				foreach( var item in allvideos )
				{
					//playlistTasks.Add( youtubeService.PlaylistItems.Delete( item.Id ).ExecuteAsync() );

					try
					{
						await youtubeService.PlaylistItems.Delete( item.Id ).ExecuteAsync();
					}
					catch( Exception )
					{
						Console.WriteLine( $"Could not delete {item.Snippet.Title} from {playlist.Snippet.Title}" );
					}
				}

				if( playlistTasks.Count > 0 )
				{
					try
					{
						await Task.WhenAll( playlistTasks );
					}
					catch( Exception ex )
					{
						Console.WriteLine( ex );
					}
				}
			}
		}

		public async Task CleanupVideos()
		{
			GlobalData globalData = await gameDatabase.GetGlobalData();
			foreach( string roundName in globalData.Rounds )
			{
				RoundData roundData = await gameDatabase.GetRoundData( roundName );
				if( roundData.YoutubeUrl != null )
				{
					await RemoveVideoFile( roundName );
				}
			}
		}

		public void CommitGitChanges()
		{
			if( databaseRepository == null )
				return;

			Signature us = new Signature( Assembly.GetEntryAssembly().GetName().Name , uploaderSettings.gitEmail , DateTime.Now );
			var credentialsHandler = new CredentialsHandler(
				( url , usernameFromUrl , supportedCredentialTypes ) =>
					new UsernamePasswordCredentials()
					{
						Username = uploaderSettings.gitUsername ,
						Password = uploaderSettings.gitPassword ,
					}
			);

			bool hasChanges = false;

			Console.WriteLine( "Fetching repository status" );

			var mergeResult = Commands.Pull( databaseRepository , us , new PullOptions()
			{
				FetchOptions = new FetchOptions()
				{
					CredentialsProvider = credentialsHandler ,
				} ,
				MergeOptions = new MergeOptions()
				{
					CommitOnSuccess = true ,
				}
			} );

			if( mergeResult.Status == MergeStatus.Conflicts )
			{
				throw new Exception( "Could not complete a successful merge. " );
			}

			foreach( var item in databaseRepository.RetrieveStatus() )
			{
				if( item.State != FileStatus.Ignored && item.State != FileStatus.Unaltered )
				{
					Console.WriteLine( "File {0} {1}" , item.FilePath , item.State );

					Commands.Stage( databaseRepository , item.FilePath );
					hasChanges = true;
				}
			}

			if( hasChanges )
			{
				//Commands.Stage( repository , "*" );

				databaseRepository.Commit( "Updated database" , us , us );

				Console.WriteLine( "Creating commit" );

				//I guess you should always try to push regardless if there has been any changes
				PushOptions pushOptions = new PushOptions
				{
					CredentialsProvider = credentialsHandler ,
				};
				databaseRepository.Network.Push( currentBranch , pushOptions );
				Console.WriteLine( "Commit pushed" );
			}
		}

		public async Task<Playlist> CreatePlaylist( MatchData matchData )
		{
			try
			{
				Playlist pl = await GetPlaylistDataForMatch( matchData );
				var createPlaylistRequest = youtubeService.Playlists.Insert( pl , "snippet,status" );
				Playlist matchPlaylist = await createPlaylistRequest.ExecuteAsync();
				if( matchPlaylist != null )
				{
					matchData.YoutubeUrl = matchPlaylist.Id;
				}

				return matchPlaylist;
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			return null;
		}

		public async Task DoLogin()
		{
			if( discordClient != null )
			{
				await discordClient.ConnectAsync();
				await discordClient.InitializeAsync();
			}

			string appName = Assembly.GetEntryAssembly().GetName().Name;

			//youtube stuff
			youtubeService = new YouTubeService( new BaseClientService.Initializer()
			{
				HttpClientInitializer = await GoogleWebAuthorizationBroker.AuthorizeAsync( uploaderSettings.secrets ,
					new [] { YouTubeService.Scope.Youtube } ,
					"youtube" ,
					CancellationToken.None ,
					uploaderSettings.dataStore
				) ,
				ApplicationName = appName ,
				GZipEnabled = true ,
			} );
			youtubeService.HttpClient.Timeout = TimeSpan.FromMinutes( 2 );

			//calendar stuff

			calendarService = new CalendarService( new BaseClientService.Initializer()
			{
				HttpClientInitializer = await GoogleWebAuthorizationBroker.AuthorizeAsync( uploaderSettings.secrets ,
					new [] { CalendarService.Scope.Calendar } ,
					"calendar" ,
					CancellationToken.None ,
					uploaderSettings.dataStore
				) ,
				ApplicationName = appName ,
				GZipEnabled = true ,
			} );
		}

		public async Task<List<Event>> GetAllCalendarEvents()
		{
			var allEvents = new List<Event>();


			var eventRequest = calendarService.Events.List( uploaderSettings.calendarID );
			Events eventResponse;

			do
			{
				eventResponse = await eventRequest.ExecuteAsync();

				foreach( var eventItem in eventResponse.Items )
				{
					if( !allEvents.Contains( eventItem ) )
					{
						allEvents.Add( eventItem );
					}
				}

				eventRequest.PageToken = eventResponse.NextPageToken;
			}
			while( eventResponse.Items.Count > 0 && eventResponse.NextPageToken != null );

			return allEvents;
		}

		public async Task HandleCalendar()
		{
			if( calendarService is null )
			{
				return;
			}

			string calendarID = uploaderSettings.calendarID;

			var allEvents = await GetAllCalendarEvents();

			GlobalData globalData = await gameDatabase.GetGlobalData();

			List<Task<Event>> matchTasks = new List<Task<Event>>();

			foreach( string matchName in globalData.Matches )
			{
				string strippedName = GetStrippedMatchName( await gameDatabase.GetMatchData( matchName ) );

				//if this event is already added, don't even call this

				if( allEvents.Any( x => x.Id.Equals( strippedName ) ) )
				{
					continue;
				}

				matchTasks.Add( GetCalendarEventForMatch( matchName ) );
			}

			await Task.WhenAll( matchTasks );

			//only get the first one and try using it

			List<Task<Event>> eventTasks = new List<Task<Event>>();

			foreach( var matchTask in matchTasks )
			{
				eventTasks.Add( calendarService.Events.Insert( matchTask.Result , calendarID ).ExecuteAsync() );
			}

			//now create one for each one
			await Task.WhenAll( eventTasks );
		}

		private string GetStrippedMatchName( MatchData matchData )
		{
			return matchData.TimeStarted.ToString( "yyyyMMddHHmmss" );
		}

		public async Task<Event> GetCalendarEventForMatch( string matchName )
		{
			MatchData matchData = await gameDatabase.GetMatchData( matchName );

			var youtubeSnippet = await GetPlaylistDataForMatch( matchData );

			return new Event()
			{
				Id = GetStrippedMatchName( matchData ) ,
				Start = new EventDateTime()
				{
					DateTime = matchData.TimeStarted
				} ,
				End = new EventDateTime()
				{
					DateTime = matchData.TimeEnded
				} ,
				Summary = youtubeSnippet.Snippet.Title ,
				Description = youtubeSnippet.Snippet.Description ,
			};
		}

		public async Task FixupTeams()
		{
			GlobalData globalData = await gameDatabase.GetGlobalData();
			foreach( string matchName in globalData.Matches )
			{
				MatchData matchData = await gameDatabase.GetMatchData( matchName );
				DoFixupTeams( matchData );
				await gameDatabase.SaveMatchData( matchName , matchData );
			}

			foreach( string roundName in globalData.Rounds )
			{
				RoundData roundData = await gameDatabase.GetRoundData( roundName );
				DoFixupTeams( roundData );
				await gameDatabase.SaveRoundData( roundName , roundData );
			}

			await gameDatabase.SaveGlobalData( globalData );
		}

		public async Task<List<PlaylistItem>> GetAllPlaylistItems( string playlistId )
		{
			List<PlaylistItem> allplaylistitems = new List<PlaylistItem>();
			var playlistItemsRequest = youtubeService.PlaylistItems.List( "snippet" );
			playlistItemsRequest.PlaylistId = playlistId;
			playlistItemsRequest.MaxResults = 50;

			PlaylistItemListResponse playlistItemListResponse = null;

			do
			{
				playlistItemListResponse = await playlistItemsRequest.ExecuteAsync();
				foreach( var plitem in playlistItemListResponse.Items )
				{
					if( !allplaylistitems.Any( x => x.Id == plitem.Id ) )
					{
						allplaylistitems.Add( plitem );
					}
				}
				playlistItemsRequest.PageToken = playlistItemListResponse.NextPageToken;
			}
			while( playlistItemListResponse.Items.Count > 0 && playlistItemListResponse.NextPageToken != null );

			return allplaylistitems;
		}

		public async Task<List<Playlist>> GetAllPlaylists()
		{
			List<Playlist> allplaylists = new List<Playlist>();
			var playlistsRequest = youtubeService.Playlists.List( "snippet" );
			playlistsRequest.Mine = true;
			playlistsRequest.MaxResults = 50;
			PlaylistListResponse playlistResponse;
			do
			{
				playlistResponse = await playlistsRequest.ExecuteAsync();

				//try to aggregate playlists until the response gives 0 videos
				foreach( var plitem in playlistResponse.Items )
				{
					if( !allplaylists.Contains( plitem ) )
					{
						allplaylists.Add( plitem );
					}
				}
				playlistsRequest.PageToken = playlistResponse.NextPageToken;
			}
			while( playlistResponse.Items.Count > 0 && playlistResponse.NextPageToken != null );
			return allplaylists;
		}

		public async Task<Playlist> GetPlaylistDataForMatch( MatchData matchData )
		{
			await Task.CompletedTask;

			string winner = matchData.GetWinnerName();

			if( string.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			return new Playlist()
			{
				Snippet = new PlaylistSnippet()
				{
					Title = $"{matchData.Name} {winner}" ,
					Description = string.Format( "Recorded on {0}\nThe winner is {1}" , gameDatabase.SharedSettings.DateTimeToString( matchData.TimeStarted ) , winner ) ,
					Tags = new List<string>() { "duckgame" , "peniscorp" }
				} ,
				Status = new PlaylistStatus()
				{
					PrivacyStatus = "public"
				}
			};
		}

		public async Task<PlaylistItem> GetPlaylistItemForRound( RoundData roundData )
		{
			await Task.CompletedTask;
			return new PlaylistItem()
			{
				Snippet = new PlaylistItemSnippet()
				{
					ResourceId = new ResourceId()
					{
						Kind = "youtube#video" ,
						VideoId = roundData.YoutubeUrl ,
					}
				} ,
			};
		}

		public async Task<Video> GetVideoDataForRound( RoundData roundData )
		{
			await Task.CompletedTask;
			string winner = roundData.GetWinnerName();

			if( string.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			string description = string.Format( "Recorded on {0}\nThe winner is {1}" , gameDatabase.SharedSettings.DateTimeToString( roundData.TimeStarted ) , winner );

			Video videoData = new Video()
			{
				Snippet = new VideoSnippet()
				{
					Title = $"{roundData.Name} {winner}" ,
					Tags = new List<string>() { "duckgame" , "peniscorp" } ,
					CategoryId = "20" , // See https://developers.google.com/youtube/v3/docs/videoCategories/list
					Description = description ,
				} ,
				Status = new VideoStatus()
				{
					PrivacyStatus = "unlisted" ,
				} ,
				RecordingDetails = new VideoRecordingDetails()
				{
					RecordingDate = roundData.TimeStarted ,
				}
			};

			return videoData;
		}

		public async Task LoadDatabase()
		{
			await gameDatabase.Load();
			Console.WriteLine( "Finished loading the database" );
		}

		public async Task LookForOrphanedItems()
		{
			Console.WriteLine( "Looking for orphaned items" );
			GlobalData globalData = await gameDatabase.GetGlobalData();
			//go through all the rounds defined in globaldata then check if that match contains that round, otherwise it's orphaned
			foreach( string roundName in globalData.Rounds )
			{
				bool foundParent = false;
				foreach( string matchName in globalData.Matches )
				{
					MatchData matchData = await gameDatabase.GetMatchData( matchName );
					if( matchData.Rounds.Contains( roundName ) )
					{
						foundParent = true;
					}
				}

				if( !foundParent )
				{
					Console.WriteLine( $"{roundName} is an orphaned item!!!" );
				}
			}
		}

		public async Task Run()
		{
			await LoadDatabase();
			await DoLogin();

			try
			{
				await HandleCalendar();
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			await SaveSettings();

			CommitGitChanges();
			await UploadAllRounds();
		}

		//in this context, settings are only the uploaderSettings
		public async Task SaveSettings()
		{
			await File.WriteAllTextAsync(
				Path.Combine( settingsFolder , "uploader.json" ) ,
				JsonConvert.SerializeObject( uploaderSettings , Formatting.Indented )
			);
		}

		//updates the global data.json
		public async Task UpdateGlobalData()
		{
			string roundsPath = Path.Combine( gameDatabase.SharedSettings.GetRecordingFolder() , gameDatabase.SharedSettings.roundsFolder );
			string matchesPath = Path.Combine( gameDatabase.SharedSettings.GetRecordingFolder() , gameDatabase.SharedSettings.matchesFolder );

			if( !Directory.Exists( gameDatabase.SharedSettings.GetRecordingFolder() ) || !Directory.Exists( roundsPath ) || !Directory.Exists( matchesPath ) )
			{
				throw new DirectoryNotFoundException( "Folders do not exist" );
			}

			string globalDataPath = gameDatabase.SharedSettings.GetGlobalPath();

			GlobalData globalData = new GlobalData();

			if( File.Exists( globalDataPath ) )
			{
				globalData = await gameDatabase.GetGlobalData();
			}

			var roundFolders = Directory.EnumerateDirectories( roundsPath );

			foreach( var folderPath in roundFolders )
			{
				//if it doesn't contain the folder, check if the round is valid
				string folderName = Path.GetFileName( folderPath );

				if( !globalData.Rounds.Contains( folderName ) )
				{
					globalData.Rounds.Add( folderName );
				}

				RoundData roundData = await gameDatabase.GetRoundData( folderName );
				if( string.IsNullOrEmpty( roundData.Name ) )
				{
					roundData.Name = gameDatabase.SharedSettings.DateTimeToString( roundData.TimeStarted );
					Console.WriteLine( $"Adding roundName to roundData {roundData.Name}" );

					await gameDatabase.SaveRoundData( roundData.Name , roundData );
				}

				if( roundData.RecordingType == RecordingType.None )
				{
					if( roundData.YoutubeUrl != null || File.Exists( gameDatabase.SharedSettings.GetRoundVideoPath( roundData.Name ) ) )
					{
						roundData.RecordingType = RecordingType.Video;
						await gameDatabase.SaveRoundData( roundData.Name , roundData );
					}
				}

				foreach( var roundPlayer in roundData.Players )
				{
					//if this player is in the globaldata, we need to fill in the missing info

					foreach( var globalPly in globalData.Players )
					{
						if( roundPlayer.Equals( globalPly ) )
						{
							roundPlayer.DiscordId = globalPly.DiscordId;
							roundPlayer.NickName = globalPly.NickName;
						}
					}
				}

				await gameDatabase.SaveRoundData( roundData.Name , roundData );
			}

			var matchFiles = Directory.EnumerateFiles( matchesPath );

			foreach( var matchPath in matchFiles )
			{
				string matchName = Path.GetFileNameWithoutExtension( matchPath );

				if( !globalData.Matches.Contains( matchName ) )
				{
					globalData.Matches.Add( matchName );
				}

				MatchData matchData = await gameDatabase.GetMatchData( matchName );

				//while we're here, let's check if all the players are added to the global data too
				foreach( PlayerData ply in matchData.Players )
				{
					if( !globalData.Players.Any( p => p.UserId == ply.UserId ) )
					{
						PlayerData toAdd = ply;
						ply.Team = null;

						globalData.Players.Add( toAdd );
					}
				}

				foreach( var matchPlayer in matchData.Players )
				{
					//if this player is in the globaldata, we need to fill in the missing info

					foreach( var globalPly in globalData.Players )
					{
						if( matchPlayer.Equals( globalPly ) )
						{
							matchPlayer.DiscordId = globalPly.DiscordId;
							matchPlayer.NickName = globalPly.NickName;
						}
					}
				}

				foreach( var roundName in matchData.Rounds )
				{
					RoundData roundData = await gameDatabase.GetRoundData( roundName );
					if( string.IsNullOrEmpty( roundData.MatchName ) )
					{
						roundData.MatchName = matchData.Name;
						await gameDatabase.SaveRoundData( roundData.Name , roundData );
					}
				}

				if( string.IsNullOrEmpty( matchData.Name ) )
				{
					matchData.Name = gameDatabase.SharedSettings.DateTimeToString( matchData.TimeStarted );
					Console.WriteLine( $"Adding matchName to matchData {matchData.Name}" );
				}

				await gameDatabase.SaveMatchData( matchData.Name , matchData );
			}

			await gameDatabase.SaveGlobalData( globalData );
		}

		public async Task UpdatePlaylists()
		{
			//TODO:I'm gonna regret writing this piece of shit tomorrow

			//go through every match, then try to find the playlist on youtube that contains its name, if it doesn't exist, create it
			//get all playlists first

			try
			{
				//queue all the addrounds tasks together
				List<Task> addrounds = new List<Task>();

				Console.WriteLine( "Searching all playlists..." );
				var allplaylists = await GetAllPlaylists();

				GlobalData globalData = await gameDatabase.GetGlobalData();

				foreach( var matchName in globalData.Matches )
				{
					MatchData matchData = await gameDatabase.GetMatchData( matchName );

					//this returns the youtubeurl if it's not null otherwise it tries to search for it in all of the playlists title
					string matchPlaylist = string.IsNullOrEmpty( matchData.YoutubeUrl )
						? allplaylists.FirstOrDefault( x => x.Snippet.Title.Contains( matchName ) )?.Id
						: matchData.YoutubeUrl;

					try
					{
						if( string.IsNullOrEmpty( matchPlaylist ) )
						{
							Console.WriteLine( "Did not find playlist for {0}, creating" , matchName );
							//create the playlist now, doesn't matter that it's empty
							Playlist pl = await CreatePlaylist( matchData );
							if( pl != null )
							{
								await gameDatabase.SaveMatchData( matchName , matchData );
							}
						}
						else
						{
							//add this playlist id to the youtubeurl of the matchdata
							if( string.IsNullOrEmpty( matchData.YoutubeUrl ) )
							{
								matchData.YoutubeUrl = matchPlaylist;
								await gameDatabase.SaveMatchData( matchName , matchData );
							}
						}
					}
					catch( Exception ex )
					{
						Console.WriteLine( "Could not create playlist for {0}" , matchName , ex );
					}

					if( !string.IsNullOrEmpty( matchData.YoutubeUrl ) )
					{
						List<PlaylistItem> playlistItems = await GetAllPlaylistItems( matchData.YoutubeUrl );

						foreach( string roundName in matchData.Rounds )
						{
							RoundData roundData = await gameDatabase.GetRoundData( roundName );
							if( roundData.YoutubeUrl != null )
							{
								await AddRoundToPlaylist( roundData , matchData , playlistItems );
								//addrounds.Add( AddRoundToPlaylist( roundData , matchData , playlistItems ) );
							}
						}
					}
				}

				//finally await it all at once
				if( addrounds.Count > 0 )
				{
					await Task.WhenAll( addrounds );
				}
			}
			catch( Exception ex )
			{
				Console.WriteLine( ex.Message );
			}

			CommitGitChanges();
		}

		//go through every match that has a playlist id, then update the name of it to reflect the new one
		public async Task UpdatePlaylistsNames()
		{
			try
			{
				GlobalData globalData = await gameDatabase.GetGlobalData();

				foreach( string matchName in globalData.Matches )
				{
					List<Task> matchTasks = new List<Task>();

					MatchData matchData = await gameDatabase.GetMatchData( matchName );

					if( matchData.YoutubeUrl != null )
					{
						var playlistData = await GetPlaylistDataForMatch( matchData );
						playlistData.Id = matchData.YoutubeUrl;
						matchTasks.Add( youtubeService.Playlists.Update( playlistData , "snippet,status" ).ExecuteAsync() );
					}

					foreach( string roundName in matchData.Rounds )
					{
						//do the same for videos
						RoundData roundData = await gameDatabase.GetRoundData( roundName );
						if( roundData.YoutubeUrl != null )
						{
							Video videoData = await GetVideoDataForRound( roundData );
							videoData.Id = roundData.YoutubeUrl;
							matchTasks.Add( youtubeService.Videos.Update( videoData , "snippet,status,recordingDetails" ).ExecuteAsync() );
						}
					}

					await Task.WhenAll( matchTasks );
				}
			}
			catch( Exception ex )
			{
				Console.WriteLine( ex );
			}
		}

		private async Task<IEnumerable<RoundData>> GetUploadableRounds()
		{
			ConcurrentBag<RoundData> uploadableRounds = new ConcurrentBag<RoundData>();

			await gameDatabase.IterateOverAllRoundsOrMatches( false , async ( round ) =>
			{
				if( uploadableRounds.Count >= 100 )
				{
					return;
				}

				RoundData roundData = (RoundData) round;

				if( roundData.RecordingType == RecordingType.Video && string.IsNullOrEmpty( roundData.YoutubeUrl ) )
				{
					uploadableRounds.Add( roundData );
				}

				await Task.CompletedTask;
			} );

			return uploadableRounds.OrderBy( roundData => roundData.TimeStarted );
		}

		public async Task UploadAllRounds()
		{
			Console.WriteLine( "Starting youtube uploads" );

			var roundsToUpload = await GetUploadableRounds();

			int remaining = roundsToUpload.Count();

			foreach( RoundData roundData in roundsToUpload )
			{
				await UpdateUploadProgress( remaining );

				MatchData matchData = ( !string.IsNullOrEmpty( roundData.MatchName ) ) ? await gameDatabase.GetMatchData( roundData.MatchName ) : null;
				List<PlaylistItem> playlistItems = null;

				if( matchData != null && string.IsNullOrEmpty( matchData.YoutubeUrl ) )
				{
					Playlist playlist = await CreatePlaylist( matchData );
					if( playlist != null )
					{
						await gameDatabase.SaveMatchData( roundData.MatchName , matchData );
					}
				}

				if( matchData != null && matchData.YoutubeUrl != null )
				{
					try
					{
						playlistItems = await GetAllPlaylistItems( matchData.YoutubeUrl );
					}
					catch( Exception )
					{
						playlistItems = null;
					}
				}

				bool isUploaded = await UploadRoundToYoutubeAsync( roundData );

				if( isUploaded )
				{
					await RemoveVideoFile( roundData.Name );
					remaining--;

					if( matchData != null && playlistItems != null )
					{
						await AddRoundToPlaylist( roundData , matchData , playlistItems );
					}
				}
			}


			CommitGitChanges();
		}


		public async Task<bool> UploadRoundToYoutubeAsync( RoundData roundData )
		{
			if( youtubeService == null )
			{
				throw new NullReferenceException( "Youtube service is not initialized!!!" );
			}

			if( roundData.YoutubeUrl != null )
			{
				return false;
			}

			Video videoData = await GetVideoDataForRound( roundData );
			string filePath = gameDatabase.SharedSettings.GetRoundVideoPath( roundData.Name );

			if( !File.Exists( filePath ) )
			{
				throw new ArgumentNullException( $"{roundData.Name} does not contain a video!" );
			}

			string reEncodedVideoPath = Path.ChangeExtension( filePath , "converted.mp4" );

			if( File.Exists( reEncodedVideoPath ) )
			{
				filePath = reEncodedVideoPath;
			}

			using( var fileStream = new FileStream( filePath , FileMode.Open ) )
			{
				//get the pending upload for this roundName
				currentVideo = uploaderSettings.pendingUploads.Find( x => x.videoName.Equals( roundData.Name ) );

				if( currentVideo == null )
				{
					currentVideo = new PendingUpload()
					{
						videoName = roundData.Name
					};

					uploaderSettings.pendingUploads.Add( currentVideo );
				}

				currentVideo.fileSize = fileStream.Length;

				if( currentVideo.errorCount > uploaderSettings.retryCount )
				{
					currentVideo.uploadUrl = null;
					Console.WriteLine( "Replacing resumable upload url for {0} after too many errors" , currentVideo.videoName );
					currentVideo.errorCount = 0;
					currentVideo.lastException = string.Empty;
				}

				//TODO:Maybe it's possible to create a throttable request by extending the class of this one and initializing it with this one's values
				var videosInsertRequest = youtubeService.Videos.Insert( videoData , "snippet,status,recordingDetails" , fileStream , "video/*" );
				videosInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;
				videosInsertRequest.ProgressChanged += OnUploadProgress;
				videosInsertRequest.ResponseReceived += OnResponseReceived;
				videosInsertRequest.UploadSessionData += OnStartUploading;

				IUploadProgress uploadProgress;

				if( currentVideo.uploadUrl != null )
				{
					Console.WriteLine( "Resuming upload {0}" , currentVideo.videoName );
					uploadProgress = await videosInsertRequest.ResumeAsync( currentVideo.uploadUrl );
				}
				else
				{
					Console.WriteLine( "Beginning to upload {0}" , currentVideo.videoName );
					uploadProgress = await videosInsertRequest.UploadAsync();
				}

				//save it to the uploader settings and increment the error count only if it's not the annoying too many videos error
				if( uploadProgress.Status != UploadStatus.Completed && currentVideo.uploadUrl != null )
				{
					currentVideo.lastException = uploadProgress.Exception.Message;
					currentVideo.errorCount++;
					await SaveSettings();
				}
				currentVideo = null;
				return uploadProgress.Status == UploadStatus.Completed;
			}
		}

		private async Task SearchMap( string mapGuid )
		{
			GlobalData globalData = await gameDatabase.GetGlobalData();
			await gameDatabase.IterateOverAllRoundsOrMatches( false , async ( round ) =>
			{
				RoundData roundData = (RoundData) round;

				if( !string.IsNullOrEmpty( roundData.YoutubeUrl ) && roundData.LevelName == mapGuid )
				{
					string youtubeUrl = $"https://www.youtube.com/watch?v={roundData.YoutubeUrl}";
					Process.Start( "cmd" , $"/c start {youtubeUrl}" );
				}


				await Task.CompletedTask;
			} );
		}

		private async Task AddYoutubeIdToRound( string roundName , string videoId )
		{
			RoundData roundData = await gameDatabase.GetRoundData( roundName );
			roundData.YoutubeUrl = videoId;
			await gameDatabase.SaveRoundData( roundName , roundData );
		}

		private void DoFixupTeams( IWinner winnerObject )
		{
			//first things first, get the teamdata entity of each player and add it to winnerObject.teams if it's not on there
			foreach( PlayerData playerData in winnerObject.Players )
			{
				TeamData teamData = playerData.Team;

				//if the player of that team is not in that team list then add it to it first

				if( !teamData.Players.Any( x => x.Equals( playerData ) ) )
				{
					teamData.Players.Add( playerData );
				}


				//if the team is not in the teamlist then add it too
				if( !winnerObject.Teams.Any( x => x.Equals( teamData ) ) )
				{
					winnerObject.Teams.Add( teamData );
				}
			}

			//if the team object is the same as the one in the teamlist but with the wrong reference, replace it

			if( winnerObject.Winner != null )
			{
				TeamData foundList = winnerObject.Teams.Find( x => x.Equals( winnerObject.Winner ) );
				if( foundList != null && !ReferenceEquals( winnerObject.Winner , foundList ) )
				{
					winnerObject.Winner = foundList;
				}
			}
		}

		private async Task<int> GetRemainingFilesCount()
		{
			int remainingFiles = 0;
			await gameDatabase.IterateOverAllRoundsOrMatches( false , async ( matchOrRound ) =>
			{
				IYoutube youtube = (IYoutube) matchOrRound;
				if( string.IsNullOrEmpty( youtube.YoutubeUrl ) )
				{
					Interlocked.Increment( ref remainingFiles );
				}
				await Task.CompletedTask;
			} );

			return remainingFiles;
		}

		private async Task<GlobalData> LoadDatabaseGlobalDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings )
		{
			return JsonConvert.DeserializeObject<GlobalData>( await File.ReadAllTextAsync( sharedSettings.GetGlobalPath() ) , JsonSettings );
		}

		private async Task<MatchData> LoadDatabaseMatchDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string matchName )
		{
			return JsonConvert.DeserializeObject<MatchData>( await File.ReadAllTextAsync( sharedSettings.GetMatchPath( matchName ) ) , JsonSettings );
		}

		private async Task<RoundData> LoadDatabaseRoundDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string roundName )
		{
			return JsonConvert.DeserializeObject<RoundData>( await File.ReadAllTextAsync( sharedSettings.GetRoundPath( roundName ) ) , JsonSettings );
		}

		private void OnResponseReceived( Video video )
		{
			if( uploaderSettings.pendingUploads.Contains( currentVideo ) )
			{
				uploaderSettings.pendingUploads.Remove( currentVideo );
			}

			SaveSettings().Wait();
			AddYoutubeIdToRound( currentVideo.videoName , video.Id ).Wait();

			Console.WriteLine( "Round {0} with id {1} was successfully uploaded." , currentVideo.videoName , video.Id );
		}

		private void OnStartUploading( IUploadSessionData resumable )
		{
			currentVideo.uploadUrl = resumable.UploadUri;
			SaveSettings().Wait();//save right away in case the program crashes or connection screws up
		}

		private void OnUploadProgress( IUploadProgress progress )
		{
			switch( progress.Status )
			{
				case UploadStatus.Uploading:
					{
						double percentage = Math.Round( ( (double) progress.BytesSent / (double) currentVideo.fileSize ) * 100f , 2 );
						//UpdateUploadProgress( percentage , true );
						Console.WriteLine( $"{currentVideo.videoName} : {percentage}%" );
						break;
					}
				case UploadStatus.Failed:
					Console.WriteLine( "An error prevented the upload from completing. {0}" , progress.Exception );
					break;
			}
		}

		private async Task RemoveVideoFile( string roundName )
		{
			RoundData roundData = await gameDatabase.GetRoundData( roundName );

			//don't accidentally delete stuff that somehow doesn't have a url set
			if( roundData.YoutubeUrl == null )
				return;

			try
			{
				string roundsFolder = Path.Combine( gameDatabase.SharedSettings.GetRecordingFolder() , gameDatabase.SharedSettings.roundsFolder );
				string filePath = Path.Combine( Path.Combine( roundsFolder , roundName ) , gameDatabase.SharedSettings.roundVideoFile );
				string reEncodedFilePath = Path.ChangeExtension( filePath , "converted.mp4" );

				if( File.Exists( filePath ) )
				{
					Console.WriteLine( "Removed video file for {0}" , roundName );
					File.Delete( filePath );
				}

				if( File.Exists( reEncodedFilePath ) )
				{
					Console.WriteLine( "Also removing the reencoded version {0}" , roundName );
					File.Delete( reEncodedFilePath );
				}
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}
		}

		private async Task SaveDatabaseGlobalDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , GlobalData globalData )
		{
			await File.WriteAllTextAsync( sharedSettings.GetGlobalPath() , JsonConvert.SerializeObject( globalData , Formatting.Indented , JsonSettings ) );
		}

		private async Task SaveDatabaseMatchDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string matchName , MatchData matchData )
		{
			await File.WriteAllTextAsync( sharedSettings.GetMatchPath( matchName ) , JsonConvert.SerializeObject( matchData , Formatting.Indented , JsonSettings ) );
		}

		private async Task SaveDatabaseRoundataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string roundName , RoundData roundData )
		{
			await File.WriteAllTextAsync( sharedSettings.GetRoundPath( roundName ) , JsonConvert.SerializeObject( roundData , Formatting.Indented , JsonSettings ) );
		}

		private async Task SetPresence( string str )
		{
			if( discordClient == null )
			{
				return;
			}

			if( discordClient.CurrentUser.Presence.Game != null && discordClient.CurrentUser.Presence.Game.Name == str )
			{
				return;
			}

			await discordClient.UpdateStatusAsync( new DSharpPlus.Entities.DiscordGame( str ) );
		}

		private async Task UpdateUploadProgress( double percentage )
		{
			await SetPresence( $"Uploading {currentVideo.videoName} : {percentage}%" );
		}

		private async Task UpdateUploadProgress( int remaining )
		{
			await SetPresence( $"{remaining} videos remaining" );
		}

		private async Task ProcessVideo( string roundName )
		{
			RoundData roundData = await gameDatabase.GetRoundData( roundName );

			string videoPath = gameDatabase.SharedSettings.GetRoundVideoPath( roundName );

			string outputPath = Path.ChangeExtension( videoPath , "converted.mp4" );

			if( roundData.RecordingType == RecordingType.Video && File.Exists( videoPath ) && !File.Exists( outputPath ) )
			{
				Console.WriteLine( $"Converting {roundName}" );

				IMediaInfo mediaInfo = await MediaInfo.Get( videoPath );
				IConversion newConversion = Conversion.New();

				bool abortConversion = mediaInfo.VideoStreams.Any( vid => vid.Bitrate < 6000000 );

				//only continue if these are videos with 6 megabits of bitrate
				if( abortConversion )
				{
					return;
				}

				foreach( var videostream in mediaInfo.VideoStreams )
				{
					newConversion.AddStream( videostream.SetCodec( VideoCodec.H264_nvenc ) );
				}

				foreach( var audiostream in mediaInfo.AudioStreams )
				{
					newConversion.AddStream( audiostream );
				}

				newConversion.SetOverwriteOutput( true );

				newConversion.SetOutput( outputPath );

				newConversion.AddParameter( "-crf 23 -maxrate 2000k -bufsize 4000k" );

				newConversion.SetPreset( ConversionPreset.Slow );
				Console.WriteLine( newConversion.Build() );
				await newConversion.Start();

				Console.WriteLine( outputPath );
			}
		}
		private async Task ProcessVideoFiles()
		{
			string tempFFmpegFolder = Path.Combine( Path.GetTempPath() , "ffmpeg" );

			if( !Directory.Exists( tempFFmpegFolder ) )
			{
				Directory.CreateDirectory( tempFFmpegFolder );
				Console.WriteLine( $"Created directory {tempFFmpegFolder}" );
			}

			FFmpeg.ExecutablesPath = tempFFmpegFolder;
			await FFmpeg.GetLatestVersion();

			GlobalData globalData = await gameDatabase.GetGlobalData();


			List<Task> processingTasks = new List<Task>();

			foreach( string roundName in globalData.Rounds )
			{
				processingTasks.Add( ProcessVideo( roundName ) );
			}

			await Task.WhenAll( processingTasks );
		}
	}
}