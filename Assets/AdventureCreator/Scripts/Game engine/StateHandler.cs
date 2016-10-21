/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"SceneHandler.cs"
 * 
 *	This script stores the gameState variable, which is used by
 *	other scripts to determine if the game is running normal gameplay,
 *	in a cutscene, paused, or displaying conversation options.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script stores the all-important gameState variable, which determines if the game is running normal gameplay, is in a cutscene, or is paused.
	 * It also runs the various "Update", "LateUpdate", "FixedUpdate" and "OnGUI" functions that are within Adventure Creator's main scripts - by running them all from here, performance is drastically improved.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_state_handler.html")]
	#endif
	public class StateHandler : MonoBehaviour
	{

		private GameState _gameState = GameState.Normal;

		private Music music;
		private bool inScriptedCutscene;
		private GameState previousUpdateState = GameState.Normal;
		private GameState lastNonPausedState = GameState.Normal;
		private bool isACDisabled = false;

		private bool cursorIsOff = false;
		private bool inputIsOff = false;
		private bool interactionIsOff = false;
		private bool menuIsOff = false;
		private bool movementIsOff = false;
		private bool cameraIsOff = false;
		private bool triggerIsOff = false;
		private bool playerIsOff = false;

		private bool originalMenuState;
		private bool originalCursorState;

		private bool playedGlobalOnStart = false;
		private bool hasGameEngine = false;

		private ArrowPrompt[] arrowPrompts;
		private DragBase[] dragBases;
		private Parallax2D[] parallax2Ds;
		private Hotspot[] hotspots;
		private Highlight[] highlights;
		private _Camera[] cameras;
		private Sound[] sounds;
		private LimitVisibility[] limitVisibilitys;
		private Char[] characters;

		private int _i = 0;


		public void OnAwake ()
		{
			Time.timeScale = 1f;
			DontDestroyOnLoad (this);
			GetReferences ();

			InitPersistentEngine ();
		}


		private void InitPersistentEngine ()
		{
			KickStarter.runtimeLanguages.OnAwake ();
			KickStarter.sceneChanger.OnAwake ();
			KickStarter.levelStorage.OnAwake ();
			
			KickStarter.playerMenus.OnStart ();
			KickStarter.options.OnStart ();
			KickStarter.runtimeVariables.OnStart ();
			KickStarter.runtimeInventory.OnStart ();
			KickStarter.saveSystem.OnStart ();
		}


		/** The current state of the game (Normal, Cutscene, Paused, DialogOptions) */
		public GameState gameState
		{
			get
			{
				return _gameState;
			}
			set
			{
				if (KickStarter.mainCamera)
				{
					KickStarter.mainCamera.CancelPauseGame ();
				}
				_gameState = value;
			}
		}


		/**
		 * Alerts the StateHandler that a Game Engine prefab is present in the scene.
		 * This is called from KickStarter when the game begins - the StateHandler will not run until this is done.
		 */
		public void RegisterWithGameEngine ()
		{
			if (!hasGameEngine)
			{
				hasGameEngine = true;
			}
		}


		/**
		 * Alerts the StateHandler that a Game Engine prefab is no longer present in the scene.
		 * This is called from KickStarter's OnDestroy function.
		 */
		public void UnregisterWithGameEngine ()
		{
			hasGameEngine = false;
		}


		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			GetReferences ();
		}


		/**
		 * <summary>Runs the ActionListAsset defined in SettingsManager's actionListOnStart when the game begins.</summary>
		 * <returns>True if an ActionListAsset was run</returns>
		 */
		public bool PlayGlobalOnStart ()
		{
			if (playedGlobalOnStart)
			{
				return false;
			}

			if (KickStarter.settingsManager.actionListOnStart)
			{
				AdvGame.RunActionListAsset (KickStarter.settingsManager.actionListOnStart);
				playedGlobalOnStart = true;
				return true;
			}

			return false;
		}


		/**
		 * Allows the ActionListAsset defined in SettingsManager's actionListOnStart to be run again.
		 */
		public void CanGlobalOnStart ()
		{
			playedGlobalOnStart = false;
		}


		private void GetReferences ()
		{
			inScriptedCutscene = false;
			if (KickStarter.settingsManager != null && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}
			GatherObjects ();
		}


		/**
		 * <summary>Re-sets the internal arrays of GameObjects used to mass-update Adventure Creator each frame.
		 * It should be called whenever a GameObject is added to or removed from the scene.</summary>
		 * <param name = "afterDelete">If False, then IgnoreNavMeshCollisions() will also be run</param>
		 */
		public void GatherObjects (bool afterDelete = false)
		{
			dragBases = FindObjectsOfType (typeof (DragBase)) as DragBase[];
			hotspots = FindObjectsOfType (typeof (Hotspot)) as Hotspot[];
			arrowPrompts = FindObjectsOfType (typeof (ArrowPrompt)) as ArrowPrompt[];
			parallax2Ds = FindObjectsOfType (typeof (Parallax2D)) as Parallax2D[];
			highlights = FindObjectsOfType (typeof (Highlight)) as Highlight[];
			cameras = FindObjectsOfType (typeof (_Camera)) as _Camera[];
			sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
			limitVisibilitys = FindObjectsOfType (typeof (LimitVisibility)) as LimitVisibility[];
			characters = FindObjectsOfType (typeof (Char)) as Char[];

			if (!afterDelete)
			{
				IgnoreNavMeshCollisions ();
			}

			if (KickStarter.sceneSettings != null)
			{
				KickStarter.sceneSettings.UpdateAllSortingMaps ();
			}
		}


		/**
		 * Calls Physics.IgnoreCollision on all appropriate Collider combinations (Unity 5 only).
		 */
		public void IgnoreNavMeshCollisions ()
		{
			#if UNITY_5
			Collider[] allColliders = FindObjectsOfType (typeof(Collider)) as Collider[];
			NavMeshBase[] navMeshes = FindObjectsOfType (typeof(NavMeshBase)) as NavMeshBase[];

			for (_i=0; _i<navMeshes.Length; _i++)
			{
				if (navMeshes[_i].ignoreCollisions)
				{
					Collider _collider = navMeshes[_i].GetComponent <Collider>();
					if (_collider != null && _collider.enabled && _collider.gameObject.activeInHierarchy)
					{
						foreach (Collider otherCollider in allColliders)
						{
							if (!_collider.isTrigger && !otherCollider.isTrigger && otherCollider.enabled && otherCollider.gameObject.activeInHierarchy && !(_collider is TerrainCollider) && _collider != otherCollider)
							{
								Physics.IgnoreCollision (_collider, otherCollider);
							}
						}
					}
				}
			}
			#endif
		}


		private void Update ()
		{
			if (isACDisabled || !hasGameEngine)
			{
				return;
			}

			if (KickStarter.settingsManager.IsInLoadingScene () || KickStarter.sceneChanger.IsLoading ())
			{
				if (!menuIsOff)
				{
					KickStarter.playerMenus.UpdateLoadingMenus ();
				}
				return;
			}

			if (gameState != GameState.Paused)
			{
				lastNonPausedState = gameState;
			}
			if (!inputIsOff)
			{
				if (gameState == GameState.DialogOptions)
				{
					KickStarter.playerInput.DetectConversationInputs ();
				}
				KickStarter.playerInput.UpdateInput ();

				if (gameState == GameState.Normal)
				{
					KickStarter.playerInput.UpdateDirectInput ();
				}

				if (gameState != GameState.Paused)
				{
					KickStarter.playerQTE.UpdateQTE ();
				}
			}

			KickStarter.dialog._Update ();

			if (!cursorIsOff)
			{
				KickStarter.playerCursor.UpdateCursor ();

				if (gameState == GameState.Normal && KickStarter.settingsManager && KickStarter.settingsManager.hotspotIconDisplay != HotspotIconDisplay.Never)
				{
					for (_i=0; _i<hotspots.Length; _i++)
					{
						hotspots[_i].UpdateIcon ();
						if (KickStarter.settingsManager.hotspotDrawing == ScreenWorld.WorldSpace)
						{
							hotspots[_i].DrawHotspotIcon (true);
						}
					}
				}
			}

			if (!menuIsOff)
			{
				KickStarter.playerMenus.CheckForInput ();
			}

			if (!menuIsOff)
			{
				if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.playerInput.GetMouseState () != MouseState.Normal)
				{
					KickStarter.playerMenus.UpdateAllMenus ();
				}
			}

			if (!interactionIsOff)
			{
				KickStarter.playerInteraction.UpdateInteraction ();

				for (_i=0; _i<highlights.Length; _i++)
				{
					highlights[_i]._Update ();
				}

				if (KickStarter.settingsManager.hotspotDetection == HotspotDetection.MouseOver && KickStarter.settingsManager.scaleHighlightWithMouseProximity)
				{
					bool setProximity = (gameState == AC.GameState.Normal) ? true : false;
					for (_i=0; _i<hotspots.Length; _i++)
					{
						hotspots[_i].SetProximity (setProximity);
					}
				}
			}

			if (!menuIsOff)
			{
				KickStarter.playerMenus.UpdateAllMenus ();
			}

			KickStarter.actionListManager.UpdateActionListManager ();

			if (!movementIsOff)
			{
				for (_i=0; _i<dragBases.Length; _i++)
				{
					dragBases[_i].UpdateMovement ();
				}

				if (gameState == GameState.Normal && KickStarter.settingsManager && KickStarter.settingsManager.movementMethod != MovementMethod.None)
				{
					KickStarter.playerMovement.UpdatePlayerMovement ();
				}

				KickStarter.playerMovement.UpdateFPCamera ();
			}

			if (!interactionIsOff)
			{
				KickStarter.playerInteraction.UpdateInventory ();
			}

			for (_i=0; _i<limitVisibilitys.Length; _i++)
			{
				limitVisibilitys[_i]._Update ();
			}

			for (_i=0; _i<sounds.Length; _i++)
			{
				sounds[_i]._Update ();
			}

			for (_i=0; _i<characters.Length; _i++)
			{
				if (characters[_i] != null && (!playerIsOff || !(characters[_i] is Player)))
				{
					characters[_i]._Update ();
				}
			}

			if (!cameraIsOff)
			{
				for (_i=0; _i<cameras.Length; _i++)
				{
					cameras[_i]._Update ();
				}
			}

			if (HasGameStateChanged ())
			{
				KickStarter.eventManager.Call_OnChangeGameState (previousUpdateState);

				if (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson)
				{
					if (gameState == GameState.Normal || (gameState == GameState.DialogOptions && KickStarter.settingsManager.useFPCamDuringConversations))
					{
						KickStarter.mainCamera.SetFirstPerson ();
					}
				}

				if (gameState != GameState.Paused && Time.time > 0f)
				{
					AudioListener.pause = false;
				}

				if (gameState == GameState.Cutscene && previousUpdateState != GameState.Cutscene)
				{
					KickStarter.playerMenus.MakeUINonInteractive ();
				}
				else if (gameState != GameState.Cutscene && previousUpdateState == GameState.Cutscene)
				{
					KickStarter.playerMenus.MakeUIInteractive ();
				}

				KickStarter.sceneSettings.OnStateChange ();
			}

			previousUpdateState = gameState;
		}


		private void LateUpdate ()
		{
			if (isACDisabled || !hasGameEngine)
			{
				return;
			}

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			for (_i=0; _i<characters.Length; _i++)
			{
				if (!playerIsOff || !(characters[_i] is Player))
				{
					characters[_i]._LateUpdate ();
				}
			}

			if (!cameraIsOff)
			{
				KickStarter.mainCamera._LateUpdate ();
			}

			for (_i=0; _i<parallax2Ds.Length; _i++)
			{
				parallax2Ds[_i].UpdateOffset ();
			}
		}


		private void FixedUpdate ()
		{
			if (isACDisabled || !hasGameEngine)
			{
				return;
			}

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			for (_i=0; _i<characters.Length; _i++)
			{
				if (!playerIsOff || !(characters[_i] is Player))
				{
					characters[_i]._FixedUpdate ();
				}
			}

			if (!cameraIsOff)
			{
				for (_i=0; _i<cameras.Length; _i++)
				{
					cameras[_i]._FixedUpdate ();
				}
			}

			KickStarter.dialog._FixedUpdate ();
		}


		/**
		 * Sets the maximum volume of all Sound objects in the scene.
		 */
		public void UpdateAllMaxVolumes ()
		{
			foreach (Sound sound in sounds)
			{
				sound.SetMaxVolume ();
			}
		}


		private bool HasGameStateChanged ()
		{
			if (previousUpdateState != gameState)
			{
				return true;
			}
			return false;
		}


		private void OnGUI ()
		{
			if (isACDisabled || !hasGameEngine)
			{
				return;
			}

			if (KickStarter.settingsManager.IsInLoadingScene () || KickStarter.sceneChanger.IsLoading ())
			{
				if (!cameraIsOff && !KickStarter.settingsManager.IsInLoadingScene ())
				{
					KickStarter.mainCamera.DrawCameraFade ();
				}
				if (!menuIsOff)
				{
					KickStarter.playerMenus.DrawLoadingMenus ();
				}
				if (!cameraIsOff)
				{
					KickStarter.mainCamera.DrawBorders ();
				}
				return;
			}

			if (!cursorIsOff && gameState == GameState.Normal && KickStarter.settingsManager)
			{
				if (KickStarter.settingsManager.hotspotIconDisplay != HotspotIconDisplay.Never &&
				   KickStarter.settingsManager.hotspotDrawing == ScreenWorld.ScreenSpace)
				{
					for (_i=0; _i<hotspots.Length; _i++)
					{
						hotspots[_i].DrawHotspotIcon ();
					}
				}

				for (_i=0; _i<dragBases.Length; _i++)
				{
					dragBases[_i].DrawGrabIcon ();
				}
			}

			if (!inputIsOff)
			{
				if (gameState == GameState.DialogOptions)
				{
					KickStarter.playerInput.DetectConversationNumerics ();
				}
				KickStarter.playerInput.DrawDragLine ();

				for (_i=0; _i<arrowPrompts.Length; _i++)
				{
					arrowPrompts[_i].DrawArrows ();
				}
			}

			if (!menuIsOff)
			{
				KickStarter.playerMenus.DrawMenus ();
			}

			if (!cursorIsOff)
			{
				if (KickStarter.cursorManager.cursorRendering == CursorRendering.Software)
				{
					KickStarter.playerCursor.DrawCursor ();
				}
			}

			if (!cameraIsOff)
			{
				KickStarter.mainCamera.DrawCameraFade ();
				KickStarter.mainCamera.DrawBorders ();
			}
		}


		/**
		 * <summary>Gets the last value of gameState that wasn't GameState.Paused.</summary>
		 * <returns>The last value of gameState that wasn't GameState.Paused</summary>
		 */
		public GameState GetLastNonPausedState ()
		{
			return lastNonPausedState;
		}
		

		/**
		 * Restores the gameState to its former state after un-pausing the game.
		 */
		public void RestoreLastNonPausedState ()
		{
			if (Time.timeScale == 0f)
			{
				KickStarter.sceneSettings.UnpauseGame (KickStarter.playerInput.timeScale);
			}

			if (KickStarter.playerInteraction.inPreInteractionCutscene)
			{
				gameState = GameState.Cutscene;
				return;
			}

			KickStarter.playerInteraction.inPreInteractionCutscene = false;
			if (KickStarter.actionListManager.IsGameplayBlocked () || inScriptedCutscene)
			{
				gameState = GameState.Cutscene;
			}
			else if (KickStarter.playerInput.activeConversation != null)
			{
				gameState = GameState.DialogOptions;
			}
			else
			{
				gameState = GameState.Normal;
			}
		}


		/**
		 * <summary>Goes through all Hotspots in the scene, and limits their enabed state based on a specific _Camera, if appropriate.</summary>
		 * <param name = "_camera">The _Camera to attempt to limit all Hotspots to</param>
		 */
		public void LimitHotspotsToCamera (_Camera _camera)
		{
			if (_camera != null)
			{
				for (_i=0; _i<hotspots.Length; _i++)
				{
					hotspots[_i].LimitToCamera (_camera);
				}
			}
		}


		/**
		 * Begins a hard-coded cutscene.
		 * Gameplay will resume once EndCutscene() is called.
		 */
		public void StartCutscene ()
		{
			inScriptedCutscene = true;
			gameState = GameState.Cutscene;
		}


		/**
		 * Ends a hard-coded cutscene, started by calling StartCutscene().
		 */
		public void EndCutscene ()
		{
			inScriptedCutscene = false;
			if (KickStarter.playerMenus.ArePauseMenusOn (null))
			{
				KickStarter.mainCamera.PauseGame ();
			}
			else
			{
				KickStarter.stateHandler.RestoreLastNonPausedState ();
			}
		}


		/**
		 * <summary>Checks if the game is currently in a user-scripted cutscene.</summary>
		 * <returns>True if the game is currently in a user-scripted cutscene</returns>
		 */
		public bool IsInScriptedCutscene ()
		{
			return inScriptedCutscene;
		}


		/**
		 * <summary>Enables or disables Adventure Creator completely.</summary>
		 * <param name = "state">If True, then Adventure Creator will be enabled. If False, then Adventure Creator will be disabled.</param>
		 */
		public void SetACState (bool state)
		{
			isACDisabled = !state;
		}


		public bool IsACEnabled ()
		{
			return !isACDisabled;
		}


		/**
		 * Backs up the state of the menu and cursor systems, and disables them, before taking a screenshot.
		 */
		public void PreScreenshotBackup ()
		{
			originalMenuState = menuIsOff;
			originalCursorState = cursorIsOff;
			menuIsOff = true;
			cursorIsOff = true;

			foreach (Menu menu in PlayerMenus.GetMenus ())
			{
				menu.PreScreenshotBackup ();
			}
		}


		/**
		 * Restores the menu and cursor systems to their former states, after taking a screenshot.
		 */
		public void PostScreenshotBackup ()
		{
			menuIsOff = originalMenuState;
			cursorIsOff = originalCursorState;

			foreach (Menu menu in PlayerMenus.GetMenus ())
			{
				menu.PostScreenshotBackup ();
			}
		}


		/**
		 * <summary>Sets the enabled state of the PlayerCursor system.</summary>
		 * <param name = "state">If True, the PlayerCursor system will be enabled</param>
		 */
		public void SetCursorSystem (bool state)
		{
			cursorIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerInput system.</summary>
		 * <param name = "state">If True, the PlayerInput system will be enabled</param>
		 */
		public void SetInputSystem (bool state)
		{
			inputIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the cursor system.</summary>
		 * <param name = "state">If True, the cursor system will be enabled</param>
		 */
		public void SetInteractionSystem (bool state)
		{
			interactionIsOff = !state;

			if (!state)
			{
				KickStarter.playerInteraction.DeselectHotspot (true);
			}
		}


		/**
		 * <summary>Checks if the interaction system is enabled.</summary>
		 * <returns>True if the interaction system is enabled</returns>
		 */
		public bool CanInteract ()
		{
			return !interactionIsOff;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerMenus system.</summary>
		 * <param name = "state">If True, the PlayerMenus system will be enabled</param>
		 */
		public void SetMenuSystem (bool state)
		{
			menuIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerMovement system.</summary>
		 * <param name = "state">If True, the PlayerMovement system will be enabled</param>
		 */
		public void SetMovementSystem (bool state)
		{
			movementIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the MainCamera system.</summary>
		 * <param name = "state">If True, the MainCamera system will be enabled</param>
		 */
		public void SetCameraSystem (bool state)
		{
			cameraIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the trigger system.</summary>
		 * <param name = "state">If True, the trigger system will be enabled</param>
		 */
		public void SetTriggerSystem (bool state)
		{
			triggerIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the Player system.</summary>
		 * <param name = "state">If True, the Player system will be enabled</param>
		 */
		public void SetPlayerSystem (bool state)
		{
			playerIsOff = !state;
		}


		/**
		 * <summary>Checks if the trigger system is disabled.</summary>
		 * <returns>True if the trigger system is disabled</returns>
		 */
		public bool AreTriggersDisabled ()
		{
			return triggerIsOff;
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			mainData.cursorIsOff = cursorIsOff;
			mainData.inputIsOff = inputIsOff;
			mainData.interactionIsOff = interactionIsOff;
			mainData.menuIsOff = menuIsOff;
			mainData.movementIsOff = movementIsOff;
			mainData.cameraIsOff = cameraIsOff;
			mainData.triggerIsOff = triggerIsOff;
			mainData.playerIsOff = playerIsOff;

			if (music != null)
			{
				mainData = music.SaveMainData (mainData);
			}

			return mainData;
		}


		/**
		 * <summary>Updates its own variables from a MainData class.</summary>
		 * <param name = "mainData">The MainData class to load from</param>
		 */
		public void LoadMainData (MainData mainData)
		{
			cursorIsOff = mainData.cursorIsOff;
			inputIsOff = mainData.inputIsOff;
			interactionIsOff = mainData.interactionIsOff;
			menuIsOff = mainData.menuIsOff;
			movementIsOff = mainData.movementIsOff;
			cameraIsOff = mainData.cameraIsOff;
			triggerIsOff = mainData.triggerIsOff;
			playerIsOff = mainData.playerIsOff;

			if (mainData.musicQueueData != null && mainData.musicQueueData.Length > 0)
			{
				if (music == null)
				{
					CreateMusicEngine ();
				}
				music.LoadMainData (mainData);
			}
			else if (music != null)
			{
				music.StopAll (0f);
			}
		}


		private void CreateMusicEngine ()
		{
			if (music == null)
			{
				GameObject musicOb = (GameObject) Instantiate (Resources.Load (Resource.musicEngine));
				if (musicOb != null)
				{
					musicOb.name = AdvGame.GetName (Resource.musicEngine);
					if (GameObject.Find ("_Sound") && GameObject.Find ("_Sound").transform.parent == null)
					{
						musicOb.transform.parent = GameObject.Find ("_Sound").transform;
					}
					music = musicOb.GetComponent <Music>();
				}
				else
				{
					ACDebug.LogError ("Cannot find MusicEngine prefab in /AdventureCreator/Resrouces - did you import AC completely?");
				}

				GatherObjects ();
			}
		}


		/**
		 * <summary>Gets the Music component used to handle AudioClips played using the 'Sound: Play music' Action.</summary>
		 * <returns>The Music component used to handle AudioClips played using the 'Sound: Play music' Action.</returns>
		 */
		public Music GetMusicEngine ()
		{
			if (music == null)
			{
				CreateMusicEngine ();
			}
			return music;
		}

	}

}