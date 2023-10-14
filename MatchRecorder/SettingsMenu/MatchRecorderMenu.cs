using DuckGame;
using MatchRecorder.Shared.Enums;
using MatchRecorder.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MatchRecorder;

public sealed class MatchRecorderMenu
{
	public event Func<ModSettings> GetOptions;
	public event Action<ModSettings> SetOptions;
	public event Action ApplyOptions;

	/// <summary>
	/// Called when the option "RESTART" was called
	/// </summary>
	public event Action RestartCompanion;

	public event Action GenerateThumbnails;

	public bool RecordingEnabled { get; set; }
	public int RecordingTypeInt { get; set; }
	private List<string> RecorderTypeOptions { get; } = new List<string>();

	#region UICOMPONENTS
	private UIComponent MainGroup { get; set; }
	private UIMenu Menu { get; set; }
	private UIMenuItemToggle RecordingEnabledToggle { get; set; }
	private UIMenuItemNumber RecordingTypeSwitch { get; set; }
	private UIMenuItem RestartCompanionButton { get; set; }
	private UIMenuItem GenerateThumbnailsButton { get; set; }
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

		Menu = new UIMenu( "@LWING@MODRECORDER SETTINGS@RWING@", Layer.HUD.camera.width / 2f, Layer.HUD.camera.height / 2f, 240, -1f, "@CANCEL@CLOSE  @SELECT@SELECT" );
		Menu.SetBackFunction( new UIMenuActionCloseMenuCallFunction( MainGroup, CloseSettingsMenu ) );
		MainGroup.Add( Menu );

		RecordingEnabledToggle = new UIMenuItemToggle( "RECORDING",
			new UIMenuActionCallFunction( OnSettingsChanged ),
			new FieldBinding( this, nameof( RecordingEnabled ) ) );
		Menu.Add( RecordingEnabledToggle );

		RecordingTypeSwitch = new UIMenuItemNumber( "TYPE",
			new UIMenuActionCallFunction( OnSettingsChanged ),
			new FieldBinding( this, nameof( RecordingTypeInt ), 0, RecorderTypeOptions.Count - 1 ),
			valStrings: RecorderTypeOptions );
		Menu.Add( RecordingTypeSwitch );

		RestartCompanionButton = new UIMenuItem( "RESTART",
			new UIMenuActionCallFunction( RestartCompanionCallback ) );
		Menu.Add( RestartCompanionButton );

		GenerateThumbnailsButton = new UIMenuItem( "GENERATE LEVEL THUMBNAILS",
			new UIMenuActionCallFunction( GenerateThumbnailsCallback ) );
		Menu.Add( GenerateThumbnailsButton );

		MainGroup.Close();
	}

	private void GenerateThumbnailsCallback()
	{
		GenerateThumbnails();
	}

	private void RestartCompanionCallback()
	{
		RestartCompanion();
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
