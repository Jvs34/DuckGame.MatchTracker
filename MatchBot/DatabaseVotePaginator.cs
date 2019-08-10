using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.EventHandling;
using MatchTracker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchBot
{
	public class DatabaseVotePaginator<T> : IPaginationRequest where T : IDatabaseEntry, ITagsList
	{
		public List<string> DatabaseIndexes { get; }
		private DiscordClient DiscordClient { get; }
		private IGameDatabase DB { get; }
		private DiscordMessage PaginatorMessage { get; }
		private CancellationTokenSource TimeoutToken { get; }
		private DiscordUser User { get; }
		private PaginationEmojis PaginationEmojis { get; } = new PaginationEmojis();
		private TaskCompletionSource<bool> CompletitionCondition { get; } = new TaskCompletionSource<bool>();

		private int CurrentIndex { get; set; }

		private string CurrentItem
		{
			get
			{
				return DatabaseIndexes [CurrentIndex];
			}
		}

		public DatabaseVotePaginator( DiscordClient client , IGameDatabase database , IEnumerable<string> databaseIndexes , DiscordUser usr , DiscordMessage message )
		{
			DiscordClient = client;
			DatabaseIndexes = new List<string>( databaseIndexes );
			User = usr;
			DB = database;
			PaginatorMessage = message;
			TimeoutToken = new CancellationTokenSource( TimeSpan.FromMinutes( 5 ) );
			TimeoutToken.Token.Register( () => CompletitionCondition.TrySetResult( true ) );

			//no need for pagination emojis when it's only one item to vote for
			if( DatabaseIndexes.Count == 1 )
			{
				PaginationEmojis.Left = null;
				PaginationEmojis.SkipLeft = null;
				PaginationEmojis.Right = null;
				PaginationEmojis.SkipRight = null;
			}
		}

		private async Task SaveCurrentEmojis( bool cleanEmojiTags = false )
		{
			T itemData = await DB.GetData<T>( CurrentItem );

			if( itemData == null )
			{
				return;
			}

			var message = await GetMessageAsync();
			//get all reactions except for the pagination emojis

			message = await message.Channel.GetMessageAsync( message.Id );

			var emojiDeletionTasks = new List<Task>();

			foreach( var reaction in message.Reactions )
			{
				var emoji = reaction.Emoji;

				if( emoji == PaginationEmojis.Left
					|| emoji == PaginationEmojis.SkipLeft
					|| emoji == PaginationEmojis.Right
					|| emoji == PaginationEmojis.SkipRight
					|| emoji == PaginationEmojis.Stop )
				{
					continue;
				}

				//we only care about unicode emojis
				if( emoji.Id == 0 )
				{
					await DB.AddTag( emoji.Name , emoji.GetDiscordName() , itemData );
					await DB.SaveData( itemData );
				}

				//clean every single emoji from this reaction type
				if( cleanEmojiTags )
				{
					var usersRated = await message.GetReactionsAsync( reaction.Emoji );
					foreach( var user in usersRated )
					{
						emojiDeletionTasks.Add( message.DeleteReactionAsync( reaction.Emoji , user ) );
					}
				}

			}


			await Task.WhenAll( emojiDeletionTasks );
		}

		private async Task<Page> GetPageFromItem()
		{
			T itemData = await DB.GetData<T>( CurrentItem );

			if( itemData == null )
			{
				return new Page( $"No {typeof( T )} found in database with id {CurrentItem}" );
			}

			DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
			{
				Color = DiscordColor.Orange ,
				Footer = new DiscordEmbedBuilder.EmbedFooter() ,
				Title = itemData.DatabaseIndex ,
			};

			//TODO: add stuff in between for RoundData and MatchData too

			if( itemData is IStartEnd startEnd )
			{
				embed.Timestamp = startEnd.TimeStarted;
			}

			if( itemData is IPlayersList playerList )
			{
				StringBuilder descriptionBuilder = new StringBuilder( "Players: " );

				//see if this user has a discord id, then use the mention thinghy to add him
				foreach( var player in playerList.Players )
				{
					var playerData = await DB.GetData<PlayerData>( player );

					string playerName = playerData.GetName();

					DiscordUser discordUser = await DiscordClient.GetUserAsync( playerData.DiscordId );

					if( discordUser != null )
					{
						playerName = discordUser.Mention;
					}

					descriptionBuilder.Append( playerName );
				}

				embed.Description = descriptionBuilder.ToString();
			}

			if( itemData is IWinner winner )
			{
				var playerWinners = await DB.GetAllData<PlayerData>( winner.GetWinners().ToArray() );

				string winnerName = string.Join( " " , playerWinners.Select( x => x.GetName() ) );

				if( string.IsNullOrEmpty( winnerName ) )
				{
					winnerName = "Nobody";
				}

				embed.Author = new DiscordEmbedBuilder.EmbedAuthor()
				{
					Name = $"Winner: {winnerName}" ,
				};

				if( winner.Winner?.Players.Count == 1 )
				{
					//see if it's a valid discord user
					var firstPlayer = await DB.GetData<PlayerData>( winner.Winner.Players.FirstOrDefault() );

					var discordUser = await DiscordClient.GetUserAsync( firstPlayer.DiscordId );
					if( discordUser != null )
					{
						embed.Author.IconUrl = discordUser.AvatarUrl;
					}
				}
			}

			if( itemData is IVideoUpload videoUpload && !string.IsNullOrEmpty( videoUpload.YoutubeUrl ) )
			{
				switch( videoUpload.VideoType )
				{
					case VideoType.VideoLink:
						{
							embed.Url = $"https://www.youtube.com/watch?v={videoUpload.YoutubeUrl}";

							embed.ThumbnailUrl = $"https://img.youtube.com/vi/{videoUpload.YoutubeUrl}/maxresdefault.jpg";

							//embed.ImageUrl = $"https://img.youtube.com/vi/{videoUpload.YoutubeUrl}/maxresdefault.jpg";
							//embed.ThumbnailUrl = $"https://img.youtube.com/vi/{videoUpload.YoutubeUrl}/0.jpg";
						}
						break;

					case VideoType.PlaylistLink:
						{
							embed.Url = $"https://www.youtube.com/playlist?list={videoUpload.YoutubeUrl}";
						}
						break;
					/*
					case VideoType.MergedVideoLink:
						{
							//TODO: if this is ever actually a thing, append the timing at the end of the url
						}
						break;
					*/
					default:
						break;
				}
			}

			if( itemData is LevelData levelData )
			{
				embed.Author = new DiscordEmbedBuilder.EmbedAuthor()
				{
					Name = levelData.Author ,
				};
				embed.Description = levelData.Description;
				embed.ImageUrl = DB.SharedSettings.GetLevelPreviewPath( CurrentItem , true );
				embed.Title = $"{Path.GetFileName( levelData.FilePath )} ({CurrentItem})";
			}

			StringBuilder builder = new StringBuilder();

			builder.Append( "Current Tags: " );

			if( itemData.Tags.Count == 0 )
			{
				builder.Append( "No Tags" );
			}

			foreach( var tagName in itemData.Tags )
			{
				var tagData = await DB.GetData<TagData>( tagName );

				if( tagData != null )
				{
					builder.Append( DiscordEmoji.FromUnicode( tagData.Emoji ) );
				}
			}

			embed.Footer.Text = builder.ToString();

			return new Page( embed: embed );
		}


		#region PAGINATOR STUFF
		public async Task DoCleanupAsync()
		{
			await SaveCurrentEmojis();

			await PaginatorMessage.DeleteAllReactionsAsync();
		}

		public async Task<PaginationEmojis> GetEmojisAsync()
		{
			await Task.CompletedTask;
			return PaginationEmojis;
		}

		public async Task<DiscordMessage> GetMessageAsync()
		{
			await Task.CompletedTask;
			return PaginatorMessage;
		}

		public async Task<Page> GetPageAsync() => await GetPageFromItem();

		public async Task<TaskCompletionSource<bool>> GetTaskCompletionSourceAsync()
		{
			await Task.CompletedTask;
			return CompletitionCondition;
		}

		public async Task<DiscordUser> GetUserAsync()
		{
			await Task.CompletedTask;
			return User;
		}

		public async Task NextPageAsync()
		{
			await SaveCurrentEmojis( true );

			if( CurrentIndex < DatabaseIndexes.Count - 1 )
			{
				CurrentIndex++;
			}
		}

		public async Task PreviousPageAsync()
		{
			await SaveCurrentEmojis( true );

			if( CurrentIndex > 0 )
			{
				CurrentIndex--;
			}
		}

		public async Task SkipLeftAsync()
		{
			await SaveCurrentEmojis( true );

			CurrentIndex = 0;
		}

		public async Task SkipRightAsync()
		{
			await SaveCurrentEmojis( true );

			CurrentIndex = DatabaseIndexes.Count - 1;
		}
		#endregion
	}
}