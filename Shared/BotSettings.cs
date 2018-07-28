using System;

namespace MatchTracker
{
	public class BotSettings
    {
		public String discordClientId;
		public String discordToken;
		public String luisModelId;
		public String luisSubcriptionKey;
		public Uri luisUri;

		/// <summary>
		/// The user to join the channel of for audio recording purposes
		/// </summary>
		public ulong discordUserToStalk;
    }
}
