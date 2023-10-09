using MatchShared.Databases.Interfaces;
using MatchShared.Databases.LiteDB;
using MatchViewer.Shared;

var builder = WebApplication.CreateBuilder( args );
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient( "Anonymous" );
SharedProgram.SetupServices( builder.Services, true, args );
SharedProgram.SetupConfiguration( builder.Configuration, false, args );

builder.Services.AddSingleton<IGameDatabase, LiteDBGameDatabase>();

var app = builder.Build();

if( !app.Environment.IsDevelopment() )
{
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage( "/_Host" );

await app.RunAsync();