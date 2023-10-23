using DuckGame;
using MatchShared.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchRecorder.Utils;

internal static class RecorderUtils
{
	public static KeyValuePair<Profile, string> GetBestDestroyTypeKillerAndWeapon( DestroyType destroyType )
	{
		//try a direct check, easiest one
		Profile profile = destroyType.responsibleProfile;

		string weapon = string.Empty;

		if( destroyType is DTShot shotType && shotType.bulletFiredFrom != null )
		{
			//god, grenade launchers are a pain in the ass
			var type = shotType.bulletFiredFrom.GetType();

			if( shotType.bulletFiredFrom.killThingType != null )
			{
				type = shotType.bulletFiredFrom.killThingType;
			}

			if( shotType.bulletFiredFrom.responsibleProfile != null )
			{
				profile = destroyType.responsibleProfile;
			}

			weapon = type.Name;
		}

		//... I know I know, but either I Import the tuples nuget or I make my own struct, so whatever
		return new KeyValuePair<Profile, string>( profile, weapon );
	}

	public static ObjectData ConvertThingToObjectData( Thing killingObject ) => killingObject is null ? null : new()
	{
		ClassName = killingObject.GetType().Name,
		BioDescription = killingObject is Gun gun ? gun.bio : string.Empty,
		EditorName = killingObject.editorName,
		EditorTooltip = killingObject.editorTooltip
	};

	public static TeamData ConvertDuckGameTeamToTeamData( Team duckgameteam ) => duckgameteam is null ? null : new()
	{
		HasHat = duckgameteam.hasHat,
		Score = duckgameteam.score,
		HatName = duckgameteam.name,
		IsCustomHat = duckgameteam.customData != null,
		Players = duckgameteam.activeProfiles.Select( x => GetPlayerID( x ) ).ToList()
	};

	public static PlayerData ConvertDuckGameProfileToPlayerData( Profile profile ) => new()
	{
		Name = profile.name,
		UserId = GetPlayerID( profile )
	};

	public static TeamData ConvertDuckGameProfileToTeamData( Profile profile )
	{
		var teamData = ConvertDuckGameTeamToTeamData( profile.team );

		if( teamData != null )
		{
			teamData.Players = teamData.Players.Where( x => x.Equals( GetPlayerID( profile ), StringComparison.InvariantCultureIgnoreCase ) ).ToList();
		}

		return teamData;
	}

	public static string GetPlayerID( Profile profile )
	{
		var id = profile.id;

		if( Network.isActive )
		{
			var steamid = profile.steamID.ToString();

			id = steamid;

			if( profile.isRemoteLocalDuck )
			{
				id = $"{steamid}_{profile.name}";
			}
		}

		return id;
	}
}
