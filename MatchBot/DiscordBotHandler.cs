
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MatchBot
{
	public class DiscordBotHandler
	{
		private HttpClient HttpClient { get; } = new HttpClient();
		private BotSettings BotSettings { get; } = new BotSettings();
		private DiscordClient DiscordInstance { get; }
		private IConfigurationRoot Configuration { get; }
		private IServiceCollection Services { get; }
		public IGameDatabase DB { get; }
		private CommandsNextExtension CommandsModule { get; }
		private InteractivityExtension InteractivityModule { get; }

		public DiscordBotHandler( string [] args )
		{

			BotSettings = new BotSettings();
			Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
				.AddJsonFile( "bot.json" )
				.AddJsonFile( "uploader.json" )
				.AddCommandLine( args )
			.Build();

			Configuration.Bind( BotSettings );

			if( BotSettings.UseRemoteDatabase )
			{
				DB = new OctoKitGameDatabase( HttpClient , Configuration ["GitUsername"] , Configuration ["GitPassword"] )
				{
					InitialLoad = true,
				};
			}
			else
			{
				DB = new FileSystemGameDatabase();
			}

			
			Configuration.Bind( DB.SharedSettings );

			DiscordInstance = new DiscordClient( new DiscordConfiguration()
			{
				AutoReconnect = true ,
				TokenType = TokenType.Bot ,
				Token = BotSettings.DiscordToken ,
#if DEBUG
				LogLevel = LogLevel.Debug ,
#endif
			} );

			//TODO:not sure if this has to be any other way, but singleton for now will work
			Services = new ServiceCollection()
				.AddSingleton( DB );

			CommandsModule = DiscordInstance.UseCommandsNext( new CommandsNextConfiguration()
			{
				Services = Services.BuildServiceProvider() ,
			} );

			InteractivityModule = DiscordInstance.UseInteractivity( new InteractivityConfiguration() );

			CommandsModule.RegisterCommands<MatchTrackerCommands>();
		}

		public async Task Initialize()
		{
			await DB.Load();
			await DiscordInstance.ConnectAsync( status: UserStatus.Online );
			await DiscordInstance.InitializeAsync();
		}

	}
}
