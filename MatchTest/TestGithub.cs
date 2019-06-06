using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MatchTracker;
using Microsoft.Extensions.Configuration;

namespace MatchTest
{
	public class TestGithub
	{

		public async Task Run()
		{
			var Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
				.AddJsonFile( "uploader.json" )
			.Build();


			HttpClient httpClient = new HttpClient( new SocketsHttpHandler()
			{
				AllowAutoRedirect = true
			} );

			var db = new OctoKitGameDatabase( httpClient , Configuration ["GitUsername"] , Configuration ["GitPassword"] );
			Configuration.Bind( db.SharedSettings );

			await db.Load();

			var globalData = await db.GetData<GlobalData>();

			if( globalData.Tags.Count == 0 )
			{
				globalData.Tags.Add( new TagData()
				{
					Emoji = "😡",
					Name = "Angery",
				} );
			}

			await db.SaveData( globalData );
		}
	}
}
