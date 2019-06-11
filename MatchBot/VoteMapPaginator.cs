using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.EventHandling;
using Humanizer;
using Humanizer.Localisation;
using MatchTracker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace MatchBot
{
	public class VoteMapPaginator : IPaginationRequest
	{
		private PaginationEmojis PaginationEmojis { get; } = new PaginationEmojis();

		private TaskCompletionSource<bool> CompletitionCondition { get; } = new TaskCompletionSource<bool>();

		private List<string> Levels { get; }

		private int CurrentIndex { get; set; }

		private string CurrentLevel
		{
			get
			{
				return Levels [CurrentIndex];
			}
		}

		private IGameDatabase DB { get; }

		private DiscordMessage PaginatorMessage { get; }
		private CancellationTokenSource TimeoutToken { get; }

		private DiscordUser User { get; }

		public VoteMapPaginator( DiscordUser usr , IGameDatabase database , IEnumerable<string> levels , DiscordMessage message )
		{
			User = usr;
			DB = database;
			Levels = new List<string>( levels );
			PaginatorMessage = message;

			TimeoutToken = new CancellationTokenSource( TimeSpan.FromSeconds( 120 ) );
			TimeoutToken.Token.Register( () => CompletitionCondition.TrySetResult( true ) );

			//no need for pagination emojis when it's only one level to vote for
			if( Levels.Count == 1 )
			{
				PaginationEmojis.Left = null;
				PaginationEmojis.SkipLeft = null;
				PaginationEmojis.Right = null;
				PaginationEmojis.SkipRight = null;
			}
		}

		private async Task<Page> GetPageFromLevel()
		{
			LevelData levelData = await DB.GetData<LevelData>( CurrentLevel );

			if( levelData == null )
			{
				return new Page( $"No data found in database about {CurrentLevel}" );
			}

			DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
			{
				Color = DiscordColor.Orange ,
				Author = new DiscordEmbedBuilder.EmbedAuthor()
				{
					Name = levelData.Author ,
				} ,
				Description = levelData.Description ,
				ImageUrl = DB.SharedSettings.GetLevelPreviewPath( CurrentLevel , true ) ,
				Title = Path.GetFileName( levelData.FilePath ) ,
				Footer = new DiscordEmbedBuilder.EmbedFooter() ,
			};

			StringBuilder builder = new StringBuilder();

			builder.Append( "Current Tags: " );

			if( levelData.Tags.Count == 0 )
			{
				builder.Append( "No Tags" );
			}

			foreach( var tagName in levelData.Tags )
			{
				var tagData = await DB.GetData<TagData>( tagName );

				if( tagData != null )
				{
					builder.Append( DiscordEmoji.FromUnicode( tagData.Emoji ) );
				}
			}

			embed.Footer.Text = builder.ToString();

			return new Page( null , embed );
		}

		private async Task SaveCurrentEmojis()
		{
			LevelData levelData = await DB.GetData<LevelData>( CurrentLevel );

			if( levelData == null )
			{
				return;
			}

			var globalData = await DB.GetData<GlobalData>();

			var message = await GetMessageAsync();
			//get all reactions except for the pagination emojis

			message = await message.Channel.GetMessageAsync( message.Id );

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
					string emojiDatabaseIndex = string.Join( ' ' , Encoding.UTF8.GetBytes( emoji.Name ) );

					TagData tagData = await DB.GetData<TagData>( emojiDatabaseIndex );

					//create the emoji now
					if( tagData == null )
					{
						tagData = new TagData()
						{
							Name = emojiDatabaseIndex ,
							Emoji = emoji.Name ,
						};

						await DB.SaveData( tagData );
					}

					if( !globalData.Tags.Contains( emojiDatabaseIndex ) )
					{
						globalData.Tags.Add( emojiDatabaseIndex );
						await DB.SaveData( globalData );
					}

					if( !levelData.Tags.Contains( emojiDatabaseIndex ) )
					{
						levelData.Tags.Add( emojiDatabaseIndex );
						await DB.SaveData( levelData );
					}
				}



			}

		}

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

		public async Task<Page> GetPageAsync()
		{
			await Task.CompletedTask;
			return await GetPageFromLevel();
		}

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
			await SaveCurrentEmojis();
			if( CurrentIndex < Levels.Count - 1 )
			{
				CurrentIndex++;
			}
		}

		public async Task PreviousPageAsync()
		{
			await SaveCurrentEmojis();
			if( CurrentIndex > 0 )
			{
				CurrentIndex--;
			}
		}

		public async Task SkipLeftAsync()
		{
			await SaveCurrentEmojis();
			CurrentIndex = 0;
		}

		public async Task SkipRightAsync()
		{
			await SaveCurrentEmojis();
			CurrentIndex = Levels.Count - 1;
		}
	}
}
