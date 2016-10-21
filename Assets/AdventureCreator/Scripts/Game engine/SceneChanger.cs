/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"SceneChanger.cs"
 * 
 *	This script handles the changing of the scene, and stores
 *	which scene was previously loaded, for use by PlayerStart.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Handles the changing of the scene, and keeps track of which scene was previously loaded.
	 * It should be placed on the PersistentEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_scene_changer.html")]
	#endif
	public class SceneChanger : MonoBehaviour
	{

		/** Info about the previous scene */
		public SceneInfo previousSceneInfo;

		private List<SubScene> subScenes = new List<SubScene>();

		private Vector3 relativePosition;
		private AsyncOperation preloadAsync;
		private SceneInfo preloadSceneInfo;
		private SceneInfo thisSceneInfo;
		private Player playerOnTransition = null;
		private Texture2D textureOnTransition = null;
		private bool isLoading = false;
		private float loadingProgress = 0f;


		public void OnAwake ()
		{
			previousSceneInfo = new SceneInfo ("", -1);
			relativePosition = Vector3.zero;
			isLoading = false;
			AssignThisSceneInfo ();
		}


		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			AssignThisSceneInfo ();
		}


		private void AssignThisSceneInfo ()
		{
			thisSceneInfo = new SceneInfo (UnityVersionHandler.GetCurrentSceneName (), UnityVersionHandler.GetCurrentSceneNumber ());
		}


		/**
		 * <summary>Calculates the player's position relative to the next scene's PlayerStart.</summary>
		 * <param name = "markerTransform">The Transform of the GameObject that marks the position that the player should be placed relative to.</param>
		 */
		public void SetRelativePosition (Transform markerTransform)
		{
			if (KickStarter.player == null || markerTransform == null)
			{
				relativePosition = Vector2.zero;
			}
			else
			{
				relativePosition = KickStarter.player.transform.position - markerTransform.position;
				if (KickStarter.settingsManager.IsUnity2D ())
				{
					relativePosition.z = 0f;
				}
				else if (KickStarter.settingsManager.IsTopDown ())
				{
					relativePosition.y = 0f;
				}
			}
		}


		/**
		 * <summary>Gets the player's starting position by adding the relative position (set in ActionScene) to the PlayerStart's position.</summary>
		 * <param name = "playerStartPosition">The position of the PlayerStart object</param>
		 * <returns>The player's starting position</returns>
		 */
		public Vector3 GetStartPosition (Vector3 playerStartPosition)
		{
			Vector3 startPosition = playerStartPosition + relativePosition;
			relativePosition = Vector2.zero;
			return startPosition;
		}


		/**
		 * <summary>Gets the progress of an asynchronous scene load as a decimal.</summary>
		 * <returns>The progress of an asynchronous scene load as a decimal.</returns>
		 */
		public float GetLoadingProgress ()
		{
			if (KickStarter.settingsManager.useAsyncLoading)
			{
				if (isLoading)
				{
					return loadingProgress;
				}
			}
			else
			{
				ACDebug.LogWarning ("Cannot get the loading progress because asynchronous loading is not enabled in the Settings Manager.");
			}
			return 0f;
		}


		public bool IsLoading ()
		{
			return isLoading;
		}


		public void PreloadScene (SceneInfo nextSceneInfo)
		{
			StartCoroutine (PreloadLevelAsync (nextSceneInfo));
		}


		/**
		 * <summary>Loads a new scene.</summary>
		 * <param name = "nextSceneInfo">Info about the scene to load</param>
		 * <param name = "sceneNumber">The number of the scene to load, if sceneName = ""</param>
		 * <param name = "saveRoomData">If True, then the states of the current scene's Remember scripts will be recorded in LevelStorage</param>
		 * <param name = "forceReload">If True, the scene will be re-loaded if it is already open.</param>
		 */
		public void ChangeScene (SceneInfo nextSceneInfo, bool saveRoomData, bool forceReload = false)
		{
			if (!isLoading)
			{
				if (!nextSceneInfo.Matches (thisSceneInfo) || forceReload)
				{
					PrepareSceneForExit (!KickStarter.settingsManager.useAsyncLoading, saveRoomData);
					LoadLevel (nextSceneInfo, KickStarter.settingsManager.useLoadingScreen, KickStarter.settingsManager.useAsyncLoading, forceReload);
				}
			}
		}


		/**
		 * <summary>Loads the previously-entered scene.</summary>
		 */
		public void LoadPreviousScene ()
		{
			if (previousSceneInfo != null)
			{
				ChangeScene (previousSceneInfo, true);
			}
		}


		/**
		 * <summary>Gets the Player prefab that was active during the last scene transition.</summary>
		 * <returns>The Player prefab that was active during the last scene transition</returns>
		 */
		public Player GetPlayerOnTransition ()
		{
			return playerOnTransition;
		}


		/**
		 * Destroys the Player prefab that was active during the last scene transition.
		 */
		public void DestroyOldPlayer ()
		{
			if (playerOnTransition)
			{
				ACDebug.Log ("New player prefab found - " + playerOnTransition.name + " deleted");
				DestroyImmediate (playerOnTransition.gameObject);
			}
		}


		/*
		 * <summary>Stores a texture used as an overlay during a scene transition. This texture can be retrieved with GetAndResetTransitionTexture().</summary>
		 * <param name = "_texture">The Texture2D to store</param>
		 */
		public void SetTransitionTexture (Texture2D _texture)
		{
			textureOnTransition = _texture;
		}


		/**
		 * <summary>Gets, and removes from memory, the texture used as an overlay during a scene transition.</summary>
		 * <returns>The texture used as an overlay during a scene transition</returns>
		 */
		public Texture2D GetAndResetTransitionTexture ()
		{
			Texture2D _texture = textureOnTransition;
			textureOnTransition = null;
			return _texture;
		}


		private void LoadLevel (SceneInfo nextSceneInfo, bool useLoadingScreen, bool useAsyncLoading, bool forceReload = false)
		{
			if (useLoadingScreen)
			{
				StartCoroutine (LoadLoadingScreen (nextSceneInfo, new SceneInfo (KickStarter.settingsManager.loadingSceneIs, KickStarter.settingsManager.loadingSceneName, KickStarter.settingsManager.loadingScene), useAsyncLoading));
			}
			else
			{
				if (useAsyncLoading && !forceReload)
				{
					StartCoroutine (LoadLevelAsync (nextSceneInfo));
				}
				else
				{
					StartCoroutine (LoadLevelCo (nextSceneInfo, forceReload));
				}
			}
		}


		private IEnumerator LoadLoadingScreen (SceneInfo nextSceneInfo, SceneInfo loadingSceneInfo, bool loadAsynchronously = false)
		{
			isLoading = true;
			loadingProgress = 0f;

			loadingSceneInfo.LoadLevel ();
			yield return null;
			
			if (KickStarter.player != null)
			{
				KickStarter.player.transform.position += new Vector3 (0f, -10000f, 0f);
			}

			PrepareSceneForExit (true, false);
			if (loadAsynchronously)
			{
				yield return new WaitForSeconds (KickStarter.settingsManager.loadingDelay);

				AsyncOperation aSync = null;
				if (nextSceneInfo.Matches (preloadSceneInfo))
				{
					aSync = preloadAsync;
				}
				else
				{
					aSync = nextSceneInfo.LoadLevelASync ();
				}

				if (KickStarter.settingsManager.loadingDelay > 0f)
				{
					aSync.allowSceneActivation = false;

					while (aSync.progress < 0.9f)
					{
						loadingProgress = aSync.progress;
						yield return null;
					}
				
					isLoading = false;
					yield return new WaitForSeconds (KickStarter.settingsManager.loadingDelay);
					aSync.allowSceneActivation = true;
				}
				else
				{
					while (!aSync.isDone)
					{
						loadingProgress = aSync.progress;
						yield return null;
					}
				}
				KickStarter.stateHandler.GatherObjects ();
			}
			else
			{
				nextSceneInfo.LoadLevel ();
			}

			isLoading = false;
			preloadAsync = null;
			preloadSceneInfo = new SceneInfo ("", -1);

			if (KickStarter.eventManager != null)
			{
				KickStarter.eventManager.Call_OnAfterChangeScene ();
			}
		}


		private IEnumerator LoadLevelAsync (SceneInfo nextSceneInfo)
		{
			isLoading = true;
			loadingProgress = 0f;
			PrepareSceneForExit (true, false);

			AsyncOperation aSync = null;
			if (nextSceneInfo.Matches (preloadSceneInfo))
			{
				aSync = preloadAsync;
				aSync.allowSceneActivation = true;
			}
			else
			{
				aSync = nextSceneInfo.LoadLevelASync ();
			}

			while (!aSync.isDone)
			{
				loadingProgress = aSync.progress;
				yield return null;
			}

			KickStarter.stateHandler.GatherObjects ();
			isLoading = false;
			preloadAsync = null;
			preloadSceneInfo = new SceneInfo ("", -1);

			if (KickStarter.eventManager != null)
			{
				KickStarter.eventManager.Call_OnAfterChangeScene ();
			}
		}


		private IEnumerator PreloadLevelAsync (SceneInfo nextSceneInfo)
		{
			loadingProgress = 0f;

			preloadSceneInfo = nextSceneInfo;
			preloadAsync = nextSceneInfo.LoadLevelASync ();
			preloadAsync.allowSceneActivation = false;

			// Wait until done and collect progress as we go.
			while (!preloadAsync.isDone)
			{
				loadingProgress = preloadAsync.progress;
				if (loadingProgress >= 0.9f)
				{
					// Almost done.
					break;
				}
				yield return null;
			}
		}


		private IEnumerator LoadLevelCo (SceneInfo nextSceneInfo, bool forceReload = false)
		{
			isLoading = true;
			yield return new WaitForEndOfFrame ();

			nextSceneInfo.LoadLevel (forceReload);
			isLoading = false;

			if (KickStarter.eventManager != null)
			{
				KickStarter.eventManager.Call_OnAfterChangeScene ();
			}
		}


		private void PrepareSceneForExit (bool isInstant, bool saveRoomData)
		{
			if (isInstant)
			{
				KickStarter.mainCamera.FadeOut (0f);
				
				if (KickStarter.player)
				{
					KickStarter.player.Halt ();
				}
				
				KickStarter.stateHandler.gameState = GameState.Normal;
			}
			
			Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
			foreach (Sound sound in sounds)
			{
				sound.TryDestroy ();
			}
			KickStarter.stateHandler.GatherObjects ();
			
			KickStarter.playerMenus.ClearParents ();
			if (KickStarter.dialog)
			{
				KickStarter.dialog.KillDialog (true, true);
			}
			
			if (saveRoomData)
			{
				KickStarter.levelStorage.StoreAllOpenLevelData ();
				previousSceneInfo = new SceneInfo ();
			}
			subScenes.Clear ();

			playerOnTransition = KickStarter.player;

			if (KickStarter.eventManager != null)
			{
				KickStarter.eventManager.Call_OnBeforeChangeScene ();
			}
		}


		/** SUB-SCENES */

		/**
		 * <summary>Adds a new scene as a sub-scene, without affecting any other open scenes.</summary>
		 * <param name = "sceneInfo">The SceneInfo of the new scene to open</param>
		 */
		public bool AddSubScene (SceneInfo sceneInfo)
		{
			// Check if scene is already open
			if (sceneInfo.Matches (thisSceneInfo))
			{
				return false;
			}
		
			foreach (SubScene subScene in subScenes)
			{
				if (subScene.SceneInfo.Matches (sceneInfo))
				{
					return false;
				}
			}
		
			sceneInfo.AddLevel ();

			KickStarter.playerMenus.AfterSceneAdd ();
			return true;
		}


		/**
		 * <summary>Registers a SubScene component with the SceneChanger.</summary>
		 * <param name = "subScene">The SubScene component to register</param>
		 */
		public void RegisterSubScene (SubScene subScene)
		{
			if (!subScenes.Contains (subScene))
			{
				subScenes.Add (subScene);

				KickStarter.levelStorage.ReturnSubSceneData (subScene, isLoading);
				KickStarter.stateHandler.GatherObjects ();
			}
		}


		/**
		 * <summary>Gets an array of all open SubScenes.</summary>
		 * <returns>An array of all open SubScenes</returns>
		 */
		public SubScene[] GetSubScenes ()
		{
			return subScenes.ToArray ();
		}


		/**
		 * <summary>Removes a scene, without affecting any other open scenes, provided multiple scenes are open. If the active scene is removed, the last-added sub-scene will become the new active scene.</summary>
		 * <param name = "sceneInfo">The SceneInfo of the new scene to remove</param>
		 */
		public bool RemoveScene (SceneInfo sceneInfo)
		{
			// Kill actionlists
			KickStarter.actionListManager.KillAllFromScene (sceneInfo);

			if (thisSceneInfo.Matches (sceneInfo))
			{
				// Want to close active scene

				if (subScenes == null || subScenes.Count == 0)
				{
					ACDebug.LogWarning ("Cannot remove scene " + sceneInfo.number + ", as it is the only one open!");
					return false;
				}

				// Save active scene
				KickStarter.levelStorage.StoreCurrentLevelData ();

				// Make the last-opened subscene the new active one
				SubScene lastSubScene = subScenes [subScenes.Count-1];
				KickStarter.mainCamera.gameObject.SetActive (false);
				lastSubScene.MakeMain ();
				subScenes.Remove (lastSubScene);

				StartCoroutine (CloseScene (thisSceneInfo));
				thisSceneInfo = lastSubScene.SceneInfo;
				return true;
			}

			// Want to remove a sub-scene
			for (int i=0; i<subScenes.Count; i++)
			{
				if (subScenes[i].SceneInfo.Matches (sceneInfo))
				{
					// Save sub scene
					KickStarter.levelStorage.StoreSubSceneData (subScenes[i]);

					StartCoroutine (CloseScene (subScenes[i].SceneInfo));
					subScenes.RemoveAt (i);
					return true;
				}
			}

			return false;
		}


		private IEnumerator CloseScene (SceneInfo _sceneInfo)
		{
			yield return new WaitForEndOfFrame ();

			_sceneInfo.CloseLevel ();

			KickStarter.stateHandler.GatherObjects (true);
			KickStarter.stateHandler.RegisterWithGameEngine ();
		}


		/**
		 * <summary>Saves data used by this script in a PlayerData class.</summary>
		 * <param name = "playerData">The PlayerData to save in.</param>
		 * <returns>The updated PlayerData</returns>
		 */
		public PlayerData SavePlayerData (PlayerData playerData)
		{
			playerData.previousScene = previousSceneInfo.number;
			playerData.previousSceneName = previousSceneInfo.name;

			System.Text.StringBuilder subSceneData = new System.Text.StringBuilder ();
			foreach (SubScene subScene in subScenes)
			{
				subSceneData.Append (subScene.SceneInfo.name + SaveSystem.colon + subScene.SceneInfo.number + SaveSystem.pipe);
			}
			if (subSceneData.Length > 0)
			{
				subSceneData.Remove (subSceneData.Length-1, 1);
			}
			playerData.openSubScenes = subSceneData.ToString ();

			return playerData;
		}


		/**
		 * <summary>Loads data used by this script from a PlayerData class.</summary>
		 * <param name = "playerData">The PlayerData to load from.</param>
		 */
		public void LoadPlayerData (PlayerData playerData)
		{
			previousSceneInfo = new SceneInfo (playerData.previousSceneName, playerData.previousScene);

			foreach (SubScene subScene in subScenes)
			{
				subScene.SceneInfo.CloseLevel ();
			}
			subScenes.Clear ();

			if (playerData.openSubScenes != null && playerData.openSubScenes.Length > 0)
			{
				string[] subSceneArray = playerData.openSubScenes.Split (SaveSystem.pipe[0]);
				foreach (string chunk in subSceneArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					int _number = 0;
					int.TryParse (chunkData[0], out _number);
					SceneInfo sceneInfo = new SceneInfo (chunkData[0], _number);
					AddSubScene (sceneInfo);
				}
			}

			KickStarter.stateHandler.GatherObjects (true);
			KickStarter.stateHandler.RegisterWithGameEngine ();
		}

	}


	/**
	 * A container for information about a scene that can be loaded.
	 */
	public class SceneInfo
	{

		/** The scene's name */
		public string name;
		/** The scene's number. If name is left empty, this number will be used to reference the scene instead */
		public int number;


		/**
		 * A Constructor for the current active scene.
		 */
		public SceneInfo ()
		{
			number = UnityVersionHandler.GetCurrentSceneNumber ();
			name = UnityVersionHandler.GetCurrentSceneName ();
		}


		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_name">The scene's name</param>
		 * <param name = "_number">The scene's number. If name is left empty, this number will be used to reference the scene instead</param>
		 */
		public SceneInfo (string _name, int _number)
		{
			number = _number;
			name = _name;
		}


		/**
		 * <summary>A Constructor.</summary>
		 * <param name = "chooseSeneBy">The method by which the scene is referenced (Name, Number)</param>
		 * <param name = "_name">The scene's name</param>
		 * <param name = "_number">The scene's number. If name is left empty, this number will be used to reference the scene instead</param>
		 */
		public SceneInfo (ChooseSceneBy chooseSceneBy, string _name, int _number)
		{
			number = _number;

			if (chooseSceneBy == ChooseSceneBy.Number)
			{
				name = "";
			}
			else
			{
				name = _name;
			}
		}


		/**
		 * <summary>Checks if the variables in this instance of the class match another instance.</summary>
		 * <param name = "_sceneInfo">The other SceneInfo instance to compare</param>
		 * <returns>True if the variables in this instance of the class matches the other instance</returns>
		 */
		public bool Matches (SceneInfo _sceneInfo)
		{
			if (_sceneInfo != null)
			{
				if (number == _sceneInfo.number)
				{
					if (name != "" && _sceneInfo.name != "" && name == _sceneInfo.name)
					{
						return true;
					}
					if (name == "" || _sceneInfo.name == "")
					{
						return true;
					}
				}
			}

			/*if (_sceneInfo != null && name == _sceneInfo.name && number == _sceneInfo.number)
			{
				return true;
			}*/
			return false;
		}


		/*
		 * <summary>Gets a string with info about the scene the class represents.</summary>
		 * <returns>A string with info about the scene the class represents.</returns>
		 */
		public string GetLabel ()
		{
			if (name != "")
			{
				return name;
			}
			return number.ToString ();
		}


		/**
		 * <summary>Loads the scene normally.</summary>
		 * <param name = "forceReload">If True, the scene will be re-loaded if it is already open.</param>
		 */
		public void LoadLevel (bool forceReload = false)
		{
			if (name != "")
			{
				UnityVersionHandler.OpenScene (name, forceReload);
			}
			else
			{
				UnityVersionHandler.OpenScene (number, forceReload);
			}
		}


		/**
		 * <summary>Adds the scene additively.</summary>
		 */
		public void AddLevel ()
		{
			if (name != "")
			{
				UnityVersionHandler.OpenScene (name, false, true);
			}
			else
			{
				UnityVersionHandler.OpenScene (number, false, true);
			}
		}


		/**
		 * <summary>Closes the scene additively.</summary>
		 * <returns>True if the operation was successful</returns>
		 */
		public bool CloseLevel ()
		{
			if (name != "")
			{
				return UnityVersionHandler.CloseScene (name);
			}
			return UnityVersionHandler.CloseScene (number);
		}


		/**
		 * <summary>Loads the scene asynchronously.</summary>
		 * <returns>The generated AsyncOperation class</returns>
		 */
		public AsyncOperation LoadLevelASync ()
		{
			return UnityVersionHandler.LoadLevelAsync (number, name);
		}

	}

}