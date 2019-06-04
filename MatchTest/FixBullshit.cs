using MatchTracker;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchTest
{
	public class FixBullshit
	{
		public async Task Run()
		{
			var Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
			.Build();

			IGameDatabase db = new FileSystemGameDatabase();
			Configuration.Bind( db.SharedSettings );
			//await db.Load();

			GlobalData globalData = await db.GetData<GlobalData>();

			var metaman = globalData.Players.Find( x => x.DatabaseIndex == "76561197999418456" );
			var willox = globalData.Players.Find( x => x.DatabaseIndex == "76561197998909316" );

			//get these two matches
			var match1 = await db.GetData<MatchData>( "2019-06-02 21-58-53" );
			await db.SaveData( match1 );



			ReplaceUserWith( match1.Players.Find( x => x.Name == "MetaMan" ) , metaman );

			foreach( var roundName in match1.Rounds )
			{
				RoundData roundData = await db.GetData<RoundData>( roundName );
				ReplaceUserWith( roundData.Players.Find( x => x.Name == "MetaMan" ) , metaman );

				//remove the duplicate metaman

				var metamen = roundData.Players.FindAll( x => x.Name == "MetaMan" );
				if( metamen.Count > 1 )
				{
					roundData.Players.Remove( metamen [1] );
				}

				//now add willox to the players and to the team ducks

				if( roundData.Players.Find( x => x.Name == "Willox" ) == null )
				{
					roundData.Players.Add( willox );

					TeamData ducksTeam = roundData.Teams.Find( x => x.HatName == "DUCKS" );

					if( ducksTeam != null )
					{
						ducksTeam.Players [0] = willox;
					}
				}


				await db.SaveData( roundData );
			}



			

		}

		public void ReplaceUserWith( PlayerData user , PlayerData substitute )
		{
			user.DiscordId = substitute.DiscordId;
			user.Name = substitute.Name;
			user.NickName = substitute.Name;
			user.UserId = substitute.UserId;
		}
	}
}
