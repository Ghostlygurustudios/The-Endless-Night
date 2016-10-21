/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"References.cs"
 * 
 *	This script stores references to each of the managers that store the main game data.
 *	Each of the references need to be assigned for the game to work,
 *	and an asset file of this script must be placed in the Resources folder.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * This script stores references to each of the managers that store the main game data.
	 * Each of the references need to be assigned for the game to work, and an asset file of this script must be placed in the Resources folder.
	 */
	[System.Serializable]
	public class References : ScriptableObject
	{

		/** The current game's ActionsManager */
		public ActionsManager actionsManager;
		/** The current game's SceneManager */
		public SceneManager sceneManager;
		/** The current game's SettingsManager */
		public SettingsManager settingsManager;
		/** The current game's InventoryManager */
		public InventoryManager inventoryManager;
		/** The current game's VariablesManager */
		public VariablesManager variablesManager;
		/** The current game's SpeechManager */
		public SpeechManager speechManager;
		/** The current game's CursorManager */
		public CursorManager cursorManager;
		/** The current game's MenuManager */
		public MenuManager menuManager;

		/** True if the Menu Manager is open, so that AC knows to preview Menus in the Game Window */
		[HideInInspector] public bool viewingMenuManager;

	}

}