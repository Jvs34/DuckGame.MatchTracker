using DuckGame;
using MatchRecorder.Shared.Enums;
using MatchRecorder.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MatchRecorder;

public sealed class MatchRecorderMenu
{
	/// <summary>
	/// Called when we need to refresh the settings of the menu
	/// </summary>
	public event Func<ModSettings> GetOptions;
	public event Action<ModSettings> SetOptions;

	/// <summary>
	/// Called when the menu is closed and we need to restart the companion process
	/// </summary>
	public event Action ApplyOptions;

	public bool RecordingEnabled { get; set; }
	public int RecordingTypeInt { get; set; }
	private List<string> RecorderTypeOptions { get; } = new List<string>();

	#region UICOMPONENTS
	private UIComponent MainGroup { get; set; }
	private UIMenu Menu { get; set; }
	public UIMenuItemToggle RecordingEnabledToggle { get; private set; }
	public UIMenuItemNumber RecordingTypeSwitch { get; private set; }
	#endregion UICOMPONENTS

	public MatchRecorderMenu()
	{
		SetupRecorderTypeEnum();
		CreateUI();
	}

	private void SetupRecorderTypeEnum()
	{
		RecorderTypeOptions.AddRange( Enum.GetNames( typeof( RecorderType ) ) );
	}

	private void CreateUI()
	{
		MainGroup = new UIComponent( Layer.HUD.camera.width / 2f, Layer.HUD.camera.height / 2f, 0f, 0f );
		Menu = new UIMenu( "@LWING@MODRECORDER SETTINGS@RWING@", Layer.HUD.camera.width / 2f, Layer.HUD.camera.height / 2f, 200, -1f, "@CANCEL@CLOSE  @SELECT@SELECT" );
		Menu.SetBackFunction( new UIMenuActionCloseMenuCallFunction( MainGroup, CloseSettingsMenu ) );

		RecordingEnabledToggle = new UIMenuItemToggle( "RECORDING",
			new UIMenuActionCallFunction( OnSettingsChanged ),
			new FieldBinding( this, nameof( RecordingEnabled ) ) );

		RecordingTypeSwitch = new UIMenuItemNumber( "TYPE",
			new UIMenuActionCallFunction( OnSettingsChanged ),
			new FieldBinding( this, nameof( RecordingTypeInt ), 0, RecorderTypeOptions.Count - 1 ),
			valStrings: RecorderTypeOptions );

		Menu.Add( RecordingEnabledToggle );
		Menu.Add( RecordingTypeSwitch );
		MainGroup.Add( Menu );
		MainGroup.Close();
	}

	private void CloseSettingsMenu()
	{
		ApplyOptions();
	}

	private void OnSettingsChanged()
	{
		SetOptions( new ModSettings()
		{
			RecorderType = (RecorderType) RecordingTypeInt,
			RecordingEnabled = RecordingEnabled
		} );
	}

	private void RefreshSettings()
	{
		var options = GetOptions();

		RecordingEnabled = options.RecordingEnabled;
		RecordingTypeInt = (int) options.RecorderType;

		RecordingTypeSwitch.Activate( "ANYTHINGWILLREFRESHYOU" );
	}

	public void ShowUI( Level currentLevel )
	{
		if( MainGroup != null )
		{
			RefreshSettings();
			Level.Add( MainGroup );
			MonoMain.pauseMenu = MainGroup;
			MainGroup.Open();
		}
	}


}
