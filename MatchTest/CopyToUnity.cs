using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MatchTracker;

namespace MatchTest
{
	public class CopyToUnity
	{
		public async Task Run()
		{
			var settingsPath = Path.Combine( Directory.GetCurrentDirectory() , "Settings" );

			IConfigurationRoot Configuration = new ConfigurationBuilder()
				.SetBasePath( settingsPath )
				.AddJsonFile( "unity.json" , true )
			.Build();

			string unityPath = Configuration ["MatchViewerProjectPath"];

			if( Directory.Exists( unityPath ) )
			{
				//now copy the json files too

				File.Copy( Path.Combine( settingsPath , "shared.json" ) , Path.Combine( unityPath , "Resources" , "Settings" , "shared.json" ) );


				var matchSharedAssembly = typeof( IGameDatabase ).Assembly;
				var matchSharedPath = matchSharedAssembly.Location;
				File.Copy( matchSharedPath , Path.Combine( unityPath , "Plugins" , Path.GetFileName( matchSharedPath ) ) , true );
			}
		}
	}
}
