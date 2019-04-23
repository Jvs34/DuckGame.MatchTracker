using System;

namespace MatchTracker
{
	public class BotSettings
	{
		public string DiscordClientId { get; set; }
		public string DiscordToken { get; set; }

		/// <summary>
		/// The user to join the channel of for audio recording purposes
		/// </summary>
		public ulong DiscordUserToStalk { get; set; }

		public string LuisModelId { get; set; }
		public string LuisSubcriptionKey { get; set; }
		public Uri LuisUri { get; set; }

		public bool UseRemoteDatabase { get; set; }
	}
}