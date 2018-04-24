using System;
using System.Linq;
using System.Reflection;
using Harmony;
using DuckGame;
using OBSWebsocketDotNet;

namespace MatchRecorder
{
	public class MatchRecorderHandler
	{
		private OBSWebsocket handler;

		public MatchRecorderHandler()
		{
			handler = new OBSWebsocket();
			
		}
	}






	public class MatchRecorder_Overrides
	{

	}
}
