using DuckGame;
using MatchRecorder.Shared.Enums;
using MatchRecorder.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MatchRecorder;

internal sealed class ModSettingsMenu
{
	public event Action<ModSettings> GetOptions;
	public event Func<ModSettings> SetOptions;

	private MenuBoolean RecordingEnabled { get; } = new MenuBoolean();
	private string RecorderTypeString { get; set; }
	private List<string> RecorderTypeOptions { get; } = new List<string>();

	private UIMenu ModSettingsUIMenu { get; }

	public ModSettingsMenu()
	{
		SetupRecorderTypeEnum();
		CreateUI();
	}

	private void SetupRecorderTypeEnum()
	{
		RecorderTypeOptions.AddRange( Enum.GetNames( typeof( RecorderType ) ) );
		RecorderTypeString = RecorderTypeOptions.FirstOrDefault();
	}

	public void CreateUI()
	{

	}
}
