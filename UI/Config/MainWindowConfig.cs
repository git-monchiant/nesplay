using Avalonia;
using ReactiveUI.Fody.Helpers;
using System;

namespace Mesen.Config
{
	public class MainWindowConfig : BaseWindowConfig<MainWindowConfig>
	{
		public MainWindowConfig()
		{
			// Default to HD 16:9 window size (1280x720)
			WindowSize = new MesenSize { Width = 1280, Height = 720 };
		}
	}
}
