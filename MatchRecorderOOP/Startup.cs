using MatchRecorder.Initializers;
using MatchTracker;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MatchRecorder
{
	internal class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices( IServiceCollection services )
		{
			services.AddSignalR();
			services.AddAsyncInitializer<IGameDatabaseInitializer>();

			//database
			services.AddSingleton<IGameDatabase , LiteDBGameDatabase>();

			//recorder
			services.AddSingleton<IModToRecorderMessageQueue , ModToRecorderMessageQueue>();
			services.AddHostedService<MatchRecorderService>();
			services.AddHostedService<RecorderToModSenderService>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure( IApplicationBuilder app , IWebHostEnvironment env )
		{
			app.UseRouting();

			app.UseEndpoints( endpoints =>
			{
				endpoints.MapHub<MatchRecorderHub>( "/MatchRecorderHub" );
			} );
		}
	}
}