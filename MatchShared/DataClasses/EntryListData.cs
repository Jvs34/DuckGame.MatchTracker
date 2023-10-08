using MatchShared.Interfaces;
using System.Collections.Generic;

namespace MatchShared.DataClasses;

/// <summary>
/// Lists the entries currently present in the database to avoid expensive lookups
/// </summary>
public class EntryListData : IDatabaseEntry
{
	public string Type { get; set; }
	public string DatabaseIndex => Type;
	public HashSet<string> Entries { get; set; } = new HashSet<string>();
}
