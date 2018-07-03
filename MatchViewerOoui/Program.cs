using System;
using Ooui;
using Xamarin.Forms;

namespace MatchViewerOoui
{
	static class Program
	{
		static void Main( string [] args )
		{
			Forms.Init();

			MainPage mp = new MainPage();

			UI.Port = 9090;
			UI.Host = "localhost";

			UI.Publish( "/" , mp.GetOouiElement() );
			UI.Publish( "/gay" , mp.GetOouiElement() );
			Console.ReadLine();
		}
	}
}
