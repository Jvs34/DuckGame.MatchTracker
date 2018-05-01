﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using MatchTracker;

/*
	Goes through all the folders, puts all rounds and matches into data.json
	Also returns match/round data from the timestamped name and whatnot

*/
namespace MatchUploader
{
	public class MatchManager
	{

		public MatchManager()
		{

		}


		//updates the global data.json
		public void UpdateGlobalData( String path )
		{
			//

			String roundsPath = Path.Combine( path , "rounds" );
			String matchesPath = Path.Combine( path , "matches" );

			if( !Directory.Exists( path ) || !Directory.Exists( roundsPath ) || !Directory.Exists( matchesPath ) )
			{
				throw new DirectoryNotFoundException( "Folders do not exist" );
			}

			String globalDataPath = Path.Combine( path , "data.json" );



			GlobalData globalData = new GlobalData();

			if( File.Exists( globalDataPath ) )
			{
				String fileData = File.ReadAllText( globalDataPath );

				globalData = JsonConvert.DeserializeObject<GlobalData>( fileData );
			}

			var roundFolders = Directory.EnumerateDirectories( roundsPath );

			Console.WriteLine( "Rounds\n" );
			foreach( var folderPath in roundFolders )
			{
				//if it doesn't contain the folder, check if the round is valid
				String folderName = Path.GetFileName( folderPath );
				Console.WriteLine( folderName + "\n" );
				
				if( !globalData.rounds.Contains( folderName ) )
				{
					globalData.rounds.Add( folderName );
				}
			}

			var matchFiles = Directory.EnumerateFiles( matchesPath );

			Console.WriteLine( "Matches\n" );
			foreach( var matchPath in matchFiles )
			{
				String matchName = Path.GetFileName( matchPath );
				Console.WriteLine( matchName + "\n" );
				if( !globalData.matches.Contains( matchName ))
				{
					globalData.matches.Add( matchName );
				}
			}


			File.WriteAllText( globalDataPath , JsonConvert.SerializeObject( globalData , Formatting.Indented ) );
		}


	}
}
