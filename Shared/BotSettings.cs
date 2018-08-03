using System;

namespace MatchTracker
{
	public class BotSettings
	{
		public String discordClientId { get; set; }
		public String discordToken { get; set; }

		/// <summary>
		/// The user to join the channel of for audio recording purposes
		/// </summary>
		public ulong discordUserToStalk { get; set; }

		public String luisModelId { get; set; }
		public String luisSubcriptionKey { get; set; }
		public Uri luisUri { get; set; }
	}
}