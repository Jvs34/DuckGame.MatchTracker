using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace MatchTracker
{
	internal class GameDatabaseContext : DbContext
	{
		//public DbSet<GlobalData> GlobalDataSet { get; set; }
		//public DbSet<MatchData> MatchDataSet { get; set; }
		public DbSet<PlayerData> PlayerDataSet { get; set; }

		public DbSet<RoundData> RoundDataSet { get; set; }
		public DbSet<TeamData> TeamDataSet { get; set; }

		internal GameDatabaseContext( DbContextOptions options ) : base( options )
		{
		}

		protected override void OnConfiguring( DbContextOptionsBuilder optionsBuilder )
		{
			if( optionsBuilder.IsConfigured )
			{
				return;
			}

			optionsBuilder
				.UseInMemoryDatabase( "DuckGameRecordings" )
				.EnableSensitiveDataLogging();
		}

		protected override void OnModelCreating( ModelBuilder modelBuilder )
		{
			modelBuilder.Entity<RoundData>()
				.HasKey( key => key.name );

			modelBuilder.Entity<RoundData>()
				.HasMany( prop => prop.players )
				.WithOne();

			modelBuilder.Entity<PlayerData>()
				.HasKey( key => key.userId );

			modelBuilder.Entity<TeamData>()
				.HasKey( key => new { key.hatName , key.isCustomHat , key.score } );

			/*
			modelBuilder.Entity<MatchData>()
				.HasKey( key => key.name );

			modelBuilder.Entity<GlobalData>()
				.HasMany( prop => prop.matches );
			*/
		}
	}
}