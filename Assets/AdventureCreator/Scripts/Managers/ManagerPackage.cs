﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ManagerPackage.cs"
 * 
 *	This script is used to store references to Manager assets,
 *	so that they can be quickly loaded into the game engine in bulk.
 * 
 */

using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * An asset file that stores references to Manager assets, so that they can be quickly assigned in bulk.
	 */
	[System.Serializable]
	public class ManagerPackage : ScriptableObject
	{

		public ActionsManager actionsManager;
		public SceneManager sceneManager;
		public SettingsManager settingsManager;
		public InventoryManager inventoryManager;
		public VariablesManager variablesManager;
		public SpeechManager speechManager;
		public CursorManager cursorManager;
		public MenuManager menuManager;


		/**
		 * Assigns its various Manager asset files.
		 */
		public void AssignManagers ()
		{
			if (AdvGame.GetReferences () != null)
			{
				int numAssigned = 0;

				if (sceneManager)
				{
					AdvGame.GetReferences ().sceneManager = sceneManager;
					numAssigned ++;
				}
				
				if (settingsManager)
				{
					AdvGame.GetReferences ().settingsManager = settingsManager;
					numAssigned ++;
				}
				
				if (actionsManager)
				{
					AdvGame.GetReferences ().actionsManager = actionsManager;
					numAssigned ++;
				}
				
				if (variablesManager)
				{
					AdvGame.GetReferences ().variablesManager = variablesManager;
					numAssigned ++;
				}
				
				if (inventoryManager)
				{
					AdvGame.GetReferences ().inventoryManager = inventoryManager;
					numAssigned ++;
				}
				
				if (speechManager)
				{
					AdvGame.GetReferences ().speechManager = speechManager;
					numAssigned ++;
				}
				
				if (cursorManager)
				{
					AdvGame.GetReferences ().cursorManager = cursorManager;
					numAssigned ++;
				}
				
				if (menuManager)
				{
					AdvGame.GetReferences ().menuManager = menuManager;
					numAssigned ++;
				}

				#if UNITY_EDITOR
				if (KickStarter.sceneManager)
				{
					KickStarter.sceneManager.GetPrefabsInScene ();
				}

				UnityVersionHandler.CustomSetDirty (AdvGame.GetReferences (), true);
				AssetDatabase.SaveAssets ();
				#endif

				if (this)
				{
					if (numAssigned == 0)
					{
						ACDebug.Log (this.name + " No Mangers assigned.");
					}
					else if (numAssigned == 1)
					{
						ACDebug.Log (this.name + " - (" + numAssigned.ToString () + ") Manager assigned.");
					}
					else
					{
						ACDebug.Log (this.name + " - (" + numAssigned.ToString () + ") Managers assigned.");
					}
				}
			}
			else
			{
				ACDebug.LogError ("Can't assign managers - no References file found in Resources folder.");
			}
		}

	}

}