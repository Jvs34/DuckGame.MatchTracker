using System;

namespace MatchUploader
{
	class Program
	{
		static void Main( string [] args )
		{
			//%CCYY-%MM-%DD %hh-%mm-%ss
			String dateFormat = "yyyy-MM-dd HH-mm-ss";

			DateTime date = DateTime.Now;

			Console.WriteLine( date.ToString( dateFormat ) );
			Console.ReadKey();
		}
	}
}
