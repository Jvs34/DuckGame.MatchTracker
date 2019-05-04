using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MatchTracker
{
	public class HttpGameDatabase : GameDatabase
	{
		private HttpClient Client { get; }
		private JsonSerializerSettings JsonSettings { get; }

		public HttpGameDatabase( HttpClient httpClient )
		{
			Client = httpClient;

			JsonSettings = new JsonSerializerSettings()
			{
				PreserveReferencesHandling = PreserveReferencesHandling.Objects ,
			};

			LoadGlobalDataDelegate += LoadDatabaseGlobalDataWeb;
			LoadMatchDataDelegate += LoadDatabaseMatchDataWeb;
			LoadRoundDataDelegate += LoadDatabaseRoundDataWeb;
		}

		private async Task<GlobalData> LoadDatabaseGlobalDataWeb( IGameDatabase gameDatabase , SharedSettings sharedSettings )
		{
			var response = await Client.GetStringAsync( sharedSettings.GetGlobalPath( true ) );
			Console.WriteLine( "Loading GlobalData" );
			return JsonConvert.DeserializeObject<GlobalData>( HttpUtility.HtmlDecode( response ) , JsonSettings );
		}

		private async Task<MatchData> LoadDatabaseMatchDataWeb( IGameDatabase gameDatabase , SharedSettings sharedSettings , string matchName )
		{
			var response = await Client.GetStringAsync( sharedSettings.GetMatchPath( matchName , true ) );
			Console.WriteLine( $"Loading MatchData {matchName}" );
			return JsonConvert.DeserializeObject<MatchData>( HttpUtility.HtmlDecode( response ) , JsonSettings );
		}

		private async Task<RoundData> LoadDatabaseRoundDataWeb( IGameDatabase gameDatabase , SharedSettings sharedSettings , string roundName )
		{
			var response = await Client.GetStringAsync( sharedSettings.GetRoundPath( roundName , true ) );
			Console.WriteLine( $"Loading RoundData {roundName}" );
			return JsonConvert.DeserializeObject<RoundData>( HttpUtility.HtmlDecode( response ) , JsonSettings );
		}
	}
}
