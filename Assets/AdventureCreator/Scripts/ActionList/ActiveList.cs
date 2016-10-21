﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActiveList.cs"
 * 
 *	A container for data about ActionLists and ActionListAssets that have been run.  It stores information about what to skip, pause-points and current parameter data.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{
	
	/**
	 * A container for data about ActionLists and ActionListAssets that have been run.  It stores information about what to skip, pause-points and current parameter data.
	 */
	public class ActiveList
	{
		
		/** The ActionList this references */
		public ActionList actionList;
		/** The ActionListAsset this references */
		public ActionListAsset actionListAsset;
		/** The index number of the Action to skip from */
		public int startIndex;
		/** Whether or not the ActionList this class references should be skipped when 'EndCutscene' is triggered. */
		public bool inSkipQueue;

		private bool isRunning;
		private bool isConversationOverride;
		private int[] resumeIndices;
		private Conversation conversationOnEnd;
		private string parameterData;


		/**
		 * The default Constructor.
		 */
		public ActiveList ()
		{
			actionList = null;
			actionListAsset = null;
			conversationOnEnd = null;
			inSkipQueue = false;
			isRunning = false;
			resumeIndices = new int[0];
			parameterData = "";
		}


		/**
		 * <summary>A Constructor</summary>
		 * <param name = "_actionList">The ActionList that this class will store data for</param>
		 * <param name = "_inSkipQueue">Whether or not the ActionList will be skipped when 'EndCutscene' is triggered</param>
		 * <param name = "_startIndex">The index of Actions within the ActionList that it starts from when run</param>
		 */
		public ActiveList (ActionList _actionList, bool _inSkipQueue, int _startIndex)
		{
			actionList = _actionList;

			if (actionList.conversation)
			{
				conversationOnEnd = actionList.conversation;
			}
			
			if (actionList is RuntimeActionList)
			{
				RuntimeActionList runtimeActionList = (RuntimeActionList) actionList;
				actionListAsset = runtimeActionList.assetSource;
			}
			else
			{
				actionListAsset = null;
			}

			inSkipQueue = _inSkipQueue;
			startIndex = _startIndex;
			isRunning = true;
			resumeIndices = new int[0];
			parameterData = "";
		}


		/**
		 * <summary>Checks whether or not the associated ActionList is running.</summary>
		 * <returns>True if the associated ActionList is running</returns>
		 */
		public bool IsRunning ()
		{
			return isRunning;
		}


		/**
		 * <summary>Checks whether the class contains any useful information. If not, the ActionListManager will delete it.</summary>
		 * <returns>True if the class contains any useful information</returns>
		 */
		public bool IsNecessary ()
		{
			if (isRunning || inSkipQueue || isConversationOverride || resumeIndices.Length > 0)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Resets the associated ActionList.</summary>
		 * <param name = "removeFromSkipQueue">If True, then the associated ActionList will not be run when 'EndCutscene' is triggered</param>
		 */
		public void Reset (bool removeFromSkipQueue)
		{
			isRunning = false;
		
			if (actionList != null)
			{
				actionList.ResetList ();
				if (actionList is RuntimeActionList)
				{
					RuntimeActionList runtimeActionList = (RuntimeActionList) actionList;
					runtimeActionList.DestroySelf ();
				}
			}

			if (removeFromSkipQueue)
			{
				inSkipQueue = false;
			}
		}


		#if UNITY_EDITOR

		/**
		 * Shows some information about the associated ActionList, if it is running.
		 */
		public void ShowGUI ()
		{
			if (actionList != null && isRunning && GUILayout.Button (actionList.gameObject.name))
			{
				UnityEditor.EditorGUIUtility.PingObject (actionList.gameObject);
			}
		}

		#endif


		/**
		 * <summary>Checks if the class is linked to a specific ActionList.</summary>
		 * <param name = "_actionList">The ActionList to check against</param>
		 * <returns>True if the class is linked to the ActionList</returns>
		 */
		public bool IsFor (ActionList _actionList)
		{
			if (_actionList != null && actionList == _actionList)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the class is linked to a specific ActionListAsset.</summary>
		 * <param name = "_actionListAsset">The ActionListAsset to check against</param>
		 * <returns>True if the class is linked to the ActionListAsset</returns>
		 */
		public bool IsFor (ActionListAsset _actionListAsset)
		{
			if (_actionListAsset != null)
			{
				if (actionListAsset == _actionListAsset)
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Skips the associated ActionList.</summary>
		 */
		public void Skip ()
		{
			if (inSkipQueue)
			{
				if (actionListAsset != null)
				{
					// Destroy old list, but don't go through ActionListManager's Reset code, to bypass changing GameState etc
					KickStarter.actionListAssetManager.DestroyAssetList (actionListAsset);
					actionList = AdvGame.SkipActionListAsset (actionListAsset, startIndex);
				}
				else if (actionList != null)
				{
					actionList.Skip (startIndex);
				}
			}
		}


		/**
		 * <summary>Checks if the associated ActionList is capable of unfreezing pause Menus.</summary>
		 * <returns>True if the associated ActionList is capable of unfreezing pause Menus.</returns>
		 */
		public bool CanUnfreezePauseMenus ()
		{
			if (actionList.actionListType == ActionListType.PauseGameplay && actionListAsset != null && actionList.unfreezePauseMenus)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Resets the associated ActionList and records the index of an ActionConversation instance, if it is within the ActionList's list of Actions.</summary>
		 * <param name = "actionConversation">The ActionConversation to search for</param>
		 */
		public void SetConversationOverride (ActionConversation actionConversation)
		{
			if (actionList != null)
			{
				foreach (Action action in actionList.actions)
				{
					if (action == actionConversation)
					{
						startIndex = actionList.actions.IndexOf (action);
						isConversationOverride = true;
						Reset (true);
						return;
					}
				}
			}
		}


		/**
		 * <summary>Attempts to resume a Conversation, if the associated ActionList overrides it's handling.</summary>
		 * <returns>True if the ActionList was overriding a Conversation</returns>
		 */
		public bool ResumeConversationOverride ()
		{
			if (isConversationOverride)
			{
				isConversationOverride = false;

				if (actionListAsset != null)
				{
					actionList = AdvGame.RunActionListAsset (actionListAsset, startIndex, true);
				}
				else if (actionList != null)
				{
					actionList.Interact (startIndex, true);
				}

				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets the Conversation to run once the associated ActionList has finished running.</summary>
		 * <returns>The Conversation to run once the assocated ActionList has finished running</returns>
		 */
		public Conversation GetConversationOnEnd ()
		{
			if (conversationOnEnd != null)
			{
				if (KickStarter.stateHandler)
				{
					KickStarter.stateHandler.gameState = GameState.Cutscene;
				}
				else
				{
					ACDebug.LogWarning ("Could not set correct GameState!");
				}

				return conversationOnEnd;
			}
			return null;
		}


		/**
		 * <summary>Runs the Conversation set to do so when the associated ActionList has finished.</summary>
		 */
		public void RunConversation ()
		{
			conversationOnEnd.Interact ();
			conversationOnEnd = null;
		}


		/**
		 * <summary>Resumes the associated ActionList, if it had previously been paused.</summary>
		 * <param name = "runtimeActionListAsset">The RuntimeActionList to re-associate the class with</param>
		 */
		public void Resume (RuntimeActionList runtimeActionList = null)
		{
			if (runtimeActionList != null)
			{
				actionList = runtimeActionList;
				runtimeActionList.Resume (startIndex, resumeIndices, parameterData);
			}
			else
			{
				actionList.Resume (startIndex, resumeIndices, parameterData);
			}
		}


		/**
		 * <summary>Records the Action indices that the associated ActionList was running before being paused.</summary>
		 * <param name = "_resumeIndices">An array of Action indices to run when the ActionList is resumed</param>
		 */
		public void SetResumeIndices (int[] _resumeIndices)
		{
			List<int> resumeIndexList = new List<int>();
			foreach (int resumeIndex in _resumeIndices)
			{
				resumeIndexList.Add (resumeIndex);
			}
			resumeIndices = resumeIndexList.ToArray ();
		}


		/**
		 * <summary>Converts the class's data into a string that can be saved.</summary>
		 * <param name = "subScene">If set, only data for a given subscene will be saved. If null, only data for the active scene will be saved</param>
		 * <returns>The class's data, converted to a string</returns>
		 */
		public string GetSaveData (SubScene subScene)
		{
			string ID = "";
			string convID = "";

			if (isRunning)
			{
				// Unless ActionLists can be saved mid-stream, don't save info about those currently-running
				return "";
			}

			string parameterData = "";
			if (actionListAsset != null)
			{
				ID = AdvGame.PrepareStringForSaving (actionListAsset.name);
			}
			else if (actionList != null)
			{
				if (actionList.GetComponent <ConstantID>())
				{
					ID = actionList.GetComponent <ConstantID>().constantID.ToString ();

					if (subScene == null && UnityVersionHandler.ObjectIsInActiveScene (actionList.gameObject))
					{
						// OK
					}
					else if (subScene != null && UnityVersionHandler.GetSceneInfoFromGameObject (actionList.gameObject).Matches (subScene.SceneInfo))
					{
						// OK
					}
					else
					{
						return "";
					}
				}
				else
				{
					ACDebug.LogWarning ("Data for the ActionList '" + actionList.gameObject.name + "' was not saved because it has no Constant ID.", actionList.gameObject);
					return "";
				}
			}

			if (actionList != null)
			{
				parameterData = actionList.GetParameterData ();
			}

			if (conversationOnEnd != null && conversationOnEnd.GetComponent <ConstantID>())
			{
				convID = conversationOnEnd.GetComponent <ConstantID>().ToString ();
			}

			return (ID + SaveSystem.colon +
			        ConvertIndicesToString () + SaveSystem.colon +
			        startIndex + SaveSystem.colon +
			        ((inSkipQueue) ? 1 : 0) + SaveSystem.colon +
			        ((isRunning) ? 1 : 0) + SaveSystem.colon +
			        convID + SaveSystem.colon +
			        parameterData);
		}


		private string ConvertIndicesToString ()
		{
			string data = "";
			if (resumeIndices != null && resumeIndices.Length > 0)
			{
				for (int i=0; i<resumeIndices.Length; i++)
				{
					data += resumeIndices[i];
					if (i < (resumeIndices.Length - 1))
					{
						data += "]";
					}
				}
			}
			return data;
		}


		/**
		 * <summary>Restores the class's data from a saved string.</summary>
		 * <param name = "data">The saved string to restore from</param>
		 * <param name = "subScene">If set, only data for a given subscene will be loaded. If null, only data for the active scene will be loaded</param>
		 * <returns>True if the data was successfully restored</returns>
		 */
		public bool LoadData (string dataString, SubScene subScene = null)
		{
			if (dataString.Length == 0) return false;

			string[] dataArray = dataString.Split (SaveSystem.colon[0]);

			// ID
			string listName = AdvGame.PrepareStringForLoading (dataArray[0]);
			resumeIndices = new int[0];

			// Resume
			string[] resumeData = dataArray[1].Split ("]"[0]);
			if (resumeData.Length > 0)
			{
				List<int> resumeIndexList = new List<int>();
				for (int i=0; i<resumeData.Length; i++)
				{
					int resumeIndex = -1;
					if (int.TryParse (resumeData[i], out resumeIndex) && resumeIndex >= 0)
					{
						resumeIndexList.Add (resumeIndex);
					}
				}
				resumeIndices = resumeIndexList.ToArray ();
			}

			// StartIndex
			int.TryParse (dataArray[2], out startIndex);

			// Skip queue
			int j = 0;
			int.TryParse (dataArray[3], out j);
			inSkipQueue = (j == 1) ? true : false;

			// IsRunning
			j = 0;
			int.TryParse (dataArray[4], out j);
			isRunning = (j == 1) ? true : false;

			// Conversation on end
			int convID = 0;
			int.TryParse (dataArray[5], out convID);
			if (convID != 0)
			{
				conversationOnEnd = Serializer.returnComponent <Conversation> (convID, (subScene != null) ? subScene.gameObject : null);
			}

			// Parameter data
			parameterData = dataArray[6];

			// ActionList
			int ID = 0;
			if (int.TryParse (listName, out ID))
			{
				// Scene
				ConstantID constantID = Serializer.returnComponent <ConstantID> (ID, (subScene != null) ? subScene.gameObject : null);
				if (constantID != null && constantID.GetComponent <ActionList>() != null)
				{
					actionList = constantID.GetComponent <ActionList>();
					return true;
				}
			}
			else
			{
				// Asset file
				ActionListAsset tempAsset = ScriptableObject.CreateInstance <ActionListAsset> ();
				actionListAsset = AssetLoader.RetrieveAsset <ActionListAsset> (tempAsset, listName);
				if (actionListAsset != null && actionListAsset != tempAsset)
				{
					return true;
				}

				ACDebug.LogWarning ("Could not restore data related to the ActionList asset '" + listName + "' - to restore it correctly, the asset must be placed in a folder named Resources.");
			}
			return false;
		}
		
	}

}