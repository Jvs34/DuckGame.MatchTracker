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
using System.Globalization;
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

			TimeoutToken = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );
			TimeoutToken.Token.Register( () => CompletitionCondition.TrySetResult( true ) );
		}

		private async Task<Page> GetPageFromLevel()
		{
			Page levelPage = new Page( $"lmao {CurrentLevel}" );
			Console.WriteLine( $"fucking {CurrentLevel}" );
			return levelPage;
		}


		public async Task DoCleanupAsync()
		{
			await Task.CompletedTask;
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
			await Task.CompletedTask;
			if( CurrentIndex < Levels.Count - 1 )
			{
				CurrentIndex++;
			}
		}

		public async Task PreviousPageAsync()
		{
			await Task.CompletedTask;
			if( CurrentIndex > 0 )
			{
				CurrentIndex--;
			}
		}

		public async Task SkipLeftAsync()
		{
			await Task.CompletedTask;
			CurrentIndex = 0;
		}

		public async Task SkipRightAsync()
		{
			await Task.CompletedTask;
			CurrentIndex = Levels.Count - 1;
		}
	}
}
