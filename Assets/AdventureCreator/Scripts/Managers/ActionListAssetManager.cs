/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionListAssetManager.cs"
 * 
 *	This script keeps track of which ActionListAssets are running.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component keeps track of which ActionListAssets are running.
	 * It should be placed on the PersistentEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_action_list_asset_manager.html")]
	#endif
	public class ActionListAssetManager : MonoBehaviour
	{

		/** Data about any ActionListAsset that has been run and we need to store information about */
		public List<ActiveList> activeLists = new List<ActiveList>();
		
		
		public void OnAwake ()
		{
			activeLists.Clear ();
		}
		

		/**
		 * <summary>Checks if a particular ActionListAsset file is running.</summary>
		 * <param name = "actionListAsset">The ActionListAsset to search for</param>
		 * <returns>True if the ActionListAsset file is currently running</returns>
		 */
		public bool IsListRunning (ActionListAsset actionListAsset)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionListAsset) && activeList.IsRunning ())
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Adds a new ActionListAsset, assumed to already be running, to the internal record of currently-running ActionListAssets, and sets the correct GameState in StateHandler.</summary>
		 * <param name = "runtimeActionList">The RuntimeActionList associated with the ActionListAsset to run</param>
		 * <param name = "actionListAsset">The ActionListAsset that is the runtimeActionList's source, if it has one.</param>
		 * <param name = "addToSkipQueue">If True, then the ActionList will be added to the list of ActionLists to skip</param>
		 * <param name = "_startIndex">The index number of the Action to start skipping from, if addToSkipQueue = True</param>
		 */
		public void AddToList (RuntimeActionList runtimeActionList, ActionListAsset actionListAsset, bool addToSkipQueue, int _startIndex)
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionListAsset))
				{
					activeLists.RemoveAt (i);
				}
			}

			addToSkipQueue = KickStarter.actionListManager.CanAddToSkipQueue (runtimeActionList, addToSkipQueue);
			activeLists.Add (new ActiveList (runtimeActionList, addToSkipQueue, _startIndex));
			
			if (runtimeActionList.actionListType == ActionListType.PauseGameplay && !runtimeActionList.unfreezePauseMenus && KickStarter.playerMenus.ArePauseMenusOn (null))
			{
				// Don't affect the gamestate if we want to remain frozen
				return;
			}
			
			KickStarter.actionListManager.SetCorrectGameState ();
		}
		
		
		/**
		 * <summary>Destroys the RuntimeActionList scene object that is running Actions from an ActionListAsset.</summary>
		 * <param name = "asset">The asset file that the RuntimeActionList has sourced its Actions from</param>
		 */
		public void DestroyAssetList (ActionListAsset asset)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (asset))
				{
					activeList.Reset (true);
				}
			}
		}
		
		
		/**
		 * <summary>Stops an ActionListAsset from running.</summary>
		 * <param name = "The ActionListAsset file to stop"></param>
		 * <param name = "_action">An Action that, if present within 'asset', will prevent the ActionListAsset from ending prematurely</param>
		 */
		public void EndAssetList (ActionListAsset asset, Action _action = null)
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (asset))
				{
					if (_action == null || !activeLists[i].actionList.actions.Contains (_action))
					{
						KickStarter.actionListManager.EndList (activeLists[i]);
					}
					else if (_action != null) ACDebug.Log ("Left " + activeLists[i].actionList.gameObject.name + " alone.");
				}
			}
		}

		
		private void OnDestroy ()
		{
			activeLists.Clear ();
		}
		

		/**
		 * <summary>Records the Action indices that the associated ActionListAsset was running before being paused. This data is sent to the ActionListAsset's associated ActiveList</summary>
		 * <param name = "actionListAsset">The ActionListAsset that is being paused</param>
		 * <param name = "resumeIndices">An array of Action indices to run when the ActionListAsset is resumed</param>
		 */
		public void AssignResumeIndices (ActionListAsset actionListAsset, int[] resumeIndices)
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionListAsset))
				{
					activeLists[i].SetResumeIndices (resumeIndices);
				}
			}
		}
		

		/**
		 * <summary>Pauses an ActionListAsset, provided that it is currently running.</summary>
		 * <param name = "actionListAsset">The ActionListAsset to pause</param>
		 * <returns>The RuntimeActionList that is in the scene, associated with the ActionListAsset</returns>
		 */
		public RuntimeActionList Pause (ActionListAsset actionListAsset)
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionListAsset))
				{
					RuntimeActionList runtimeActionList = (RuntimeActionList) activeLists[i].actionList;
					runtimeActionList.Pause ();
					return runtimeActionList;
				}
			}
			return null;
		}
		

		/**
		 * <summary>Resumes a previously-paused ActionListAsset. If the ActionListAsset is already running, nothing will happen.</summary>
		 * <param name = "actionListAsset">The ActionListAsset to pause</param>
		 */
		public void Resume (ActionListAsset actionListAsset)
		{
			if (IsListRunning (actionListAsset))
			{
				return;
			}

			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionListAsset))
				{
					GameObject runtimeActionListObject = (GameObject) Instantiate (Resources.Load (Resource.runtimeActionList));
					RuntimeActionList runtimeActionList = runtimeActionListObject.GetComponent <RuntimeActionList>();
					runtimeActionList.DownloadActions (actionListAsset, activeLists[i].GetConversationOnEnd (), activeLists[i].startIndex, false, activeLists[i].inSkipQueue, true);
					activeLists[i].Resume (runtimeActionList);
					return;
				}
			}
			AdvGame.RunActionListAsset (actionListAsset);
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
		}
		
		
		/**
		 * <summary>Generates a save-able string out of the ActionList resume data.<summary>
		 * <returns>A save-able string out of the ActionList resume data<returns>
		 */
		public string GetSaveData ()
		{
			PurgeLists ();
			string assetResumeData = "";
			for (int i=0; i<activeLists.Count; i++)
			{
				string thisResumeData = activeLists[i].GetSaveData (null);
				if (thisResumeData != null && thisResumeData.Length > 0)
				{
					assetResumeData += thisResumeData;

					if (i < (activeLists.Count - 1))
					{
						assetResumeData += SaveSystem.pipe;
					}
				}
			}
			return assetResumeData;
		}
		
		
		/**
		 * <summary>Recreates ActionList resume data from a saved data string.</summary>
		 * <param name = "_dataString">The saved data string</param>
		 */
		public void LoadData (string _dataString)
		{
			activeLists.Clear ();
			if (_dataString != null && _dataString.Length > 0)
			{
				string[] dataArray = _dataString.Split (SaveSystem.pipe[0]);
				foreach (string chunk in dataArray)
				{
					ActiveList activeList = new ActiveList ();
					if (activeList.LoadData (chunk))
					{
						activeLists.Add (activeList);
					}
				}
			}
		}
		
	}
	
}