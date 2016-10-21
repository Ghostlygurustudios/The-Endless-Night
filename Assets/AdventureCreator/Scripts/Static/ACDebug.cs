using UnityEngine;
using System.Collections;

namespace AC
{

	public static class ACDebug
	{

		private const string hr = "\n\n-----------------------------";


		public static void Log (object message, UnityEngine.Object context = null)
		{
			if (CanDisplay (true))
			{
				Debug.Log (message + hr, context);
			}
		}


		public static void LogWarning (object message, UnityEngine.Object context = null)
		{
			if (CanDisplay ())
			{
				Debug.LogWarning (message + hr, context);
			}
		}


		public static void LogError (object message)
		{
			if (CanDisplay ())
			{
				Debug.LogError (message + hr);
			}
		}


		private static bool CanDisplay (bool isInfo = false)
		{
			if (KickStarter.settingsManager)
			{
				switch (KickStarter.settingsManager.showDebugLogs)
				{
				case ShowDebugLogs.Always :
					return true;

				case ShowDebugLogs.Never :
					return false;

				case ShowDebugLogs.OnlyWarningsOrErrors :
					if (!isInfo)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}
			return true;
		}

	}

}