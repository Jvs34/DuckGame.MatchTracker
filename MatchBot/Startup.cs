using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Ai.LUIS;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MatchBot
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public Startup( IHostingEnvironment env )
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath( env.ContentRootPath )
				.AddJsonFile( "appsettings.json" , optional: true , reloadOnChange: true )
				.AddJsonFile( $"appsettings.{env.EnvironmentName}.json" , optional: true )
				.AddEnvironmentVariables();

			Configuration = builder.Build();
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices( IServiceCollection services )
		{
			services.AddBot<LuisMatchBot>( options =>
			{
				options.CredentialProvider = new ConfigurationCredentialProvider( Configuration );
				options.Middleware.Add( new CatchExceptionMiddleware<Exception>( async ( context , exception ) =>
				{
					await context.TraceActivity( "MatchBot Exception" , exception );
					await context.SendActivity( "Sorry, it looks like something went wrong!" );
				} ) );

				// The Memory Storage used here is for local bot debugging only. When the bot
				// is restarted, anything stored in memory will be gone. 
				IStorage dataStore = new MemoryStorage();

				options.Middleware.Add( new ConversationState<DuckGameDatabase>( dataStore ) );

				// Add LUIS recognizer as middleware
				options.Middleware.Add(
					new LuisRecognizerMiddleware(
						new LuisModel( "7ca2989c-899b-40ac-a8a6-a26c887080e6" , "2b40fa31e06a440cb98b783bb2d71a73" ,
							new Uri( "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/" )
						)
					)
				);
			} );
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure( IApplicationBuilder app , IHostingEnvironment env )
		{
			if( env.IsDevelopment() )
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseDefaultFiles()
				.UseStaticFiles()
				.UseBotFramework();
		}
	}
}
