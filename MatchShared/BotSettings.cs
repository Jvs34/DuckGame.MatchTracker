using System;

namespace MatchTracker
{
	public class BotSettings
	{
		public string discordClientId { get; set; }
		public string discordToken { get; set; }

		/// <summary>
		/// The user to join the channel of for audio recording purposes
		/// </summary>
		public ulong discordUserToStalk { get; set; }

		public string luisModelId { get; set; }
		public string luisSubcriptionKey { get; set; }
		public Uri luisUri { get; set; }
	}
}