/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionListManager.cs"
 * 
 *	This script keeps track of which ActionLists are running in a scene.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component keeps track of which ActionLists are running.
	 * When an ActionList runs or ends, it is passed to this script, which sets up the correct GameState in StateHandler.
	 * It should be placed on the GameEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_action_list_manager.html")]
	#endif
	public class ActionListManager : MonoBehaviour
	{

		/** If True, then the next time ActionConversation's Skip() function is called, it will be ignored */
		[HideInInspector] public bool ignoreNextConversationSkip = false;

		private bool playCutsceneOnVarChange = false;
		private bool saveAfterCutscene = false;

		private int playerIDOnStartQueue;
		private List<ActiveList> activeLists = new List<ActiveList>();

		
		public void OnAwake ()
		{
			activeLists.Clear ();
		}
		

		/**
		 * Checks for autosaving and changed variables.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateActionListManager ()
		{
			if (saveAfterCutscene && !IsGameplayBlocked ())
			{
				saveAfterCutscene = false;
				SaveSystem.SaveAutoSave ();
			}
			
			if (playCutsceneOnVarChange && KickStarter.stateHandler && (KickStarter.stateHandler.gameState == GameState.Normal || KickStarter.stateHandler.gameState == GameState.DialogOptions))
			{
				playCutsceneOnVarChange = false;
				
				if (KickStarter.sceneSettings.cutsceneOnVarChange != null)
				{
					KickStarter.sceneSettings.cutsceneOnVarChange.Interact ();
				}
			}
		}
		

		/**
		 * Ends all skippable ActionLists.
		 * This is triggered when the user presses the "EndCutscene" Input button.
		 */
		public void EndCutscene ()
		{
			if (!IsGameplayBlocked ())
			{
				return;
			}

			if (AdvGame.GetReferences ().settingsManager.blackOutWhenSkipping)
			{
				KickStarter.mainCamera.HideScene ();
			}
			
			// Stop all non-looping sound
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

			// Set correct Player prefab
			if (KickStarter.player != null && playerIDOnStartQueue != KickStarter.player.ID && playerIDOnStartQueue >= 0)
			{
				Player playerToRevertTo = KickStarter.settingsManager.GetPlayer (playerIDOnStartQueue);
				KickStarter.ResetPlayer (playerToRevertTo, playerIDOnStartQueue, true, Quaternion.identity, false, true);
			}

			for (int i=0; i<activeLists.Count; i++)
			{
				if (!activeLists[i].inSkipQueue && activeLists[i].actionList.IsSkippable ())
				{
					// Kill, but do isolated, to bypass setting GameState etc
					activeLists[i].Reset (true);
				}
				else
				{
					activeLists[i].Skip ();
				}
			}

			for (int i=0; i<KickStarter.actionListAssetManager.activeLists.Count; i++)
			{
				if (!KickStarter.actionListAssetManager.activeLists[i].inSkipQueue && KickStarter.actionListAssetManager.activeLists[i].actionList.IsSkippable ())
				{
					// Kill, but do isolated, to bypass setting GameState etc
					KickStarter.actionListAssetManager.activeLists[i].Reset (true);
				}
				else
				{
					KickStarter.actionListAssetManager.activeLists[i].Skip ();
				}
			}
		}
			

		/**
		 * <summary>Checks if a particular ActionList is running.</summary>
		 * <param name = "actionList">The ActionList to search for</param>
		 * <returns>True if the ActionList is currently running</returns>
		 */
		public bool IsListRunning (ActionList actionList)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionList) && activeList.IsRunning ())
				{
					return true;
				}
			}
			
			return false;
		}


		/**
		 * <summary>Checks if any currently-running ActionLists pause gameplay.</summary>
		 * <param title = "_actionToIgnore">Any ActionList that contains this Action will be excluded from the check</param>
		 * <returns>True if any currently-running ActionLists pause gameplay</returns>
		 */
		public bool IsGameplayBlocked (Action _actionToIgnore = null)
		{
			if (KickStarter.stateHandler.IsInScriptedCutscene ())
			{
				return true;
			}
			
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.actionList.actionListType == ActionListType.PauseGameplay && activeList.IsRunning ())
				{
					if (_actionToIgnore != null)
					{
						if (activeList.actionList.actions.Contains (_actionToIgnore))
						{
							continue;
						}
					}
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.actionList != null && activeList.actionList.actionListType == ActionListType.PauseGameplay && activeList.IsRunning ())
				{
					if (_actionToIgnore != null)
					{
						if (activeList.actionList.actions.Contains (_actionToIgnore))
						{
							continue;
						}
					}
					return true;
				}
			}

			return false;
		}
		
		
		/**
		 * <summary>Checks if any currently-running ActionListAssets pause gameplay and unfreeze 'Pause' Menus.</summary>
		 * <returns>True if any currently-running ActionListAssets pause gameplay and unfreeze 'Pause' Menus.</returns>
		 */
		public bool IsGameplayBlockedAndUnfrozen ()
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.CanUnfreezePauseMenus ())
				{
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.CanUnfreezePauseMenus ())
				{
					return true;
				}
			}
			return false;
		}
		
		
		/**
		 * <summary>Checks if any skippable ActionLists are currently running.</summary>
		 * <returns>True if any skippable ActionLists are currently running.</returns>
		 */
		public bool IsInSkippableCutscene ()
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsRunning () && activeList.inSkipQueue)
				{
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.IsRunning () && activeList.inSkipQueue)
				{
					return true;
				}
			}
			
			return false;
		}


		#if UNITY_EDITOR

		private Rect debugWindowRect = new Rect (0, 0, 220, 500);

		private void OnGUI ()
		{
			if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().settingsManager != null && AdvGame.GetReferences ().settingsManager.showActiveActionLists)
			{
				debugWindowRect.height = 21f;
				debugWindowRect = GUILayout.Window (0, debugWindowRect, StatusWindow, "AC status", GUILayout.Width (220));
			}
		}


		private void StatusWindow (int windowID)
		{
			GUISkin testSkin = (GUISkin) Resources.Load ("SceneManagerSkin");
			GUI.skin = testSkin;
			
			GUILayout.Label ("Current game state: " + KickStarter.stateHandler.gameState.ToString ());
			
			if (KickStarter.stateHandler.gameState == GameState.DialogOptions && KickStarter.playerInput.activeConversation != null)
			{
				if (GUILayout.Button ("Conversation: " + KickStarter.playerInput.activeConversation.gameObject.name))
				{
					UnityEditor.EditorGUIUtility.PingObject (KickStarter.playerInput.activeConversation.gameObject);
				}
			}
			
			GUILayout.Space (4f);

			bool anyRunning = false;
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsRunning ())
				{
					anyRunning = true;
					break;
				}
			}

			if (anyRunning)
			{
				GUILayout.Label ("ActionLists running:");
				
				foreach (ActiveList activeList in activeLists)
				{
					activeList.ShowGUI ();
				}
			}

			anyRunning = false;
			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.IsRunning ())
				{
					anyRunning = true;
					break;
				}
			}

			if (anyRunning)
			{
				GUILayout.Label ("ActionList Assets running:");
				
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
				{
					activeList.ShowGUI ();
				}
			}

			if (IsGameplayBlocked ())
			{
				GUILayout.Space (4f);
				GUILayout.Label ("Gameplay is blocked");
			}
		}

		#endif


		/**
		 * <summary>Adds a new ActionList, assumed to already be running, to the internal record of currently-running ActionLists, and sets the correct GameState in StateHandler.</summary>
		 * <param name = "actionList">The ActionList to run</param>
		 * <param name = "addToSkipQueue">If True, then the ActionList will be added to the list of ActionLists to skip</param>
		 * <param name = "_startIndex">The index number of the Action to start skipping from, if addToSkipQueue = True</param>
		 * <param name = "actionListAsset">The ActionListAsset that is the ActionList's source, if it has one.</param>
		 */
		public void AddToList (ActionList actionList, bool addToSkipQueue, int _startIndex)
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionList))
				{
					activeLists.RemoveAt (i);
				}
			}

			addToSkipQueue = CanAddToSkipQueue (actionList, addToSkipQueue);
			activeLists.Add (new ActiveList (actionList, addToSkipQueue, _startIndex));

			if (actionList is RuntimeActionList && actionList.actionListType == ActionListType.PauseGameplay && !actionList.unfreezePauseMenus && KickStarter.playerMenus.ArePauseMenusOn (null))
			{
				// Don't affect the gamestate if we want to remain frozen
				return;
			}

			SetCorrectGameState ();
		}
		

		/**
		 * <summary>Resets and removes a ActionList from the internal record of currently-running ActionLists, and sets the correct GameState in StateHandler.</summary>
		 * <param name = "actionList">The ActionList to end</param>
		 */
		public void EndList (ActionList actionList)
		{
			if (actionList == null)
			{
				return;
			}

			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionList))
				{
					EndList (activeLists[i]);
				}
			}
		}


		/**
		 * <summary>Ends the ActionList or ActionListAsset associated with a given ActiveList data container</summary>
		 * <param name = "activeList">The ActiveList associated with the ActionList or ActionListAsset to end.</param>
		 */
		public void EndList (ActiveList activeList)
		{
			activeList.Reset (false);

			if (activeList.GetConversationOnEnd ())
			{
				ResetSkipVars ();
				activeList.RunConversation ();
			}
			else
			{
				if (activeList.actionListAsset != null && activeList.actionList.actionListType == ActionListType.PauseGameplay && !activeList.actionList.unfreezePauseMenus && KickStarter.playerMenus.ArePauseMenusOn (null))
				{
					// Don't affect the gamestate if we want to remain frozen
					if (KickStarter.stateHandler.gameState != GameState.Cutscene)
					{
						ResetSkipVars ();
					}
				}
				else
				{
					SetCorrectGameStateEnd ();
				}
			}
			
			if (activeList.actionList.autosaveAfter)
			{
				if (!IsGameplayBlocked ())
				{
					SaveSystem.SaveAutoSave ();
				}
				else
				{
					saveAfterCutscene = true;
				}
			}
		}


		/**
		 * Inform ActionListManager that a Variable's value has changed.
		 */
		public void VariableChanged ()
		{
			playCutsceneOnVarChange = true;
		}


		/**
		 * Ends all currently-running ActionLists and ActionListAssets.
		 */
		public void KillAllLists ()
		{
			foreach (ActiveList activeList in activeLists)
			{
				activeList.Reset (true);
			}
			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				activeList.Reset (true);
			}
		}
		

		/**
		 * Ends all currently-running ActionLists and ActionListAssets.
		 */
		public static void KillAll ()
		{
			KickStarter.actionListManager.KillAllLists ();
		}


		public void KillAllFromScene (SceneInfo sceneInfo)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.actionList != null && sceneInfo.Matches (UnityVersionHandler.GetSceneInfoFromGameObject (activeList.actionList.gameObject)) && activeList.actionListAsset == null)
				{
					activeList.Reset (true);
				}
			}
		} 

		
		private void SetCorrectGameStateEnd ()
		{
			if (KickStarter.stateHandler != null)
			{
				if (KickStarter.playerMenus.ArePauseMenusOn (null))
				{
					KickStarter.mainCamera.PauseGame ();
				}
				else
				{
					KickStarter.stateHandler.RestoreLastNonPausedState ();
				}

				if (KickStarter.stateHandler.gameState != GameState.Cutscene)
				{
					ResetSkipVars ();
				}
			}
			else
			{
				ACDebug.LogWarning ("Could not set correct GameState!");
			}

			PurgeLists ();
		}


		private void PurgeLists ()
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (!activeLists[i].IsNecessary ())
				{
					activeLists.RemoveAt (i);
					i--;
				}
			}
			for (int i=0; i<KickStarter.actionListAssetManager.activeLists.Count; i++)
			{
				if (!KickStarter.actionListAssetManager.activeLists[i].IsNecessary ())
				{
					KickStarter.actionListAssetManager.activeLists.RemoveAt (i);
					i--;
				}
			}
		}


		private void ResetSkipVars ()
		{
			if (!IsGameplayBlocked ())
			{
				ignoreNextConversationSkip = false;
				foreach (ActiveList activeList in activeLists)
				{
					activeList.inSkipQueue = false;
				}
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
				{
					activeList.inSkipQueue = false;
				}

				GlobalVariables.BackupAll ();
				KickStarter.localVariables.BackupAllValues ();
			}
		}


		/**
		 * Sets the StateHandler's gameState variable to the correct value, based on what ActionLists are currently running.
		 */
		public void SetCorrectGameState ()
		{
			if (KickStarter.stateHandler != null)
			{
				if (IsGameplayBlocked ())
				{
					if (KickStarter.stateHandler.gameState != GameState.Cutscene)
					{
						ResetSkipVars ();
					}
					KickStarter.stateHandler.gameState = GameState.Cutscene;

					if (IsGameplayBlockedAndUnfrozen ())
					{
						KickStarter.sceneSettings.UnpauseGame (KickStarter.playerInput.timeScale);
					}
				}
				else if (KickStarter.playerMenus.ArePauseMenusOn (null))
				{
					KickStarter.stateHandler.gameState = GameState.Paused;
					KickStarter.sceneSettings.PauseGame ();
				}
				else
				{
					if (KickStarter.playerInput.activeConversation != null)
					{
						KickStarter.stateHandler.gameState = GameState.DialogOptions;
					}
					else
					{
						KickStarter.stateHandler.gameState = GameState.Normal;
					}
				}
			}
			else
			{
				ACDebug.LogWarning ("Could not set correct GameState!");
			}
		}
		

		private void OnDestroy ()
		{
			activeLists.Clear ();
		}


		/**
		 * <summary>Sets the point to continue from, when a Conversation's options are overridden by an ActionConversation.</summary>
		 * <param title = "actionConversation">The "Dialogue: Start conversation" Action that is overriding the Conversation's options</param>
		 */
		public void SetConversationPoint (ActionConversation actionConversation)
		{
			foreach (ActiveList activeList in activeLists)
			{
				activeList.SetConversationOverride (actionConversation);
			}
			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				activeList.SetConversationOverride (actionConversation);
			}
		}


		/**
		 * <summary>Attempts to override a Conversation object's default options by resuming an ActionList from the last ActionConversation.</summary>
		 * <param title = "optionIndex">The index number of the chosen dialogue option.</param>
		 * <returns>True if the override was succesful.</returns>
		 */
		public bool OverrideConversation (int optionIndex)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.ResumeConversationOverride ())
				{
					return true;
				}
			}
			foreach (ActiveList activeList in KickStarter.actionListAssetManager.activeLists)
			{
				if (activeList.ResumeConversationOverride ())
				{
					return true;
				}
			}

			return false;
		}


		/**
		 * <summary>Checks if a given ActionList should be skipped when the 'EndCutscene' input is triggered.</summary>
		 * <param name = "actionList">The ActionList to check</param>
		 * <param name = "originalValue">If True, the user would like it to be skippable.</param>
		 * <returns>True if the ActionList can be skipped.</returns>
		 */
		public bool CanAddToSkipQueue (ActionList actionList, bool originalValue)
		{
			if (!actionList.IsSkippable ())
			{
				return false;
			}
			else if (!KickStarter.actionListManager.IsInSkippableCutscene ())
			{
				if (KickStarter.player)
				{
					playerIDOnStartQueue = KickStarter.player.ID;
				}
				else
				{
					playerIDOnStartQueue = -1;
				}
				return true;
			}
			return originalValue;
		}


		/**
		 * <summary>Records the Action indices that the associated ActionList was running before being paused. This data is sent to the ActionList's associated ActiveList</summary>
		 * <param name = "actionList">The ActionList that is being paused</param>
		 * <param name = "resumeIndices">An array of Action indices to run when the ActionList is resumed</param>
		 */
		public void AssignResumeIndices (ActionList actionList, int[] resumeIndices)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionList))
				{
					activeList.SetResumeIndices (resumeIndices);
				}
			}
		}


		/**
		 * <summary>Resumes a previously-paused ActionList. If the ActionList is already running, nothing will happen.</summary>
		 * <param name = "actionList">The ActionList to pause</param>
		 */
		public void Resume (ActionList actionList)
		{
			if (IsListRunning (actionList))
			{
				return;
			}

			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionList))
				{
					activeLists[i].Resume ();
					return;
				}
			}

			actionList.Interact ();
		}


		/**
		 * <summary>Generates a save-able string out of the ActionList resume data.<summary>
		 * <param name = "If set, only data for a given subscene will be saved. If null, only data for the active scene will be saved</param>
		 * <returns>A save-able string out of the ActionList resume data<returns>
		 */
		public string GetSaveData (SubScene subScene = null)
		{
			PurgeLists ();
			string localResumeData = "";
			for (int i=0; i<activeLists.Count; i++)
			{
				localResumeData += activeLists[i].GetSaveData (subScene);

				if (i < (activeLists.Count - 1))
				{
					localResumeData += SaveSystem.pipe;
				}
			}
			return localResumeData;
		}


		/**
		 * <summary>Recreates ActionList resume data from a saved data string.</summary>
		 * <param name = "If set, the data is for a subscene and so existing data will not be cleared.</param>
		 * <param name = "_localResumeData">The saved data string</param>
		 */
		public void LoadData (string _dataString, SubScene subScene = null)
		{
			if (subScene == null)
			{
				activeLists.Clear ();
			}

			if (_dataString != null && _dataString.Length > 0)
			{
				string[] dataArray = _dataString.Split (SaveSystem.pipe[0]);
				foreach (string chunk in dataArray)
				{
					ActiveList activeList = new ActiveList ();
					if (activeList.LoadData (chunk, subScene))
					{
						activeLists.Add (activeList);
					}
				}
			}
		}
		
	}
	
}