/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"Options.cs"
 * 
 *	This script provides a runtime instance of OptionsData,
 *	and has functions for saving and loading this data
 *	into the PlayerPrefs
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * Stores the local instances of OptionsData, and provides functions for saving and loading options and profiles to and from the PlayerPrefs.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_options.html")]
	#endif
	public class Options : MonoBehaviour
	{

		/** A local copy of the currently-active profile */
		public static OptionsData optionsData;

		/** The maximum number of profiles that can be created */
		public static int maxProfiles = 50;
		
		
		public void OnStart ()
		{
			LoadPrefs ();

			if (KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			AfterLoad ();
		}
		

		/**
		 * <summary>Saves the default options data (i.e. the values chosen in SettingsManager) to the default profile.</summary>
		 * <param name = "defaultOptionsData">An instance of OptionsData that represents default values</param>
		 */
		public static void SaveDefaultPrefs (OptionsData defaultOptionsData)
		{
			SavePrefsToID (0, defaultOptionsData, false);
		}
		

		/**
		 * <summary>Loads the OptionsData from the default profile.</summary>
		 * <returns>An instance of OptionsData used by the default profile</returns>
		 */
		public static OptionsData LoadDefaultPrefs ()
		{
			return LoadPrefsFromID (0, false, false);
		}
		

		/**
		 * Deletes the default profile.
		 */
		public static void DeleteDefaultProfile ()
		{
			DeleteProfilePrefs (0);
		}
		

		/**
		 * Saves the current options to the active profile.
		 */
		public static void SavePrefs ()
		{
			if (Application.isPlaying)
			{
				// Linked Variables
				GlobalVariables.DownloadAll ();
				optionsData.linkedVariables = SaveSystem.CreateVariablesData (KickStarter.runtimeVariables.globalVars, true, VariableLocation.Global);
			}
			
			SavePrefsToID (GetActiveProfileID (), null, true);
			
			if (Application.isPlaying)
			{
				KickStarter.options.CustomSaveOptionsHook ();
			}
		}
		

		/**
		 * <summary>Saves specific options to a specific profile.</summary>
		 * <param name = "ID">A unique identifier for the profile to save to</param>
		 * <param name = "_optionsData">An instance of OptionsData containing the options to save</param>
		 * <param name = "showLog">If True, the details of this save will be printed in the Console window</param>
		 */
		public static void SavePrefsToID (int ID, OptionsData _optionsData = null, bool showLog = false)
		{
			if (_optionsData == null)
			{
				_optionsData = Options.optionsData;
			}
			string optionsSerialized = Serializer.SerializeObject <OptionsData> (_optionsData, true);
			if (optionsSerialized != "")
			{
				PlayerPrefs.SetString (GetPrefKeyName (ID), optionsSerialized);
				if (showLog)
				{
					ACDebug.Log ("PlayerPrefs Key '" + GetPrefKeyName (ID) + "' saved");
				}
			}
		}
		

		/**
		 * Sets the options values to those stored within the active profile.
		 */
		public static void LoadPrefs ()
		{
			if (Application.isPlaying)
			{
				KickStarter.options.CustomLoadOptionsHook ();
			}

			optionsData = LoadPrefsFromID (GetActiveProfileID (), Application.isPlaying, true);
			int numLanguages = (Application.isPlaying) ? KickStarter.runtimeLanguages.Languages.Count : AdvGame.GetReferences ().speechManager.languages.Count;
			if (optionsData.language >= numLanguages)
			{
				if (numLanguages != 0)
				{
					ACDebug.LogWarning ("Language set to an invalid index - reverting to original language.");
				}
				optionsData.language = 0;
				SavePrefs ();
			}
			if (optionsData.language == 0 && KickStarter.speechManager && KickStarter.speechManager.ignoreOriginalText && KickStarter.speechManager.languages.Count > 1)
			{
				// Ignore original language
				optionsData.language = 1;
				SavePrefs ();
			}
			
			if (Application.isPlaying)
			{
				KickStarter.saveSystem.GatherSaveFiles ();
				KickStarter.playerMenus.RecalculateAll ();
			}
		}
		

		/**
		 * <summary>Gets the options values associated with a specific profile.</summary>
		 * <param name = "ID">A unique identifier for the profile to save to</param>
		 * <param name = "showLog">If True, the details of this save will be printed in the Console window</param>
		 * <param name = "doSave">If True, and if the profile had no OptionsData to read, then new values will be saved to it</param>
		 * <returns>An instance of OptionsData containing the profile's options</returns>
		 */
		public static OptionsData LoadPrefsFromID (int ID, bool showLog = false, bool doSave = true)
		{
			if (PlayerPrefs.HasKey (GetPrefKeyName (ID)))
			{
				string optionsSerialized = PlayerPrefs.GetString (GetPrefKeyName (ID));
				if (optionsSerialized != null && optionsSerialized.Length > 0)
				{
					if (showLog)
					{
						ACDebug.Log ("PlayerPrefs Key '" + GetPrefKeyName (ID) + "' loaded");
					}
					return Serializer.DeserializeOptionsData (optionsSerialized);
				}
			}
			
			// No data exists, so create new
			OptionsData _optionsData = new OptionsData (KickStarter.settingsManager.defaultLanguage, KickStarter.settingsManager.defaultShowSubtitles, KickStarter.settingsManager.defaultSfxVolume, KickStarter.settingsManager.defaultMusicVolume, KickStarter.settingsManager.defaultSpeechVolume, ID);
			if (doSave)
			{
				optionsData = _optionsData;
				SavePrefs ();
			}
			
			return _optionsData;
		}
		

		/**
		 * <summary>Switches to a specific profile, provided that it exists.</summary>
		 * <param name = "index">The index of profiles in a MenuProfilesList element that represents the profile to switch to</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that contains the profile to switch to also lists the active profile</param>
		 * <returns>True if the switch was successful</returns>
		 */
		public bool SwitchProfileIfExists (int index, bool includeActive)
		{
			if (KickStarter.settingsManager.useProfiles)
			{
				int ID = ProfileIndexToID (index, includeActive);
				if (PlayerPrefs.HasKey (GetPrefKeyName (ID)))
				{
					SwitchProfile (ID);
					return true;
				}
				ACDebug.Log ("Profile switch failed - " + index + " doesn't exist");
			}
			return false;
		}
		

		/**
		 * <summary>Converts a profile's index in a MenuProfilesList element to an ID number.</summary>
		 * <param name = "index">The index of profiles in a MenuProfilesList element that represents the profile to switch to</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that contains the profile to switch to also lists the active profile</param>
		 * <returns>The profile's unique identifier</returns>
		 */
		public int ProfileIndexToID (int index, bool includeActive = true)
		{
			for (int i=0; i<maxProfiles; i++)
			{
				if (PlayerPrefs.HasKey (GetPrefKeyName (i)))
				{
					if (!includeActive && i == GetActiveProfileID ())
					{}
					else
					{
						index --;
					}
				}
				
				if (index < 0)
				{
					return i;
				}
			}
			return -1;
		}
		

		/**
		 * <summary>Gets the ID number of the active profile.</summary>
		 * <returns>The active profile's unique identifier</returns>
		 */
		public static int GetActiveProfileID ()
		{
			if (KickStarter.settingsManager.useProfiles)
			{
				return PlayerPrefs.GetInt ("AC_ActiveProfile", 0);
			}
			return 0;
		}
		

		/**
		 * <summary>Sets the ID number of the active profile.</summary>
		 * <param name = "ID">A unique identifier for the profile</param>
		 */
		public static void SetActiveProfileID (int ID)
		{
			PlayerPrefs.SetInt ("AC_ActiveProfile", ID);
		}
		
		
		private int FindFirstEmptyProfileID ()
		{
			for (int i=0; i<maxProfiles; i++)
			{
				if (!PlayerPrefs.HasKey (GetPrefKeyName (i)))
				{
					return i;
				}
			}
			return 0;
		}
		

		/**
		 * <summary>Creates a new profile (instance of OptionsData).</summary>
		 * <param name = "_label">The name of the new profile.</param>
		 */
		public void CreateProfile (string _label = "")
		{
			int newProfileID = FindFirstEmptyProfileID ();
			
			OptionsData newOptionsData = new OptionsData (optionsData, newProfileID);
			if (_label != "")
			{
				newOptionsData.label = _label;
			}
			optionsData = newOptionsData;

			SetActiveProfileID (newProfileID);
			SavePrefs ();
				
			if (Application.isPlaying)
			{
				KickStarter.saveSystem.GatherSaveFiles ();
				KickStarter.playerMenus.RecalculateAll ();
			}
		}


		/**
		 * <summary>Renames a profile.</summary>
		 * <param name = "newProfileLabel">The new label for the profile</param>
		 * <param name = "profileIndex">The index in the MenuProfilesList element that represents the profile to delete. If it is set to its default, -2, the active profile will be deleted</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that the profile was selected from also displays the active profile</param>
		 */
		public void RenameProfile (string newProfileLabel, int profileIndex = -2, bool includeActive = true)
		{
			if (!KickStarter.settingsManager.useProfiles || newProfileLabel.Length == 0)
			{
				return;
			}
			
			int profileID = KickStarter.options.ProfileIndexToID (profileIndex, includeActive);
			if (profileID == -1)
			{
				ACDebug.LogWarning ("Invalid profile index: " + profileIndex + " - nothing to delete!");
				return;
			}
			else if (profileIndex == -2)
			{
				profileID = Options.GetActiveProfileID ();
			}

			if (profileID == GetActiveProfileID ())
			{
				optionsData.label = newProfileLabel;
				SavePrefs ();
			}
			else if (PlayerPrefs.HasKey (GetPrefKeyName (profileID)))
			{
				OptionsData tempOptionsData = LoadPrefsFromID (profileID, false);
				tempOptionsData.label = newProfileLabel;
				SavePrefsToID (profileID, tempOptionsData, true);
			}

			KickStarter.playerMenus.RecalculateAll ();
		}


		/**
		 * <summary>Gets the name of a specific profile.</summary>
		 * <param name = "index">The index in the MenuProfilesList element that represents the profile to delete.</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that the profile was selected from also displays the active profile</param>
		 * <returns>The display name of the profile</returns>
		 */
		public string GetProfileName (int index = -1, bool includeActive = true)
		{
			if (index == -1 || !KickStarter.settingsManager.useProfiles)
			{
				if (Options.optionsData == null)
				{
					LoadPrefs ();
				}
				return Options.optionsData.label;
			}

			int ID = KickStarter.options.ProfileIndexToID (index, includeActive);

			if (PlayerPrefs.HasKey (GetPrefKeyName (ID)))
			{
				OptionsData tempOptionsData = LoadPrefsFromID (ID, false);
				return tempOptionsData.label;
			}
			else
			{
				return "";
			}
		}
		

		/**
		 * <summary>Gets the number of profiles associated with the game.</summary>
		 * <returns>The number of profiles found</returns>
		 */
		public int GetNumProfiles ()
		{
			if (KickStarter.settingsManager.useProfiles)
			{
				int count = 0;
				for (int i=0; i<maxProfiles; i++)
				{
					if (PlayerPrefs.HasKey (GetPrefKeyName (i)))
					{
						count ++;
					}
				}
				//return count;
				return Mathf.Max (1, count);
			}
			return 1;
		}
		

		/**
		 * <summary>Deletes the PlayerPrefs key associated with a specfic profile</summary>
		 * <param name = "ID">The unique identifier of the profile to delete</param>
		 */
		public static void DeleteProfilePrefs (int ID)
		{
			bool isDeletingCurrentProfile = false;
			if (ID == GetActiveProfileID ())
			{
				isDeletingCurrentProfile = true;
			}

			ACDebug.Log ("PlayerPrefs Key '" + GetPrefKeyName (ID) + "' deleted");
			PlayerPrefs.DeleteKey (GetPrefKeyName (ID));
			
			if (isDeletingCurrentProfile)
			{
				for (int i=0; i<maxProfiles; i++)
				{
					if (PlayerPrefs.HasKey (GetPrefKeyName (i)))
					{
						SwitchProfile (i);
						return;
					}
				}
				
				// No other profile found, create new
				SwitchProfile (0);
			}
		}
		

		/**
		 * <summary>Switches to a specific profile.</summary>
		 * <param name = "ID">The unique identifier of the profile to switch to</param>
		 */
		public static void SwitchProfile (int ID)
		{
			SetActiveProfileID (ID);
			LoadPrefs ();

			ACDebug.Log ("Switched to profile " + ID.ToString () + ": '" + optionsData.label + "'");
			
			if (Application.isPlaying)
			{
				KickStarter.saveSystem.GatherSaveFiles ();
				KickStarter.playerMenus.RecalculateAll ();
			}
		}
		

		/**
		 * <summary>Gets the name of the PlayerPrefs key associated with a specific profile.</summary>
		 * <param name = "ID">The unique identifier of the profile to find</param>
		 * <returns>The name of the PlayerPrefs key associated with the profile</returns>
		 */
		public static string GetPrefKeyName (int ID)
		{
			string profileName = "Profile";
			if (AdvGame.GetReferences ().settingsManager != null && AdvGame.GetReferences ().settingsManager.saveFileName != "")
			{
				profileName = AdvGame.GetReferences ().settingsManager.saveFileName;
				profileName = profileName.Replace (" ", "_");
			}

			return ("AC_" + profileName + "_" + ID.ToString ());
		}


		/**
		 * <summary>Updates the labels of all save files by storing them in the profile's OptionsData.</summary>
		 * <param name = "foundSaveFiles">An array of SaveFile instances, that represent the found save game files found on disk</param>
		 */
		public static void UpdateSaveLabels (SaveFile[] foundSaveFiles)
		{
			System.Text.StringBuilder newSaveNameData = new System.Text.StringBuilder ();

			if (foundSaveFiles != null)
			{
				foreach (SaveFile saveFile in foundSaveFiles)
				{
					newSaveNameData.Append (saveFile.ID.ToString ());
					newSaveNameData.Append (SaveSystem.colon);
					newSaveNameData.Append (saveFile.GetSafeLabel ());
					newSaveNameData.Append (SaveSystem.pipe);
				}
				
				if (foundSaveFiles.Length > 0)
				{
					newSaveNameData.Remove (newSaveNameData.Length - 1, 1);
				}
			}

			optionsData.saveFileNames = newSaveNameData.ToString ();
			SavePrefs ();
		}
		
		
		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			if (KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			UpdateMixerVolumes ();

			SetVolume (SoundType.Music);
			SetVolume (SoundType.SFX);
			SetVolume (SoundType.Speech);
		}


		private void UpdateMixerVolumes ()
		{
			#if UNITY_5
			if (KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				if (optionsData == null)
				{
					LoadPrefs ();
				}
				AdvGame.SetMixerVolume (KickStarter.settingsManager.musicMixerGroup, KickStarter.settingsManager.musicAttentuationParameter, optionsData.musicVolume);
				AdvGame.SetMixerVolume (KickStarter.settingsManager.sfxMixerGroup, KickStarter.settingsManager.sfxAttentuationParameter, optionsData.sfxVolume);
				AdvGame.SetMixerVolume (KickStarter.settingsManager.speechMixerGroup, KickStarter.settingsManager.speechAttentuationParameter, optionsData.speechVolume);
			}
			#endif
		}
		

		/**
		 * <summary>Updates the volume of all Sound object of a specific SoundType to their correct values.</summary>
		 * <param name = "_soundType">The SoundType that matches the Sound objects to update (Music, SFX, Other)</param>
		 * <param name = "newVolume">If >= 0, the OptionsData will be updated as well</param>
		 */
		public void SetVolume (SoundType _soundType, float newVolume = -1f)
		{
			if (newVolume >= 0f)
			{
				if (Options.optionsData != null)
				{
					if (_soundType == SoundType.Music)
					{
						Options.optionsData.musicVolume = newVolume;
					}
					else if (_soundType == SoundType.SFX)
					{
						Options.optionsData.sfxVolume = newVolume;
					}
					else if (_soundType == SoundType.Speech)
					{
						Options.optionsData.speechVolume = newVolume;
					}

					Options.SavePrefs ();

					KickStarter.eventManager.Call_OnChangeVolume (_soundType, newVolume);
				}
				else
				{
					ACDebug.LogWarning ("Could not find Options data!");
				}
			}

			Sound[] soundObs = FindObjectsOfType (typeof (Sound)) as Sound[];
			foreach (Sound soundOb in soundObs)
			{
				if (soundOb.soundType == _soundType)
				{
					soundOb.AfterLoading ();
				}
			}
		}
		

		/**
		 * <summary>Changes the currently-selected language.</summary>
		 * <param name = "i">The language's index number in SpeechManager</param>
		 */
		public static void SetLanguage (int i)
		{
			if (Options.optionsData != null)
			{
				Options.optionsData.language = i;
				Options.SavePrefs ();

				KickStarter.eventManager.Call_OnChangeLanguage (i);
			}
			else
			{
				ACDebug.LogWarning ("Could not find Options data!");
			}
		}


		/**
		 * <summary>Changes the subtitle display setting.</summary>
		 * <param name = "showSubtitles">If True, subtitles will be shown</param>
		 */
		public static void SetSubtitles (bool showSubtitles)
		{
			if (Options.optionsData != null)
			{
				Options.optionsData.showSubtitles = showSubtitles;
				Options.SavePrefs ();

				KickStarter.eventManager.Call_OnChangeSubtitles (showSubtitles);
			}
			else
			{
				ACDebug.LogWarning ("Could not find Options data!");
			}
		}
		

		/**
		 * <summary>Gets the name of the currently-selected language.</summary>
		 * <returns>The name of the currently-selected language, as defined in SpeechManager</returns>
		 */
		public static string GetLanguageName ()
		{
			return KickStarter.runtimeLanguages.Languages [GetLanguage ()];
		}
		

		/**
		 * <summary>Gets the index number of the currently-selected language.</summary>
		 * <returns>The language's index number in SpeechManager</returns>
		 */
		public static int GetLanguage ()
		{
			if (Application.isPlaying && optionsData != null)
			{
				return optionsData.language;
			}
			return 0;
		}


		/**
		 * <summary>Gets the current value of the 'SFX volume'.</summary>
		 * <returns>The current value of the 'SFX volume', as defined in the current instance of OptionsData</returns>
		 */
		public static float GetSFXVolume ()
		{
			if (Application.isPlaying && optionsData != null)
			{
				return optionsData.sfxVolume;
			}
			return 1f;
		}
		
		
		private void CustomSaveOptionsHook ()
		{
			ISaveOptions[] saveOptionsHooks = GetSaveOptionsHooks (GetComponents (typeof (ISaveOptions)));
			if (saveOptionsHooks != null && saveOptionsHooks.Length > 0)
			{
				foreach (ISaveOptions saveOptionsHook in saveOptionsHooks)
				{
					saveOptionsHook.PreSaveOptions ();
				}
			}
		}
		
		
		private void CustomLoadOptionsHook ()
		{
			ISaveOptions[] saveOptionsHooks = GetSaveOptionsHooks (GetComponents (typeof (ISaveOptions)));
			if (saveOptionsHooks != null && saveOptionsHooks.Length > 0)
			{
				foreach (ISaveOptions saveOptionsHook in saveOptionsHooks)
				{
					saveOptionsHook.PostLoadOptions ();
				}
			}
		}
		
		
		private ISaveOptions[] GetSaveOptionsHooks (IList list)
		{
			ISaveOptions[] ret = new ISaveOptions[list.Count];
			list.CopyTo (ret, 0);
			return ret;
		}
		
	}
	
}