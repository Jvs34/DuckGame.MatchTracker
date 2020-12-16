using MatchRecorderShared.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MatchRecorderShared
{
	public interface IMessageHandler
	{
		Task ReceiveStartMatchMessage( StartMatchMessage message );
		Task ReceiveEndMatchMessage( EndMatchMessage message );
		Task ReceiveStartRoundMessage( StartRoundMessage message );
		Task ReceiveEndRoundMessage( EndRoundMessage message );
		Task ReceiveShowHUDTextMessage( ShowHUDTextMessage message );
	}
}
