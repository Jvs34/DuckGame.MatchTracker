using MatchTracker;
using MatchTracker.Replay;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace MatchTest
{//
	internal class TestReplay
	{
		public FileSystemGameDatabase GameDatabase { get; private set; }

		public async Task Test()
		{
			string roundName = "2019-05-17 23-08-51";

			ReplayRecording.InitProtoBuf();

			File.WriteAllText( @"C:\Users\Jvsth.000.000\OneDrive\Documents\DuckGame\Mods\MatchTracker\MatchShared.Replay\replay.proto" , ReplayRecording.GetProto() );

			//TODO: unhardcode and then check for webgl to load with the unity webrequest shit

			GameDatabase = new FileSystemGameDatabase
			{
				SharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( @"C:\Users\Jvsth.000.000\OneDrive\Documents\DuckGame\Mods\MatchTracker\Settings\shared_debug.json" ) )
			};


			string replayPath = GameDatabase.SharedSettings.GetRoundReplayPath( roundName );

			Replay recording = null;

			using( var fileStream = File.OpenRead( replayPath ) )//TODO: if webgl or windows build then fetch from internet or filesystem
			using( var archive = new ZipArchive( fileStream ) )
			{
				var stream = archive.GetEntry( GameDatabase.SharedSettings.RoundReplayFile )?.Open();
				recording = ReplayRecording.Unserialize( stream );
			}
		}
	}
}
