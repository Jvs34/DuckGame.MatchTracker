using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DuckGame;

namespace MatchRecorder
{
    public class GenerateLevelImage
    {
		public GenerateLevelImage()
		{
			var levels = Content.GetLevels( "deathmatch" , LevelLocation.Content );
			foreach( String level in levels )
			{
				String levelid = Content.GetLevelID( level , LevelLocation.Content );
				Tex2D rt = Content.GeneratePreview( level );
				LevelData levelData = Content.GetLevel( levelid , LevelLocation.Content );


			}
		}
    }
}
