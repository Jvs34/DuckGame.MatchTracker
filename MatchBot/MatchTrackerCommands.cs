//		[Command( "VoteMaps" ), Description( "Votes all maps registered in the database " )]
//		public async Task VoteMapsCommand( CommandContext ctx )
//		{
//			await VoteMapCommand( ctx , ( await DB.GetAll<LevelData>() ).ToArray() );
//		}

//		[Command( "VoteMap" )]
//		public async Task VoteMapCommand( CommandContext ctx , params string [] levelIDs )
//		{
//			await VoteDatabase<LevelData>( ctx , levelIDs );
//		}

//		[Command( "VoteRounds" )]
//		public async Task VoteRoundsCommand( CommandContext ctx , [Description( "The match id to vote rounds for" )] string matchName )
//		{
//			MatchData matchData = await DB.GetData<MatchData>( matchName );
//			if( matchData == null )
//			{
//				await ctx.RespondAsync( $"{matchName} is not a valid MatchData database index!" );
//				return;
//			}

//			await VoteRoundCommand( ctx , matchData.Rounds.ToArray() );
//		}

//		[Command( "VoteRound" )]
//		public async Task VoteRoundCommand( CommandContext ctx , [Description( "List of rounds to vote for, minimum 1" )] params string [] databaseIndexes )
//		{
//			await VoteDatabase<RoundData>( ctx , databaseIndexes );
//		}

//		/*
//		[Command( "AddEmojis" )]
//		public async Task AddEmojisCommand( CommandContext ctx )
//		{

//			var globalData = await DB.GetData<GlobalData>();

//			//UnicodeEmojis

//			var UnicodeEmojisProp = typeof( DiscordEmoji ).GetProperty( "UnicodeEmojis" , System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic );

//			IReadOnlyDictionary<string , string> unicodeEmojis = (IReadOnlyDictionary<string, string>) UnicodeEmojisProp.GetValue( null );


//			try
//			{
//				foreach( var unicodeKV in unicodeEmojis )
//				{
//					await DB.AddTag( unicodeKV.Value , unicodeKV.Key );
//				}
//			}
//			catch( Exception e )
//			{

//			}
//		}
//		*/

//#endregion

//#region UTILS
//		private async Task<bool> CheckReadOnly( CommandContext ctx )
//		{
//			if( DB.ReadOnly )
//			{
//				await ctx.RespondAsync( $"{DB.GetType()} is in readonly mode" );
//				return true;
//			}

//			return false;
//		}

//		private async Task VoteDatabase<T>( CommandContext ctx , params string [] databaseIndexes ) where T : IDatabaseEntry, ITagsList
//		{
//			if( await CheckReadOnly( ctx ) )
//			{
//				return;
//			}

//			if( ctx.Channel.IsPrivate )
//			{
//				await ctx.RespondAsync( "Sorry but DSharpPlus's reaction shit doesn't seem to work in DMs, use a proper channel please" );
//				return;
//			}

//			var message = await ctx.RespondAsync( "Looking through the database..." );

//			await ctx.Channel.TriggerTypingAsync();

//			var paginator = new DatabaseVotePaginator<T>( ctx.Client , DB , databaseIndexes , ctx.User , message );

//			var page = await paginator.GetPageAsync();

//			await message.ModifyAsync( page.Content , page.Embed );

//			await ctx.Client.GetInteractivity().WaitForCustomPaginationAsync( paginator );

//			await message.DeleteAsync();
//		}

//		private async Task<DateTime> GetLastTimePlayed( PlayerData player )
//		{
//			DateTime lastPlayed = DateTime.MinValue;
//			object lastPlayedLock = new object();

//			await DB.IterateOverAllRoundsOrMatches( true , async ( matchOrRound ) =>
//			{
//				if( player == null || matchOrRound.Players.Contains( player.DatabaseIndex ) )
//				{
//					IStartEnd startEnd = (IStartEnd) matchOrRound;
//					if( startEnd.TimeEnded > lastPlayed )
//					{
//						lock( lastPlayedLock )
//						{
//							lastPlayed = startEnd.TimeEnded;
//						}
//					}
//					await Task.CompletedTask;
//				}
//				return true;
//			} );

//			return lastPlayed;
//		}

//		private async Task<(int, TimeSpan)> GetTimesPlayed( PlayerData player , bool findmatchOrRound )
//		{
//			int timesPlayed = 0;
//			TimeSpan durationPlayed = TimeSpan.Zero;
//			object durationPlayedLock = new object();

//			await DB.IterateOverAllRoundsOrMatches( findmatchOrRound , async ( matchOrRound ) =>
//			{
//				await Task.CompletedTask;

//				if( player == null || matchOrRound.Players.Contains( player.UserId ) )
//				{
//					Interlocked.Increment( ref timesPlayed );

//					if( matchOrRound is IStartEnd duration )
//					{
//						lock( durationPlayedLock )
//						{
//							durationPlayed = durationPlayed.Add( duration.GetDuration() );
//						}
//					}
//				}
//				return true;

//			} );

//			return (timesPlayed, durationPlayed);
//		}

//		private CultureInfo GetLocale( DiscordClient client ) => CultureInfo.GetCultureInfo( client.CurrentUser.Locale );

//		private async Task<PlayerData> GetPlayer( DiscordUser discordUser )
//		{
//			PlayerData foundPlayerData = null;

//			var players = await DB.GetAllData<PlayerData>();

//			foundPlayerData = players.Find( p => p.DiscordId == discordUser.Id || p.DatabaseIndex == discordUser.Id.ToString() );

//			if( foundPlayerData == null )
//			{
//				foundPlayerData = players.Find( p =>
//				{
//					return string.Equals( p.NickName , discordUser.Username , StringComparison.CurrentCultureIgnoreCase )
//											|| string.Equals( p.Name , discordUser.Username , StringComparison.CurrentCultureIgnoreCase );
//				} );
//			}

//			return foundPlayerData;
//		}

//		private async Task<int> GetUploadsLeft()
//		{
//			int uploads = 0;

//			await DB.IterateOverAllRoundsOrMatches( false , async ( round ) =>
//			{
//				await Task.CompletedTask;

//				RoundData roundData = (RoundData) round;

//				if( roundData.VideoType == VideoType.VideoLink && string.IsNullOrEmpty( roundData.YoutubeUrl ) )
//				{
//					Interlocked.Increment( ref uploads );
//				}

//				return true;
//			} );

//			return uploads;
//		}

//		private async Task<(int, int)> GetPlayerWinsAndLosses( PlayerData player , bool ismatchOrRound )
//		{
//			int wins = 0;
//			int losses = 0;

//			await DB.IterateOverAllRoundsOrMatches( ismatchOrRound , async ( matchOrRound ) =>
//			{
//				//even if it's team mode we consider it a win
//				//first off, only do this if the play is actually in the match
//				if( matchOrRound.Players.Contains( player.DatabaseIndex ) )
//				{
//					if( matchOrRound.GetWinners().Contains( player.DatabaseIndex ) )
//					{
//						Interlocked.Increment( ref wins );
//					}
//					else
//					{
//						Interlocked.Increment( ref losses );
//					}
//				}
//				await Task.CompletedTask;
//				return true;
//			} );

//			return (wins, losses);
//		}
//#endregion
//	}
//}
