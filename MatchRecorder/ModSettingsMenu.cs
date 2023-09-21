using DuckGame;
using MatchRecorderShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchRecorder
{
	internal class ModSettingsMenu
	{
		public event Action<ModSettings> GetOptions;
		public event Func<ModSettings> SetOptions;

		private UIMenu ModSettingsUIMenu { get; }

		public ModSettingsMenu()
		{

		}

		public void Initialize()
		{

		}
	}
}
