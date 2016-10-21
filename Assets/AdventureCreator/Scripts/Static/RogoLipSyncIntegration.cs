using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * A class the contains a number of static functions to assist with Rogo Digital LipSync integration.
	 * To use Rogo Digital LipSync with Adventure Creator, the 'RogoLipSyncIsPresent' preprocessor must be defined.
	 */
	public class RogoLipSyncIntegration : ScriptableObject
	{
		
		/**
		 * <summary>Checks if the 'RogoLipSyncIsPresent' preprocessor has been defined.</summary>
		 * <returns>True if the 'RogoLipSyncIsPresent' preprocessor has been defined</returns>
		 */
		public static bool IsDefinePresent ()
		{
			#if RogoLipSyncIsPresent
			return true;
			#else
			return false;
			#endif
		}


		public static void Play (Char speaker, string speakerName, int lineNumber, string language)
		{
			if (speaker == null)
			{
				return;
			}

			#if RogoLipSyncIsPresent
			if (lineNumber > -1 && speakerName != "" && KickStarter.speechManager.searchAudioFiles)
			{
				RogoDigital.Lipsync.LipSyncData lipSyncData = null;

				if (KickStarter.speechManager.autoNameSpeechFiles)
				{
					string filename = "Lipsync/";
					if (KickStarter.speechManager.placeAudioInSubfolders)
					{
						filename += speakerName + "/";
					}
					if (language != "" && KickStarter.speechManager.translateAudio)
					{
						// Not in original language
						filename += language + "/";
					}
					filename += speakerName + lineNumber;

					lipSyncData = Resources.Load (filename) as RogoDigital.Lipsync.LipSyncData;

					if (lipSyncData == null)
					{
						ACDebug.LogWarning ("No lipsync file found.  Looking for 'Resources/" + filename + "'");
					}
				}
				else
				{
					Object _object = KickStarter.speechManager.GetLineCustomLipsyncFile (lineNumber, Options.GetLanguage ());
					if (_object is RogoDigital.Lipsync.LipSyncData)
					{
						lipSyncData = (RogoDigital.Lipsync.LipSyncData) _object;
					}
				}

				if (lipSyncData != null)
				{
					RogoDigital.Lipsync.LipSync[] lipSyncs = speaker.GetComponentsInChildren <RogoDigital.Lipsync.LipSync>();
					if (lipSyncs != null && lipSyncs.Length > 0)
					{
						foreach (RogoDigital.Lipsync.LipSync lipSync in lipSyncs)
						{
							if (lipSync != null && lipSync.enabled)
							{
								lipSync.Play (lipSyncData);
							}
						}
					}
					else
					{
						ACDebug.LogWarning ("No LipSync component found on " + speaker.gameObject.name + " gameobject.");
					}
				}
				else
				{
					ACDebug.LogWarning ("No LipSync data found for " + speaker.gameObject.name + ", line ID " + lineNumber);
				}
			}
			#else
			ACDebug.LogError ("The 'RogoLipSyncIsPresent' preprocessor define must be declared in the Player Settings.");
			#endif
		}


		public static void Stop (Char speaker)
		{
			if (speaker == null)
			{
				return;
			}
			
			#if RogoLipSyncIsPresent
			RogoDigital.Lipsync.LipSync[] lipSyncs = speaker.GetComponentsInChildren <RogoDigital.Lipsync.LipSync>();
			if (lipSyncs != null && lipSyncs.Length > 0)
			{
				foreach (RogoDigital.Lipsync.LipSync lipSync in lipSyncs)
				{
					if (lipSync != null && lipSync.enabled)
					{
						lipSync.Stop (true);
					}
				}
			}
			#endif
		}
		
	}

}