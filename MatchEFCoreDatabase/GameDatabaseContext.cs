using Microsoft.EntityFrameworkCore;

namespace MatchTracker
{
	internal class GameDatabaseContext : DbContext
	{
		public DbSet<GlobalData> GlobalDataSet { get; set; }

		public DbSet<MatchData> MatchDataSet { get; set; }

		public DbSet<RoundData> RoundDataSet { get; set; }

		internal GameDatabaseContext( DbContextOptions options ) : base( options )
		{
		}

		protected override void OnConfiguring( DbContextOptionsBuilder optionsBuilder )
		{
			if( optionsBuilder.IsConfigured )
			{
				return;
			}

			optionsBuilder.UseInMemoryDatabase( "DuckGameRecordings" );
		}

		protected override void OnModelCreating( ModelBuilder modelBuilder )
		{
			modelBuilder.Entity<GlobalData>()
				.HasMany( prop => prop.matches );

			modelBuilder.Entity<MatchData>()
				.HasKey( key => key.name );

			modelBuilder.Entity<RoundData>()
				.HasKey( key => key.name );
		}
	}
}