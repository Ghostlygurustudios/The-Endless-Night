/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"SaveSystem.cs"
 * 
 *	This script processes saved game data to and from the scene objects.
 * 
 * 	It is partially based on Zumwalt's code here:
 * 	http://wiki.unity3d.com/index.php?title=Save_and_Load_from_XML
 *  and uses functions by Nitin Pande:
 *  http://www.eggheadcafe.com/articles/system.xml.xmlserialization.asp 
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace AC
{

	/**
	 * Processes save game data to and from scene objects.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_save_system.html")]
	#endif
	public class SaveSystem : MonoBehaviour
	{

		/** What type of load is being performed (No, InNewScene, InSameScene, JustSwitchingPlayer) */
		public LoadingGame loadingGame;
		/** A List of SaveFile variables, storing all available save files. */
		public List<SaveFile> foundSaveFiles = new List<SaveFile>();
		/** A List of SaveFile variables, storing all available import files. */
		public List<SaveFile> foundImportFiles = new List<SaveFile>();

		#if !UNITY_WEBPLAYER && !UNITY_WINRT && !UNITY_WII
		private string saveDirectory;
		#endif

		public const string pipe = "|";
		public const string colon = ":";

		private float gameplayInvokeTime = 0.01f;
		private SaveData saveData;
		private SelectiveLoad activeSelectiveLoad = new SelectiveLoad ();


		#if UNITY_5_4_OR_NEWER
		private void Awake ()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneLoaded;
		}
		private void OnDestroy ()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneLoaded;
		}
		private void SceneLoaded (UnityEngine.SceneManagement.Scene _scene, UnityEngine.SceneManagement.LoadSceneMode _loadSceneMode)
		{
			if (Time.time > 0f)
			{
				_OnLevelWasLoaded ();
			}
		}
		#else
		private void OnLevelWasLoaded ()
		{
			_OnLevelWasLoaded ();
		}
		#endif

		
		public void OnStart ()
		{
			#if !UNITY_WEBPLAYER && !UNITY_WINRT && !UNITY_WII
			saveDirectory = Application.persistentDataPath;
			#endif
			GatherSaveFiles ();
		}


		/**
		 * <summary>Sets the delay after loading a saved game before gameplay is resumed. This is useful in games with custom systems, e.g. weapons, where we want to prevent firing being possible immediately after loading.</summary>
		 * <param name = "The new delay time, in seconds</param>
		 */
		public void SetGameplayReturnTime (float _gameplayInvokeTime)
		{
			gameplayInvokeTime = _gameplayInvokeTime;
		}


		/**
		 * Searches the filesystem for all available save files, and stores them in foundSaveFiles.
		 */
		public void GatherSaveFiles ()
		{
			foundSaveFiles = new List<SaveFile>();

			for (int i=0; i<50; i++)
			{
				bool isAutoSave = false;

				#if UNITY_WEBPLAYER || UNITY_WINRT || UNITY_WII
			
				if (PlayerPrefs.HasKey (GetProjectName () + GetSaveIDFile (i)))
				{
					string label = "Save " + i.ToString ();
					if (i == 0)
					{
						label = "Autosave";
						isAutoSave = true;
					}
					foundSaveFiles.Add (new SaveFile (i, label, null, "", isAutoSave, 0));
				}
			
				#else
			
				string filename = saveDirectory + Path.DirectorySeparatorChar.ToString () + GetProjectName () + GetSaveIDFile (i) + GetSaveExtension ();
				if (File.Exists (filename))
				{
					int updateTime = 0;
					string label = "Save " + i.ToString ();
					if (i == 0)
					{
						label = "Autosave";
						isAutoSave = true;
					}

					if (KickStarter.settingsManager.saveTimeDisplay != SaveTimeDisplay.None)
					{
						DirectoryInfo dir = new DirectoryInfo (saveDirectory);
						FileInfo[] info = dir.GetFiles (GetProjectName () + GetSaveIDFile (i) + GetSaveExtension ());

						if (info != null && info.Length > 0)
						{
							if (!isAutoSave)
							{
								System.TimeSpan t = info[0].LastWriteTime - new System.DateTime (2015, 1, 1);
								updateTime = (int) t.TotalSeconds;
							}

							string creationTime = info[0].LastWriteTime.ToShortDateString ();
							if (KickStarter.settingsManager.saveTimeDisplay == SaveTimeDisplay.TimeAndDate)
							{
								creationTime += " " + System.DateTime.Now.ToShortTimeString ();
							}

							label += " (" + creationTime + ")";
						}
					}

					Texture2D screenShot = null;
					if (KickStarter.settingsManager.takeSaveScreenshots)
					{
						screenShot = Serializer.LoadScreenshot (GetSaveScreenshotName (i));
					}

					foundSaveFiles.Add (new SaveFile (i, label, screenShot, filename, isAutoSave, updateTime));
				}

				#endif
			}

			if (KickStarter.settingsManager.orderSavesByUpdateTime)
			{
				foundSaveFiles.Sort (delegate (SaveFile a, SaveFile b) {return a.updatedTime.CompareTo (b.updatedTime);});
			}

			// Now get save file labels
			if (Options.optionsData.saveFileNames != "")
			{
				string[] profilesArray = Options.optionsData.saveFileNames.Split (SaveSystem.pipe[0]);
				foreach (string chunk in profilesArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _id = 0;
					int.TryParse (chunkData[0], out _id);
					string _label = chunkData[1];

					for (int i=0; i<Mathf.Min (50, foundSaveFiles.Count); i++)
					{
						if (foundSaveFiles[i].ID == _id)
						{
							SaveFile newSaveFile = new SaveFile (foundSaveFiles [i]);
							newSaveFile.SetLabel (_label);
							foundSaveFiles[i] = newSaveFile;
						}
					}
				}
			}
		}


		/**
		 * <summary>Returns the default label of a save file.</summary>
		 * <param name = "saveID">The ID number of the save game</param>
		 * <returns>The save file's default label</returns>
		 */
		public string GetDefaultSaveLabel (int saveID)
		{
			string label = "Save " + saveID.ToString ();
			if (saveID == 0)
			{
				label = "Autosave";
			}
		
			#if !UNITY_WEBPLAYER && !UNITY_ANDROID && !UNITY_WINRT && !UNITY_WII

			if (KickStarter.settingsManager.saveTimeDisplay != SaveTimeDisplay.None)
			{
				string creationTime = System.DateTime.Now.ToShortDateString ();
				if (KickStarter.settingsManager.saveTimeDisplay == SaveTimeDisplay.TimeAndDate)
				{
					creationTime += " " + System.DateTime.Now.ToShortTimeString ();
				}
				label += " (" + creationTime + ")";
			}

			#endif

			return label;
		}


		#if UNITY_STANDALONE

		private string GetImportDirectory (string importProjectName)
		{
			string[] s = Application.persistentDataPath.Split ('/');
			string currentProjectName = s[s.Length - 1];
			string importDirectory = saveDirectory.Replace (currentProjectName, importProjectName);
			return importDirectory;
		}

		#endif


		/**
		 * <summary>Searches the filesystem for all available import files, and stores them in foundImportFiles.</summary>
		 * <param name = "projectName">The project name of the game whose save files we're looking to import</param>
		 * <param name = "saveFilename">The "save filename" of the game whose save files we're looking to import</param>
		 * <param name = "boolID">If >= 0, the ID of the boolean Global Variable that must be True for the file to be considered valid for import</param>
		 */
		public void GatherImportFiles (string projectName, string saveFilename, int boolID)
		{
			#if !UNITY_STANDALONE
			ACDebug.LogWarning ("Cannot import save files unless running on Windows, Mac or Linux standalone platforms.");
			return;
			#else

			foundImportFiles = new List<SaveFile>();

			if (projectName == "" || saveFilename == "")
			{
				return;
			}
			string importDirectory = GetImportDirectory (projectName);
			SettingsManager settingsManager = KickStarter.settingsManager;
			
			for (int i=0; i<50; i++)
			{
				string filename = importDirectory + Path.DirectorySeparatorChar.ToString () + saveFilename + GetSaveIDFile (i) + GetSaveExtension ();
				if (File.Exists (filename))
				{
					if (boolID >= 0 && !DoImportCheck (filename, boolID))
					{
						continue;
					}

					bool isAutoSave = false;
					string label = "Import " + i.ToString ();
					if (i == 0)
					{
						label = "Autosave";
						isAutoSave = true;
					}

					if (settingsManager.saveTimeDisplay != SaveTimeDisplay.None)
					{
						DirectoryInfo dir = new DirectoryInfo (importDirectory);
						FileInfo[] info = dir.GetFiles (saveFilename + GetSaveIDFile (i) + GetSaveExtension ());
						
						string creationTime = info [0].LastWriteTime.ToString ();
						if (settingsManager.saveTimeDisplay == SaveTimeDisplay.DateOnly)
						{
							creationTime = creationTime.Substring (0, creationTime.IndexOf (" "));
						}
						
						label += " (" + creationTime + ")";
					}
					
					Texture2D screenShot = null;
					if (settingsManager.takeSaveScreenshots)
					{
						screenShot = Serializer.LoadScreenshot (GetImportScreenshotName (i, importDirectory, saveFilename));
					}
				
					foundImportFiles.Add (new SaveFile (i, label, screenShot, filename, isAutoSave));
				}
			}
			#endif
		}


		private bool DoImportCheck (string filename, int boolID)
		{
			string allData = Serializer.LoadSaveFile (filename, false);
			if (allData.ToString () != "")
			{
				int divider = allData.IndexOf ("||");
				string mainData = allData.Substring (0, divider);

				SaveData tempSaveData = (SaveData) Serializer.DeserializeObject <SaveData> (mainData);
				if (tempSaveData == null)
				{
					tempSaveData = new SaveData ();
				}

				string varData = tempSaveData.mainData.runtimeVariablesData;
				if (varData.Length > 0)
				{
					string[] varsArray = varData.Split (SaveSystem.pipe[0]);
					
					foreach (string chunk in varsArray)
					{
						string[] chunkData = chunk.Split (SaveSystem.colon[0]);
						
						int _id = 0;
						int.TryParse (chunkData[0], out _id);

						if (_id == boolID)
						{
							int _value = 0;
							int.TryParse (chunkData[1], out _value);

							if (_value == 1)
							{
								return true;
							}
							return false;
						}
					}
				}
			}
			return false;
		}


		private IEnumerator TakeScreenshot (string fileName)
		{
			KickStarter.stateHandler.PreScreenshotBackup ();

			yield return new WaitForEndOfFrame ();
			
			Texture2D screenshotTex = new Texture2D (Screen.width, Screen.height, TextureFormat.RGB24, false);
			
			screenshotTex.ReadPixels (new Rect (0f, 0f, Screen.width, Screen.height), 0, 0);
			screenshotTex.Apply ();

			Serializer.SaveScreenshot (screenshotTex, fileName);
			Destroy (screenshotTex);
			
			KickStarter.stateHandler.PostScreenshotBackup ();
			GatherSaveFiles ();
		}


		private string GetSaveExtension ()
		{
			if (GetSaveMethod () == SaveMethod.XML)
			{
				return ".savx";
			}
			else if (GetSaveMethod () == SaveMethod.Json)
			{
				return ".savj";
			}
			return ".save";
		}


		/**
		 * <summary>Gets the SaveMethod for the current platform.  This is XML for iPhone, Windows Phone and Wii platforms, and Binary for all others</summary>
		 * <returns>The SaveMethod (XML, Binary) for the current platform</returns>
		 */
		public static SaveMethod GetSaveMethod ()
		{
			if (UnityVersionHandler.CanUseJson () && KickStarter.settingsManager.useJsonSerialization)
			{
				return SaveMethod.Json;
			}

			#if UNITY_IPHONE || UNITY_WP8 || UNITY_WINRT || UNITY_WII
			return SaveMethod.XML;
			#else
			return SaveMethod.Binary;
			#endif
		}


		/**
		 * <summary>Checks if an import file with a particular ID number exists.</summary>
		 * <param name = "saveID">The import ID to check for</param>
		 * <returns>True if an import file with a matching ID number exists</returns>
		 */
		public static bool DoesImportExist (int saveID)
		{
			if (KickStarter.saveSystem)
			{
				foreach (SaveFile file in KickStarter.saveSystem.foundImportFiles)
				{
					if (file.ID == saveID)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Checks if a save file with a particular ID number exists</summary>
		 * <param name = "saveID">The save ID to check for</param>
		 * <returns>True if a save file with a matching ID number exists</returns>
		 */
		public static bool DoesSaveExist (int saveID)
		{
			if (KickStarter.saveSystem)
			{
				foreach (SaveFile file in KickStarter.saveSystem.foundSaveFiles)
				{
					if (file.ID == saveID)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * Loads the AutoSave save file.  If multiple save profiles are used, the current profiles AutoSave will be loaded.
		 */
		public static void LoadAutoSave ()
		{
			if (KickStarter.saveSystem)
			{
				if (File.Exists (KickStarter.saveSystem.GetSaveFileName (0)))
				{
					KickStarter.saveSystem.LoadSaveGame (0);
				}
				else
				{
					ACDebug.LogWarning ("Could not load game: file " + KickStarter.saveSystem.GetSaveFileName (0) + " does not exist.");
				}
			}
		}


		/**
		 * <summary>Imports a save file from another Adventure Creator game.</summary>
		 * <param name = "elementSlot">The slot index of the MenuProfilesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to import</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to import</param>
		 * <returns>True if the import was successful</returns>
		 */
		public static bool ImportGame (int elementSlot, int saveID, bool useSaveID)
		{
			if (KickStarter.saveSystem)
			{
				if (!useSaveID)
				{
					if (KickStarter.saveSystem.foundImportFiles.Count > elementSlot)
					{
						saveID = KickStarter.saveSystem.foundImportFiles[elementSlot].ID;
					}
				}
				
				if (saveID >= 0)
				{
					KickStarter.saveSystem.ImportSaveGame (saveID);
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Sets the local instance of SelectiveLoad, which determines which save data is restored the next time (and only the next time) LoadGame is called.</summary>
		 * <param name = "selectiveLoad">An instance of SelectiveLoad the defines what elements to load</param>
		 */
		public void SetSelectiveLoadOptions (SelectiveLoad selectiveLoad)
		{
			activeSelectiveLoad = selectiveLoad;
		}


		/**
		 * Loads the last-recorded save game file.
		 */
		public static void ContinueGame ()
		{
			if (Options.optionsData != null && Options.optionsData.lastSaveID >= 0)
			{
				KickStarter.saveSystem.LoadSaveGame (Options.optionsData.lastSaveID);
			}
		}


		/**
		 * <summary>Loads a save game file.</summary>
		 * <param name = "saveID">The save ID of the file to load</param>
		 * <returns>True if the load was successful</returns>
		 */
		public static bool LoadGame (int saveID)
		{
			return LoadGame (0, saveID, true);
		}


		/**
		 * <summary>Loads a save game file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to load</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to load</param>
		 * <returns>True if the load was successful</returns>
		 */
		public static bool LoadGame (int elementSlot, int saveID, bool useSaveID)
		{
			if (KickStarter.saveSystem)
			{
				if (!useSaveID)
				{
					saveID = KickStarter.saveSystem.foundSaveFiles[elementSlot].ID;
				}
				
				if (saveID == -1)
				{
					ACDebug.LogWarning ("Could not load game: file " + KickStarter.saveSystem.GetSaveFileName (saveID) + " does not exist.");
				}
				else
				{
					KickStarter.saveSystem.LoadSaveGame (saveID);
					return true;
				}
			}
			return false;
		}
		

		/**
		 * Clears all save data stored in the SaveData class.
		 */
		public void ClearAllData ()
		{
			saveData = new SaveData ();
		}


		/**
		 * <summary>Imports a save file from another Adventure Creator game, once found to exist.</summary>
		 * <param name = "saveID">The ID number of the save file to import</param>
		 */
		public void ImportSaveGame (int saveID)
		{
			string allData = "";

			foreach (SaveFile saveFile in foundImportFiles)
			{
				if (saveFile.ID == saveID)
				{
					allData = Serializer.LoadSaveFile (saveFile.fileName, true);
				}
			}

			if (allData.ToString () != "")
			{
				KickStarter.eventManager.Call_OnImport (FileAccessState.Before);

				int divider = allData.IndexOf ("||");
				string mainData = allData.Substring (0, divider);

				saveData = (SaveData) Serializer.DeserializeObject <SaveData> (mainData);
				
				// Stop any current-running ActionLists, dialogs and interactions
				KillActionLists ();
				SaveSystem.AssignVariables (saveData.mainData.runtimeVariablesData);

				KickStarter.eventManager.Call_OnImport (FileAccessState.After);
			}
			else
			{
				KickStarter.eventManager.Call_OnImport (FileAccessState.Fail);
			}
		}


		/**
		 * <summary>Loads a save game, once found to exist.</summary>
		 * <param name = "saveID">The save ID of the file to load</param>
		 */
		public void LoadSaveGame (int saveID)
		{
			string allData = Serializer.LoadSaveFile (GetSaveFileName (saveID), true);

			if (allData.ToString () != "")
			{
				KickStarter.eventManager.Call_OnLoad (FileAccessState.Before);

				int divider = allData.IndexOf ("||");
				string mainData = allData.Substring (0, divider);
				string roomData = allData.Substring (divider + 2);

				if (activeSelectiveLoad.loadSceneObjects)
				{
					saveData = (SaveData) Serializer.DeserializeObject <SaveData> (mainData);
					KickStarter.levelStorage.allLevelData = Serializer.DeserializeAllRoomData (roomData);
				}

				// Stop any current-running ActionLists, dialogs and interactions
				KillActionLists ();
				
				// If player has changed, destroy the old one and load in the new one
				if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					if ((KickStarter.player == null && saveData.mainData.currentPlayerID != KickStarter.settingsManager.GetEmptyPlayerID ()) ||
						(KickStarter.player != null && KickStarter.player.ID != saveData.mainData.currentPlayerID))
					{
						KickStarter.ResetPlayer (GetPlayerByID (saveData.mainData.currentPlayerID), saveData.mainData.currentPlayerID, true, Quaternion.identity, false, true);
					}
				}

				int newScene = GetPlayerScene (saveData.mainData.currentPlayerID, saveData.playerData);
				
				// Load correct scene
				bool forceReload = KickStarter.settingsManager.reloadSceneWhenLoading;
				if (forceReload || (newScene != UnityVersionHandler.GetCurrentSceneNumber () && activeSelectiveLoad.loadScene))
				{
					loadingGame = LoadingGame.InNewScene;
					KickStarter.sceneChanger.ChangeScene (new SceneInfo ("", newScene), false, forceReload);
				}
				else
				{
					loadingGame = LoadingGame.InSameScene;

					// Already in the scene
					Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
					foreach (Sound sound in sounds)
					{
						if (sound.GetComponent <AudioSource>())
						{
							if (sound.soundType != SoundType.Music && !sound.GetComponent <AudioSource>().loop)
							{
								sound.Stop ();
							}
						}
					}

					_OnLevelWasLoaded ();
				}
			}
			else
			{
				KickStarter.eventManager.Call_OnLoad (FileAccessState.Fail);
			}
		}


		private Player GetPlayerByID (int id)
		{
			SettingsManager settingsManager = KickStarter.settingsManager;

			foreach (PlayerPrefab playerPrefab in settingsManager.players)
			{
				if (playerPrefab.ID == id)
				{
					if (playerPrefab.playerOb)
					{
						return playerPrefab.playerOb;
					}

					return null;
				}
			}

			return null;
		}


		private int GetPlayerScene (int playerID, List<PlayerData> _playerData)
		{
			SettingsManager settingsManager = KickStarter.settingsManager;
			if (settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				if (_playerData.Count > 0)
				{
					return _playerData[0].currentScene;
				}
			}
			else
			{
				foreach (PlayerData _data in _playerData)
				{
					if (_data.playerID == playerID)
					{
						return (_data.currentScene);
					}
				}
			}

			return UnityVersionHandler.GetCurrentSceneNumber ();
		}


		private string GetPlayerSceneName (int playerID, List<PlayerData> _playerData)
		{
			SettingsManager settingsManager = KickStarter.settingsManager;
			if (settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				if (_playerData.Count > 0)
				{
					return _playerData[0].currentSceneName;
				}
			}
			else
			{
				foreach (PlayerData _data in _playerData)
				{
					if (_data.playerID == playerID)
					{
						return (_data.currentSceneName);
					}
				}
			}
			
			return UnityVersionHandler.GetCurrentSceneName ();
		}


		private void _OnLevelWasLoaded ()
		{
			KickStarter.stateHandler.AfterLoad ();

			if (KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			if (KickStarter.sceneSettings == null)
			{
				return;
			}

			ResetSceneObjects ();
			if (loadingGame == LoadingGame.InNewScene || loadingGame == LoadingGame.InSameScene)
			{
				if (KickStarter.dialog)
				{
					KickStarter.dialog.KillDialog (true, true);
				}
				
				if (KickStarter.playerInteraction)
				{
					KickStarter.playerInteraction.StopMovingToHotspot ();
				}

				ReturnMainData ();
				KickStarter.levelStorage.ReturnCurrentLevelData (true);
				CustomLoadHook ();
				KickStarter.eventManager.Call_OnLoad (FileAccessState.After);

				/*if (loadingGame == LoadingGame.InSameScene)
				{
					loadingGame = LoadingGame.No;
				}*/
			}
					
			if (KickStarter.runtimeInventory)
		    {
				KickStarter.runtimeInventory.RemoveRecipes ();
			}


			if (loadingGame == LoadingGame.JustSwitchingPlayer)
			{
				foreach (PlayerData _data in saveData.playerData)
				{
					if (_data.playerID == KickStarter.player.ID)
					{
						ReturnCameraData (_data);
						KickStarter.playerInput.LoadPlayerData (_data);
						KickStarter.sceneChanger.LoadPlayerData (_data);
						break;
					}
				}

				KickStarter.sceneSettings.UnpauseGame (KickStarter.playerInput.timeScale);//
				KickStarter.stateHandler.gameState = GameState.Cutscene;
				KickStarter.mainCamera.FadeIn (0.5f);

				Invoke ("ReturnToGameplay", gameplayInvokeTime);
			}
			else
			{
				activeSelectiveLoad = new SelectiveLoad ();
			}

			AssetLoader.UnloadAssets ();
		}


		/**
		 * <summary>Create a new save game file.</summary>
		 * <param name = "overwriteLabel">True if the label should be updated</param>
		 * <param name = "newLabel">The new label, if it can be set</param>
		 * <returns>True if the save is successful</returns>
		 */
		public static bool SaveNewGame (bool overwriteLabel = true, string newLabel = "")
		{
			if (KickStarter.saveSystem)
			{
				return KickStarter.saveSystem.SaveNewSaveGame (overwriteLabel, newLabel);
			}
			return false;
		}
		

		/**
		 * <summary>Create a new save game file.</summary>
		 * <param name = "overwriteLabel">True if the label should be updated</param>
		 * <param name = "newLabel">The new label, if it can be set</param>
		 * <returns>True if the save is successful</returns>
		 */
		public bool SaveNewSaveGame (bool overwriteLabel = true, string newLabel = "")
		{
			if (foundSaveFiles != null && foundSaveFiles.Count > 0)
			{
				int expectedID = -1;

				for (int i=0; i<foundSaveFiles.Count; i++)
				{
					if (expectedID != -1 && expectedID != foundSaveFiles[i].ID)
					{
						return SaveSaveGame (expectedID, overwriteLabel, newLabel);
					}

					expectedID = foundSaveFiles[i].ID + 1;
				}

				// Saves present, but no gap
				int newSaveID = (foundSaveFiles [foundSaveFiles.Count-1].ID+1);
				return SaveSaveGame (newSaveID, overwriteLabel, newLabel);
			}

			return SaveSaveGame (1, overwriteLabel, newLabel);
		}


		/**
		 * <summary>Overwrites the AutoSave file.</summary>
		 * <returns>True if the save is successful</returns>
		 */
		public static bool SaveAutoSave ()
		{
			if (KickStarter.saveSystem)
			{
				return KickStarter.saveSystem.SaveSaveGame (0);
			}
			return false;
		}


		/**
		 * <summary>Saves the game.</summary>
		 * <param name = "saveID">The save ID to save</param>
		 * <param name = "overwriteLabel">True if the label should be updated</param>
		 * <param name = "newLabel">The new label, if it can be set. If blank, a default label will be generated.</param>
		 * <returns>True if the save was successful</returns>
		 */
		public static bool SaveGame (int saveID, bool overwriteLabel = true, string newLabel = "")
		{
			return SaveSystem.SaveGame (0, saveID, true, overwriteLabel, newLabel);
		}
		

		/**
		 * <summary>Saves the game.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to save</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to save</param>
		 * <param name = "overwriteLabel">True if the label should be updated</param>
		 * <param name = "newLabel">The new label, if it can be set. If blank, a default label will be generated.</param>
		 * <returns>True if the save was successful</returns>
		 */
		public static bool SaveGame (int elementSlot, int saveID, bool useSaveID, bool overwriteLabel = true, string newLabel = "")
		{
			if (KickStarter.saveSystem)
			{
				if (!useSaveID)
				{
					if (KickStarter.saveSystem.foundSaveFiles.Count > elementSlot)
					{
						saveID = KickStarter.saveSystem.foundSaveFiles[elementSlot].ID;
					}
					else
					{
						saveID = -1;
					}
				}

				if (saveID == -1)
				{
					return SaveSystem.SaveNewGame (overwriteLabel, newLabel);
				}

				return KickStarter.saveSystem.SaveSaveGame (saveID, overwriteLabel, newLabel);
			}
			return false;
		}


		/**
		 * <summary>Saves the game, once found to exist.</summary>
		 * <param name = "saveID">The save ID to save</param>
		 * <param name = "overwriteLabel">True if the label should be updated</param>
		 * <param name = "newLabel">The new label, if it can be set. If blank, a default label will be generated.</param>
		 * <returns>True if the save was successful</returns>
		 */
		public bool SaveSaveGame (int saveID, bool overwriteLabel = true, string newLabel = "")
		{
			if (GetNumSaves () >= KickStarter.settingsManager.maxSaves && !DoesSaveExist (saveID))
			{
				ACDebug.LogWarning ("Cannot save - maximum number of save files has already been reached.");
				KickStarter.eventManager.Call_OnSave (FileAccessState.Fail);
				return false;
			}

			KickStarter.eventManager.Call_OnSave (FileAccessState.Before);
			CustomSaveHook ();
			KickStarter.levelStorage.StoreAllOpenLevelData ();
			
			Player player = KickStarter.player;

			if (KickStarter.playerInput && KickStarter.runtimeInventory && KickStarter.sceneChanger && KickStarter.settingsManager && KickStarter.stateHandler)
			{
				if (saveData != null && saveData.playerData != null && saveData.playerData.Count > 0)
				{
					foreach (PlayerData _data in saveData.playerData)
					{
						if (player != null && _data.playerID == player.ID)
						{
							saveData.playerData.Remove (_data);
							break;
						}
						else if (player == null && _data.playerID == KickStarter.settingsManager.GetEmptyPlayerID ())
						{
							saveData.playerData.Remove (_data);
							break;
						}
					}
				}
				else
				{
					saveData = new SaveData ();
					saveData.mainData = new MainData ();
					saveData.playerData = new List<PlayerData>();
				}

				PlayerData playerData = SavePlayerData (player);
				saveData.playerData.Add (playerData);

				// Main data
				saveData.mainData = KickStarter.stateHandler.SaveMainData (saveData.mainData);
				saveData.mainData.movementMethod = (int) KickStarter.settingsManager.movementMethod;

				if (player != null)
				{
					saveData.mainData.currentPlayerID = player.ID;
				}
				else
				{
					saveData.mainData.currentPlayerID = KickStarter.settingsManager.GetEmptyPlayerID ();
				}

				saveData.mainData = KickStarter.playerInput.SaveMainData (saveData.mainData);
				saveData.mainData = KickStarter.runtimeInventory.SaveMainData (saveData.mainData);
				saveData.mainData = KickStarter.runtimeVariables.SaveMainData (saveData.mainData);
				saveData.mainData = KickStarter.playerMenus.SaveMainData (saveData.mainData);
				saveData.mainData.activeAssetLists = KickStarter.actionListAssetManager.GetSaveData ();

				string mainData = Serializer.SerializeObject <SaveData> (saveData, true);
				string levelData = Serializer.SerializeAllRoomData (KickStarter.levelStorage.allLevelData);

				string allData = mainData + "||" + levelData;
		
				Serializer.CreateSaveFile (GetSaveFileName (saveID), allData);

				// Update label
				if (overwriteLabel)
				{
					GatherSaveFiles ();
					for (int i=0; i<Mathf.Min (50, foundSaveFiles.Count); i++)
					{
						if (foundSaveFiles[i].ID == saveID)
						{
							SaveFile newSaveFile = new SaveFile (foundSaveFiles [i]);
							if (newLabel.Length > 0)
							{
								newSaveFile.SetLabel (newLabel);
							}
							else
							{
								newSaveFile.SetLabel (GetDefaultSaveLabel (saveID));
							}
							foundSaveFiles[i] = newSaveFile;
							break;
						}
					}
				}

				// Update PlayerPrefs
				Options.optionsData.lastSaveID = saveID;
				Options.UpdateSaveLabels (foundSaveFiles.ToArray ());

				#if !UNITY_WEBPLAYER && !UNITY_WINRT && !UNITY_WII
				if (KickStarter.settingsManager.takeSaveScreenshots)
				{
					StartCoroutine ("TakeScreenshot", GetSaveScreenshotName (saveID));
				}
				else
				{
					GatherSaveFiles ();
				}
				#else
				GatherSaveFiles ();
				#endif
			}
			else
			{
				if (KickStarter.playerInput == null)
				{
					ACDebug.LogWarning ("Save failed - no PlayerInput found.");
				}
				if (KickStarter.runtimeInventory == null)
				{
					ACDebug.LogWarning ("Save failed - no RuntimeInventory found.");
				}
				if (KickStarter.sceneChanger == null)
				{
					ACDebug.LogWarning ("Save failed - no SceneChanger found.");
				}
				if (KickStarter.settingsManager == null)
				{
					ACDebug.LogWarning ("Save failed - no Settings Manager found.");
				}
			}

			KickStarter.eventManager.Call_OnSave (FileAccessState.After);
			return true;
		}


		/**
		 * Stores the PlayerData of the active Player.
		 */
		public void SaveCurrentPlayerData ()
		{
			if (saveData != null && saveData.playerData != null && saveData.playerData.Count > 0)
			{
				foreach (PlayerData _data in saveData.playerData)
				{
					if ((KickStarter.player != null && _data.playerID == KickStarter.player.ID) ||
						(KickStarter.player == null && _data.playerID == KickStarter.settingsManager.GetEmptyPlayerID ()))
					{
						saveData.playerData.Remove (_data);
						break;
					}
				}
			}
			else
			{
				saveData = new SaveData ();
				saveData.mainData = new MainData ();
				saveData.playerData = new List<PlayerData>();
			}
			
			PlayerData playerData = SavePlayerData (KickStarter.player);
			saveData.playerData.Add (playerData);
		}


		private PlayerData SavePlayerData (Player player)
		{
			PlayerData playerData = new PlayerData ();

			playerData.currentScene = UnityVersionHandler.GetCurrentSceneNumber ();
			playerData.currentSceneName = UnityVersionHandler.GetCurrentSceneName ();

			playerData = KickStarter.sceneChanger.SavePlayerData (playerData);
			playerData = KickStarter.playerInput.SavePlayerData (playerData);

			KickStarter.runtimeInventory.RemoveRecipes ();
			playerData.inventoryData = CreateInventoryData (KickStarter.runtimeInventory.localItems);

			// Camera
			MainCamera mainCamera = KickStarter.mainCamera;
			playerData = mainCamera.SaveData (playerData);
           
			if (player == null)
			{
				playerData.playerPortraitGraphic = "";
				playerData.playerID = KickStarter.settingsManager.GetEmptyPlayerID ();
				return playerData;
			}
			
			playerData = player.SavePlayerData (playerData);

			return playerData;
		}


		/**
		 * <summary>Gets the number of found import files.</summary>
		 * <returns>The number of found import files</returns>
		 */
		public static int GetNumImportSlots ()
		{
			return KickStarter.saveSystem.foundImportFiles.Count;
		}


		/**
		 * <summary>Gets the number of found save files.</summary>
		 * <returns>The number of found save files</returns>
		 */
		public static int GetNumSlots ()
		{
			return KickStarter.saveSystem.foundSaveFiles.Count;
		}


		private string GetProjectName ()
		{
			SettingsManager settingsManager = KickStarter.settingsManager;
			if (settingsManager)
			{
				if (settingsManager.saveFileName == "")
				{
					settingsManager.saveFileName = SetProjectName ();
				}
				
				if (settingsManager.saveFileName != "")
				{
					return settingsManager.saveFileName;
				}
			}
			
			return SetProjectName ();
		}
		
		
		/**
		 * <summary>Generates a default name for the project, based on the project's folder name.</summary>
		 * <returns>The name of the project's folder</returns>
		 */
		public static string SetProjectName ()
		{
			string[] s = Application.dataPath.Split ('/');
			string projectName = s[s.Length - 2];
			return projectName;
		}


		private string GetSaveIDFile (int saveID, int profileID = -1)
		{
			if (KickStarter.settingsManager.useProfiles)
			{
				if (profileID == -1)
				{
					// None set, so just use the active profile
					profileID = Options.GetActiveProfileID ();
				}
				return ("_" + saveID.ToString () + "_" + profileID.ToString ());
			}
			return ("_" + saveID.ToString ());
		}


		private string GetSaveFileName (int saveID, int profileID = -1)
		{
			string fileName = "";

			#if UNITY_WEBPLAYER || UNITY_WINRT || UNITY_WII
			fileName = GetProjectName () + GetSaveIDFile (saveID);
			#else
			fileName = saveDirectory + Path.DirectorySeparatorChar.ToString () + GetProjectName () + GetSaveIDFile (saveID, profileID) + GetSaveExtension ();
			#endif

			return (fileName);
		}


		private string GetImportScreenshotName (int saveID, string importDirectory, string saveFileName)
		{
			return (importDirectory + Path.DirectorySeparatorChar.ToString () + saveFileName + GetSaveIDFile (saveID) + ".jpg");
		}


		private string GetSaveScreenshotName (int saveID, int profileID = -1)
		{
			string fileName = "";
			
			#if UNITY_WEBPLAYER || UNITY_WINRT || UNITY_WII
			fileName = GetProjectName () + GetSaveIDFile (saveID);
			#else
			fileName = saveDirectory + Path.DirectorySeparatorChar.ToString () + GetProjectName () + GetSaveIDFile (saveID, profileID) + ".jpg";
			#endif
			
			return (fileName);
		}
		
		
		private void KillActionLists ()
		{
			KickStarter.actionListManager.KillAllLists ();

			Moveable[] moveables = FindObjectsOfType (typeof (Moveable)) as Moveable[];
			foreach (Moveable moveable in moveables)
			{
				moveable.StopMoving ();
			}
		}


		/**
		 * <summary>Gets the label of an import file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuProfilesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to import</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to import</param>
		 * <returns>The label of the import file.</returns>
		 */
		public static string GetImportSlotLabel (int elementSlot, int saveID, bool useSaveID)
		{
			if (Application.isPlaying && KickStarter.saveSystem.foundImportFiles != null)
			{
				return KickStarter.saveSystem.GetSlotLabel (elementSlot, saveID, useSaveID, KickStarter.saveSystem.foundImportFiles.ToArray ());
			}
			return ("Save test (01/01/2001 12:00:00)"); 
		}


		/**
		 * <summary>Gets the label of a save file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to save</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to save</param>
		 * <returns>The label of the save file.  If the save file is not found, an empty string is returned.</returns>
		 */
		public static string GetSaveSlotLabel (int elementSlot, int saveID, bool useSaveID)
		{
			if (Application.isPlaying && KickStarter.saveSystem.foundSaveFiles != null)
			{
				return KickStarter.saveSystem.GetSlotLabel (elementSlot, saveID, useSaveID, KickStarter.saveSystem.foundSaveFiles.ToArray ());
			}

			if (AdvGame.GetReferences ().settingsManager)
			{
				if (AdvGame.GetReferences ().settingsManager.saveTimeDisplay == SaveTimeDisplay.DateOnly)
				{
					return ("Save test (01/01/2001)");
				}
				else if (AdvGame.GetReferences ().settingsManager.saveTimeDisplay == SaveTimeDisplay.None)
				{
					return ("Save test");
				}
			}

			return ("Save test (01/01/2001 12:00:00)"); 
		}


		/**
		 * <summary>Gets the label of a save file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to save</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to save</param>
		 * <param name = "saveFiles">An array of SaveFile instances that the save file to retrieve is assumed to be in</param>
		 * <returns>The label of the save file.  If the save file is not found, an empty string is returned.</returns>
		 */
		public string GetSlotLabel (int elementSlot, int saveID, bool useSaveID, SaveFile[] saveFiles)
		{
			if (Application.isPlaying)
			{
				if (useSaveID)
				{
					foreach (SaveFile saveFile in saveFiles)
					{
						if (saveFile.ID == saveID)
						{
							return saveFile.label;
						}
					}
				}
				else if (elementSlot >= 0)
				{
					if (elementSlot < saveFiles.Length)
					{
						return saveFiles [elementSlot].label;
					}
				}
				return "";
			}
			return ("Save test (01/01/2001 12:00:00)");
		}


		/**
		 * <summary>Gets the screenshot of an import file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to get the screenshot of</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to look for</param>
		 * <returns>The import files's screenshots as a Texture2D.  If the save file is not found, null is returned.</returns>
		 */
		public static Texture2D GetImportSlotScreenshot (int elementSlot, int saveID, bool useSaveID)
		{
			if (Application.isPlaying && KickStarter.saveSystem.foundImportFiles != null)
			{
				return KickStarter.saveSystem.GetScreenshot (elementSlot, saveID, useSaveID, KickStarter.saveSystem.foundImportFiles.ToArray ());
			}
			return null;
		}
		

		/**
		 * <summary>Gets the screenshot of a save file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to get the screenshot of</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to look for</param>
		 * <return>The save files's screenshots as a Texture2D.  If the save file is not found, null is returned.</returns>
		 */
		public static Texture2D GetSaveSlotScreenshot (int elementSlot, int saveID, bool useSaveID)
		{
			if (Application.isPlaying && KickStarter.saveSystem.foundSaveFiles != null)
			{
				return KickStarter.saveSystem.GetScreenshot (elementSlot, saveID, useSaveID, KickStarter.saveSystem.foundSaveFiles.ToArray ());
			}
			return null;
		}


		/**
		 * <summary>Gets the screenshot of a save file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to get the screenshot of</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to look for</param>
		 * <param name = "saveFiles">An array of SaveFile instances that the save file to retrieve is assumed to be in</param>
		 * <returns>The save files's screenshots as a Texture2D.  If the save file is not found, null is returned.</returns>
		 */
		public Texture2D GetScreenshot (int elementSlot, int saveID, bool useSaveID, SaveFile[] saveFiles)
		{
			if (Application.isPlaying)
			{
				if (useSaveID)
				{
					foreach (SaveFile saveFile in saveFiles)
					{
						if (saveFile.ID == saveID)
						{
							return saveFile.screenShot;
						}
					}
				}
				else if (elementSlot >= 0)
				{
					if (elementSlot < saveFiles.Length)
					{
						return saveFiles [elementSlot].screenShot;
					}
				}
			}
			return null;
		}


		private void ReturnMainData ()
		{
			if (KickStarter.playerInput && KickStarter.runtimeInventory && KickStarter.settingsManager && KickStarter.stateHandler)
			{
				PlayerData playerData = new PlayerData ();

				if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
				{
					if (saveData.playerData.Count > 0)
					{
						playerData = saveData.playerData[0];
					}
				}
				else
				{
					foreach (PlayerData _data in saveData.playerData)
					{
						if (_data.playerID == saveData.mainData.currentPlayerID)
						{
							playerData = _data;
						}
					}
				}

				if (activeSelectiveLoad.loadPlayer)
				{
					ReturnPlayerData (playerData, KickStarter.player);
				}
				if (activeSelectiveLoad.loadSceneObjects)
				{
					ReturnCameraData (playerData);
				}

				KickStarter.stateHandler.LoadMainData (saveData.mainData);
				KickStarter.actionListAssetManager.LoadData (saveData.mainData.activeAssetLists);
				KickStarter.settingsManager.movementMethod = (MovementMethod) saveData.mainData.movementMethod;

				if (activeSelectiveLoad.loadScene)
				{
					KickStarter.sceneChanger.LoadPlayerData (playerData);
				}

				if (activeSelectiveLoad.loadPlayer)
				{
					KickStarter.playerInput.LoadPlayerData (playerData);
				}

				// Inventory
				KickStarter.runtimeInventory.RemoveRecipes ();
				if (activeSelectiveLoad.loadInventory)
				{
					KickStarter.runtimeInventory.localItems = AssignInventory (KickStarter.runtimeInventory, playerData.inventoryData);
					if (saveData.mainData.selectedInventoryID > -1)
					{
						if (saveData.mainData.isGivingItem)
						{
							KickStarter.runtimeInventory.SelectItemByID (saveData.mainData.selectedInventoryID, SelectItemMode.Give);
						}
						else
						{
							KickStarter.runtimeInventory.SelectItemByID (saveData.mainData.selectedInventoryID, SelectItemMode.Use);
						}
					}
					else
					{
						KickStarter.runtimeInventory.SetNull ();
					}
					KickStarter.runtimeInventory.RemoveRecipes ();
				}

				KickStarter.playerInput.LoadMainData (saveData.mainData);

				// Variables
				if (activeSelectiveLoad.loadVariables)
				{
					SaveSystem.AssignVariables (saveData.mainData.runtimeVariablesData);
					KickStarter.runtimeVariables.AssignCustomTokensFromString (saveData.mainData.customTokenData);
				}

				// Menus
				KickStarter.playerMenus.LoadMainData (saveData.mainData);

				KickStarter.mainCamera.HideScene ();
				KickStarter.playerMenus.HideSaveMenus ();
				KickStarter.sceneSettings.UnpauseGame (KickStarter.playerInput.timeScale);//
				KickStarter.stateHandler.gameState = GameState.Cutscene;
				KickStarter.mainCamera.FadeIn (0.5f);

				Invoke ("ReturnToGameplay", gameplayInvokeTime);
			}
			else
			{
				if (KickStarter.playerInput == null)
				{
					ACDebug.LogWarning ("Load failed - no PlayerInput found.");
				}
				if (KickStarter.runtimeInventory == null)
				{
					ACDebug.LogWarning ("Load failed - no RuntimeInventory found.");
				}
				if (KickStarter.sceneChanger == null)
				{
					ACDebug.LogWarning ("Load failed - no SceneChanger found.");
				}
				if (KickStarter.settingsManager == null)
				{
					ACDebug.LogWarning ("Load failed - no Settings Manager found.");
				}
			}
		}


		/**
		 * <summary>Checks if PlayerData for a given Player has been generated.</summary>
		 * <param name = "ID">The ID number of the Player to check</param>
		 * <param name = "doSceneCheck">If True, the check will only be successful if the Player is currently within a scene</param>
		 * <returns>True if PlayerData for the given Player exists</returns>
		 */
		public bool DoesPlayerDataExist (int ID, bool doSceneCheck)
		{
			if (saveData != null && saveData.playerData.Count > 0)
			{
				foreach (PlayerData _data in saveData.playerData)
				{
					if (_data.playerID == ID)
					{
						if (doSceneCheck && _data.currentScene == -1)
						{
							return false;
						}
						return true;
					}
				}
			}

			return false;
		}


		/**
		 * <summary>Gets the current scene number that a Player is in.</summary>
		 * <param name = "ID">The ID number of the Player to check</param>
		 * <returns>The current scene number that the Player is in.  If the Player is not found, the currently-open scene number will be returned.</returns>
		 */
		public int GetPlayerScene (int ID)
		{
			if (KickStarter.player)
			{
				if (saveData.playerData.Count > 0)
				{
					foreach (PlayerData _data in saveData.playerData)
					{
						if (_data.playerID == ID)
						{
							return (_data.currentScene);
						}
					}
				}
			}
			return UnityVersionHandler.GetCurrentSceneNumber ();
		}


		/**
		 * <summary>Gets the current scene nsmr that a Player is in.</summary>
		 * <param name = "ID">The ID number of the Player to check</param>
		 * <returns>The current scene name that the Player is in.  If the Player is not found, the currently-open scene name will be returned.</returns>
		 */
		public string GetPlayerSceneName (int ID)
		{
			if (KickStarter.player)
			{
				if (saveData.playerData.Count > 0)
				{
					foreach (PlayerData _data in saveData.playerData)
					{
						if (_data.playerID == ID)
						{
							return (_data.currentSceneName);
						}
					}
				}
			}
			
			return UnityVersionHandler.GetCurrentSceneName ();
		}


		/**
		 * <summary>Unloads stored PlayerData back onto the Player object.</summary>
		 * <param name = "ID">The ID of the Player to affect</param>
		 * <param name = "doInventory">If True, updates the Player's inventory</param>
		 * <returns>The Player's new scene number</returns>
		 */
		public int AssignPlayerData (int ID, bool doInventory)
		{
			if (KickStarter.player)
			{
				if (saveData.playerData.Count > 0)
				{
					foreach (PlayerData _data in saveData.playerData)
					{
						if (_data.playerID == ID)
						{
							if (_data.currentScene != -1)
							{
								// If -1, data only exists because we updated inventory, so only restore Inventory in this case

								ReturnPlayerData (_data, KickStarter.player);
								ReturnCameraData (_data);
								KickStarter.playerInput.LoadPlayerData (_data);
								KickStarter.sceneChanger.LoadPlayerData (_data);
							}

							KickStarter.runtimeInventory.SetNull ();
							KickStarter.runtimeInventory.RemoveRecipes ();

							if (doInventory)
							{
								KickStarter.runtimeInventory.localItems = AssignInventory (KickStarter.runtimeInventory, _data.inventoryData);
							}

							return (_data.currentScene);
						}
					}
				}
			}
			AssetLoader.UnloadAssets ();
			return UnityVersionHandler.GetCurrentSceneNumber ();
		}


		private void ReturnPlayerData (PlayerData playerData, Player player)
		{
			if (player == null)
			{
				return;
			}

			player.LoadPlayerData (playerData);
		}


		private void ReturnCameraData (PlayerData playerData)
		{
			// Camera
			MainCamera mainCamera = KickStarter.mainCamera;
			mainCamera.LoadData (playerData);
		}


		private void ReturnToGameplay ()
		{
			loadingGame = LoadingGame.No;
			KickStarter.playerInput.ReturnToGameplayAfterLoad ();

			if (KickStarter.sceneSettings)
			{
				KickStarter.sceneSettings.OnLoad ();
			}
		}
		

		/**
		 * <summary>Unloads stored global variable data back into the RuntimeVariables script.</summary>
		 * <param name = "runtimeVariablesData">The values of all global variables, combined into a stingle string</param>
		 * <param name = "fromOptions">If true, only global variables that are linked to OptionsData will be affected</param>
		 */
		public static void AssignVariables (string runtimeVariablesData, bool fromOptions = false)
		{
			if (runtimeVariablesData == null)
			{
				return;
			}

			KickStarter.runtimeVariables.ClearSpeechLog ();
			
			if (runtimeVariablesData.Length > 0)
			{
				string[] varsArray = runtimeVariablesData.Split (SaveSystem.pipe[0]);
				foreach (string chunk in varsArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _id = 0;
					int.TryParse (chunkData[0], out _id);
					GVar var = GlobalVariables.GetVariable (_id);
				
					if (var == null)
					{
						continue;
					}
					if (fromOptions && var.link != VarLink.OptionsData)
					{
						continue;
					}

					if (var.type == VariableType.String)
					{
						string _text = chunkData[1];
						_text = AdvGame.PrepareStringForLoading (_text);
						var.SetStringValue (_text);
					}
					else if (var.type == VariableType.Float)
					{
						float _value = 0f;
						float.TryParse (chunkData[1], out _value);
						var.SetFloatValue (_value, SetVarMethod.SetValue);
					}
					else
					{
						int _value = 0;
						int.TryParse (chunkData[1], out _value);
						var.SetValue (_value, SetVarMethod.SetValue);
					}
				}
			}
			
			GlobalVariables.UploadAll ();
		}

		
		private List<InvItem> AssignInventory (RuntimeInventory _runtimeInventory, string inventoryData)
		{
			List<InvItem> invItems = new List<InvItem>();

			if (inventoryData != null && inventoryData.Length > 0)
			{
				string[] countArray = inventoryData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in countArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _id = 0;
					int.TryParse (chunkData[0], out _id);
		
					int _count = 0;
					int.TryParse (chunkData[1], out _count);
					
					invItems = _runtimeInventory.Add (_id, _count, invItems, false);
				}
			}

			return invItems;
		}


		private string CreateInventoryData (List<InvItem> invItems)
		{
			System.Text.StringBuilder inventoryString = new System.Text.StringBuilder ();
			
			foreach (InvItem item in invItems)
			{
				if (item != null)
				{
					inventoryString.Append (item.id.ToString ());
					inventoryString.Append (SaveSystem.colon);
					inventoryString.Append (item.count.ToString ());
					inventoryString.Append (SaveSystem.pipe);
				}
			}
			
			if (invItems != null && invItems.Count > 0)
			{
				inventoryString.Remove (inventoryString.Length-1, 1);
			}
			
			return inventoryString.ToString ();		
		}
		

		/**
		 * <summary>Condenses the values of a List of variables into a single string.</summary>
		 * <param name = "vars">A List of variables (see GVar) to condense</param>
		 * <param name = "isOptionsData">If True, only global variables that are linked to OptionsData will be included</param>
		 * <param name = "location">The variables' location (Local, Variable)</param>
		 * <returns>The variable's values, condensed into a single string</returns>
		 */
		public static string CreateVariablesData (List<GVar> vars, bool isOptionsData, VariableLocation location)
		{
			System.Text.StringBuilder variablesString = new System.Text.StringBuilder ();

			foreach (GVar _var in vars)
			{
				if ((isOptionsData && _var.link == VarLink.OptionsData) || (!isOptionsData && _var.link != VarLink.OptionsData) || location == VariableLocation.Local)
				{
					variablesString.Append (_var.id.ToString ());
					variablesString.Append (SaveSystem.colon);
					if (_var.type == VariableType.String)
					{
						string textVal = _var.textVal;
						textVal = AdvGame.PrepareStringForSaving (textVal);
						variablesString.Append (textVal);
					}
					else if (_var.type == VariableType.Float)
					{
						variablesString.Append (_var.floatVal.ToString ());
					}
					else
					{
						variablesString.Append (_var.val.ToString ());
					}
					variablesString.Append (SaveSystem.pipe);
				}
			}
			
			if (variablesString.Length > 0)
			{
				variablesString.Remove (variablesString.Length-1, 1);
			}

			return variablesString.ToString ();		
		}


		/**
		 * <summary>Returns a list of inventory items currently carried by a particular Player.</summary>
		 * <param name = "_playerID">The ID number of the Player to check the inventory of</param>
		 * <returns>A List of InvItem instances representing the inventory items</returns>
		 */
		public List<InvItem> GetItemsFromPlayer (int _playerID)
		{
			if (KickStarter.player.ID == _playerID)
			{
				return KickStarter.runtimeInventory.localItems;
			}

			if (saveData != null && saveData.playerData != null)
			{
				foreach (PlayerData _data in saveData.playerData)
				{
					if (_data.playerID == _playerID)
					{
						return AssignInventory (KickStarter.runtimeInventory, _data.inventoryData);
					}
				}
			}
			return new List<InvItem>();
		}


		/**
		 * <summary>Re-assigns the inventory items currently carried by a particular Player.</summary>
		 * <param name = "invItems">A List of InvItem instances representing the inventory items</param>
		 * <param name = "_playerID">The ID number of the Player to assign the inventory of</param>
		 */
		public void AssignItemsToPlayer (List<InvItem> invItems, int _playerID)
		{
			string invData = CreateInventoryData (invItems);

			if (saveData != null && saveData.playerData != null)
			{
				foreach (PlayerData data in saveData.playerData)
				{
					if (data.playerID == _playerID)
					{
						PlayerData newPlayerData = new PlayerData ();
						newPlayerData.CopyData (data);
						newPlayerData.inventoryData = invData;

						saveData.playerData.Remove (data);
						saveData.playerData.Add (newPlayerData);

						return;
					}
				}
			}

			PlayerData playerData = new PlayerData ();
			playerData.playerID = _playerID;
			playerData.inventoryData = invData;
			playerData.currentScene = -1;

			if (saveData == null)
			{
				ClearAllData ();
			}

			saveData.playerData.Add (playerData);
		}


		private void CustomSaveHook ()
		{
			ISave[] saveHooks = GetSaveHooks (GetComponents (typeof (ISave)));
			if (saveHooks != null && saveHooks.Length > 0)
			{
				foreach (ISave saveHook in saveHooks)
				{
					saveHook.PreSave ();
				}
			}
		}


		private void CustomLoadHook ()
		{
			ISave[] saveHooks = GetSaveHooks (GetComponents (typeof (ISave)));
			if (saveHooks != null && saveHooks.Length > 0)
			{
				foreach (ISave saveHook in saveHooks)
				{
					saveHook.PostLoad ();
				}
			}
		}


		private ISave[] GetSaveHooks (IList list)
		{
			ISave[] ret = new ISave[list.Count];
			list.CopyTo (ret, 0);
			return ret;
		}


		/**
		 * <summary>Renames the label of a save game file.</summary>
		 * <param name = "newLabel">The new label to give the save game file</param>
		 * <param name = "saveIndex">The index of the foundSaveFiles List that represents the save file to affect</param>
		 */
		public void RenameSave (string newLabel, int saveIndex)
		{
			if (newLabel.Length == 0)
			{
				return;
			}

			GatherSaveFiles ();

			if (foundSaveFiles.Count > saveIndex && saveIndex >= 0)
			{
				SaveFile newSaveFile = new SaveFile (foundSaveFiles [saveIndex]);
				newSaveFile.SetLabel (newLabel);
				foundSaveFiles [saveIndex] = newSaveFile;
				Options.UpdateSaveLabels (foundSaveFiles.ToArray ());
			}
		}


		/**
		 * <summary>Deletes a player profile.</summary>
		 * <param name = "profileIndex">The index in the MenuProfilesList element that represents the profile to delete. If it is set to its default, -2, the active profile will be deleted</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that the profile was selected from also displays the active profile</param>
		 */
		public void DeleteProfile (int profileIndex = -2, bool includeActive = true)
		{
			if (!KickStarter.settingsManager.useProfiles)
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
			
			// Delete save files
			for (int i=0; i<50; i++)
			{
				string fileName = GetSaveFileName (i, profileID);
				Serializer.DeleteSaveFile (fileName);
				if (KickStarter.settingsManager.takeSaveScreenshots)
				{
					Serializer.DeleteScreenshot (GetSaveScreenshotName (i, profileID));
				}
			}

			bool isActive = false;
			if (profileID == Options.GetActiveProfileID ())
			{
				isActive = true;
			}
			Options.DeleteProfilePrefs (profileID);
			if (isActive)
			{
				GatherSaveFiles ();
			}
			KickStarter.playerMenus.RecalculateAll ();
		}


		/**
		 * <summary>Deletes a save game file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuProfilesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to import</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to import</param>
		 */
		public void DeleteSave (int elementSlot, int saveID, bool useSaveID)
		{
			if (!useSaveID)
			{
				// For this to work, must have loaded the list of saves into a SavesList
				saveID = KickStarter.saveSystem.foundSaveFiles[elementSlot].ID;
			}

			Serializer.DeleteSaveFile (GetSaveFileName (saveID));
			if (KickStarter.settingsManager.takeSaveScreenshots)
			{
				Serializer.DeleteScreenshot (GetSaveScreenshotName (saveID));
			}

			// Also remove save label
			GatherSaveFiles ();
			foreach (SaveFile saveFile in foundSaveFiles)
			{
				if (saveFile.ID == saveID)
				{
					foundSaveFiles.Remove (saveFile);
					Options.UpdateSaveLabels (foundSaveFiles.ToArray ());
					break;
				}
			}

			if (Options.optionsData != null && Options.optionsData.lastSaveID == saveID)
			{
				Options.optionsData.lastSaveID = -1;
				Options.SavePrefs ();
			}
			KickStarter.playerMenus.RecalculateAll ();
		}


		/**
		 * <summary>Gets the number of save game files found.</summary>
		 * <param name = "includeAutoSaves">If True, then autosave files will be included in the result</param>
		 * <returns>The number of save games found</returns>
		 */
		public int GetNumSaves (bool includeAutoSaves = true)
		{
			int numFound = 0;
			foreach (SaveFile saveFile in foundSaveFiles)
			{
				if (!saveFile.isAutoSave || includeAutoSaves)
				{
					numFound ++;
				}
			}
			return numFound;
		}


		private void ResetSceneObjects ()
		{
			AC.Char[] characters = FindObjectsOfType (typeof (AC.Char)) as AC.Char[];
			foreach (AC.Char character in characters)
			{
				character.AfterLoad ();
			}

			AC.Sound[] sounds = FindObjectsOfType (typeof (AC.Sound)) as AC.Sound[];
			foreach (AC.Sound sound in sounds)
			{
				if (sound != null)
				{
					sound.AfterLoad ();
				}
			}

			FirstPersonCamera[] firstPersonCameras = FindObjectsOfType (typeof (FirstPersonCamera)) as FirstPersonCamera[];
			foreach (FirstPersonCamera firstPersonCamera in firstPersonCameras)
			{
				firstPersonCamera.AfterLoad ();
			}

			FollowTintMap[] followTintMaps = FindObjectsOfType (typeof (FollowTintMap)) as FollowTintMap[];
			foreach (FollowTintMap followTintMap in followTintMaps)
			{
				followTintMap.AfterLoad ();
			}

			FollowSortingMap[] followSortingMaps = FindObjectsOfType (typeof (FollowSortingMap)) as FollowSortingMap[];
			foreach (FollowSortingMap followSortingMap in followSortingMaps)
			{
				followSortingMap.AfterLoad ();
			}

			DetectHotspots[] detectHotspots = FindObjectsOfType (typeof (DetectHotspots)) as DetectHotspots[];
			foreach (DetectHotspots detectHotspot in detectHotspots)
			{
				detectHotspot.AfterLoad ();
			}

			KickStarter.playerMenus.AfterLoad ();
			KickStarter.runtimeInventory.AfterLoad ();
			KickStarter.sceneChanger.AfterLoad ();
			KickStarter.options.AfterLoad ();
			
			KickStarter.kickStarter.AfterLoad ();
		}

	}


	/**
	 * A data container for save game files found in the file system.  Instances of this struct are listed in the foundSaveFiles List in SaveSystem.
	 */
	public struct SaveFile
	{

		/** A unique identifier for the save file */
		public int ID;
		/** The save's label, as displayed in a MenuSavesList element */
		public string label;
		/** The save's screenshot, if save game screenshots are enabled */
		public Texture2D screenShot;
		/** The complete filename of the file, including the filepath */
		public string fileName;
		/** If True, then the file is considered to be an AutoSave */
		public bool isAutoSave;
		/** The timestamp of the file's last-updated time */
		public int updatedTime;


		/**
		 * The default Constructor.
		 */
		public SaveFile (int _ID, string _label, Texture2D _screenShot, string _fileName, bool _isAutoSave, int _updatedTime = 0)
		{
			ID = _ID;
			label = _label;
			screenShot = _screenShot;
			fileName = _fileName;
			isAutoSave = _isAutoSave;

			if (_updatedTime > 0)
			{
				updatedTime = 200000000 - _updatedTime;
			}
			else
			{
				updatedTime = 0;
			}
		}


		/**
		 * <summary>Sets the save file's label in a safe format. Pipe's and colons are converted so that they can be stored.</summary>
		 * <param name = "_label">The new label for the file.</param>
		 */
		public void SetLabel (string _label)
		{
			label = AdvGame.PrepareStringForLoading (_label);
		}


		/**
		 * <summary>Gets the save file's label.  Pipes and colons are converted back so that they can be read as expected.</summary>
		 * <returns>The file's label</returns>
		 */
		public string GetSafeLabel ()
		{
			return AdvGame.PrepareStringForSaving (label);
		}


		/**
		 * A Constructor that copies the values of another SaveFile.
		 */
		public SaveFile (SaveFile _saveFile)
		{
			ID = _saveFile.ID;
			label = _saveFile.label;
			screenShot = _saveFile.screenShot;
			fileName = _saveFile.fileName;
			isAutoSave = _saveFile.isAutoSave;
			updatedTime = _saveFile.updatedTime;
		}

	}

}