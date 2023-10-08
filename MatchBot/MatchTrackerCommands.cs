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

//#endregion
//	}
//}
