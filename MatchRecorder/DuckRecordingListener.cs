﻿namespace MatchRecorder
{
	/// <summary>
	/// A class used for the purpose of receiving duck game's recorder events
	/// used for either video or highlight recordings
	/// </summary>
	public class DuckRecordingListener : DuckGame.Recording
	{
		public DuckRecordingListener()
		{
		}

		//this is called from Prefix-Level.UpdateCurrentLevel() so it will have a blank frame here I think
		public void UpdateEvents()
		{
		}
	}
}