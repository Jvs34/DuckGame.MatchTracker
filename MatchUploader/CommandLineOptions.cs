using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace MatchUploader
{
	public class CommandLineOptions
	{
		[Option( 'm' , "mode" , Required = false , HelpText = "Sets the mode to run the program in, possible values are Normal, Client or Server" , Default = Modes.Normal )]
		public Modes Mode { get; set; }
	}
}
