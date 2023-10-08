using DSharpPlus;
using DSharpPlus.SlashCommands;
using MatchBot.Discord;
using MatchShared.Databases.Settings;
using Microsoft.Extensions.Options;

namespace MatchBot.Services;

internal class DiscordService : BackgroundService
{
	private DiscordClient DiscordInstance { get; }
	private ILogger<DiscordService> Logger { get; }
	private BotSettings BotOptions { get; }

	public DiscordService( ILogger<DiscordService> logger, IOptions<BotSettings> botSettings, IServiceProvider serviceProvider )
	{
		Logger = logger;
		BotOptions = botSettings.Value;

		var discordConfig = new DiscordConfiguration()
		{
			AutoReconnect = true,
			AlwaysCacheMembers = false,
			Token = BotOptions.DiscordToken,
			TokenType = TokenType.Bot,
#if DEBUG
			MinimumLogLevel = LogLevel.Debug,
#endif
			MessageCacheSize = 10,
		};

		DiscordInstance = new DiscordClient( discordConfig );
		var slash = DiscordInstance.UseSlashCommands( new SlashCommandsConfiguration()
		{
			Services = serviceProvider,
		} );
		slash.RegisterCommands<MatchTrackerSlashCommands>();
	}

	protected override async Task ExecuteAsync( CancellationToken stoppingToken )
	{
		Logger.LogInformation( "Starting up discord" );
		await DiscordInstance.ConnectAsync();
		//await DiscordInstance.ConnectAsync( status: DSharpPlus.Entities.UserStatus.Invisible );

		while( !stoppingToken.IsCancellationRequested )
		{
			await Task.Delay( 100, stoppingToken );
		}

		Logger.LogInformation( "Stopping discord" );
		await DiscordInstance.DisconnectAsync();
	}
}
