using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MatchViewer.Shared;
using MatchShared.Databases.Interfaces;
using MatchViewer.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault( args );
builder.RootComponents.Add<SharedApp>( "#app" );
builder.RootComponents.Add<HeadOutlet>( "head::after" );
builder.Services.AddHttpClient( "Anonymous", client => client.BaseAddress = new Uri( builder.HostEnvironment.BaseAddress ) );
builder.Services.AddSingleton<IGameDatabase, BlazorGameDatabase>();
SharedProgram.SetupServices( builder.Services, false, args );
SharedProgram.SetupConfiguration( builder.Configuration, false, args );

var app = builder.Build();

await app.RunAsync();