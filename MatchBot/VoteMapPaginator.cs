using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.EventHandling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MatchBot
{
	public class VoteMapPaginator : IPaginationRequest
	{
		public Task DoCleanupAsync()
		{
			throw new NotImplementedException();
		}

		public Task<PaginationEmojis> GetEmojisAsync()
		{
			throw new NotImplementedException();
		}

		public Task<DiscordMessage> GetMessageAsync()
		{
			throw new NotImplementedException();
		}

		public Task<Page> GetPageAsync()
		{
			throw new NotImplementedException();
		}

		public Task<TaskCompletionSource<bool>> GetTaskCompletionSourceAsync()
		{
			throw new NotImplementedException();
		}

		public Task<DiscordUser> GetUserAsync()
		{
			throw new NotImplementedException();
		}

		public Task NextPageAsync()
		{
			throw new NotImplementedException();
		}

		public Task PreviousPageAsync()
		{
			throw new NotImplementedException();
		}

		public Task SkipLeftAsync()
		{
			throw new NotImplementedException();
		}

		public Task SkipRightAsync()
		{
			throw new NotImplementedException();
		}
	}
}
