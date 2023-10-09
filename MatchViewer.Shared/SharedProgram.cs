using MatchShared.Databases.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace MatchViewer.Shared;

public static class SharedProgram
{
	public static void SetupServices( IServiceCollection services, bool isServer, params string[] args )
	{
		services.AddMudServices();
		services.AddOptions<SharedSettings>().BindConfiguration( string.Empty );
	}

	public static void SetupConfiguration( IConfigurationBuilder configuration, bool isServer, params string[] args )
	{

	}
}
