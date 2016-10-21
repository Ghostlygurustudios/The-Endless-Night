﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"Button.cs"
 * 
 *	This script is a container class for interactions
 *	that are linked to Hotspots and NPCs.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * A data container for Hotspot interactions.
	 */
	[System.Serializable]
	public class Button
	{

		/** The Interaction ActionList to run, if the Hotspot's interactionSource = InteractionSource.InScene */
		public Interaction interaction = null;
		/** The ActionListAsset to run, if the Hotspots's interactionSource = InteractionSource.AssetFile */
		public ActionListAsset assetFile = null;

		/** The GameObject with the custom script to run, if the Hotspot's interactionSource = InteractionSource.CustomScript */
		public GameObject customScriptObject = null;
		/** The name of the function to run, if the Hotspot's interactionSource = InteractionSource.CustomScript */
		public string customScriptFunction = "";

		/** If True, then the interaction is disabled and cannot be displayed or triggered*/
		public bool isDisabled = false;
		/** The ID number of the inventory item (InvItem) this interaction is associated with, if this is an "Inventory" interaction */
		public int invID = 0;
		/** The ID number of the CursorIcon this interaction is associated with, if this is a "Use" interaction */
		public int iconID = -1;
		/** What kind of inventory interaction mode this responds to (Use, Give) */
		public SelectItemMode selectItemMode = SelectItemMode.Use;

		/** What the Player prefab does after clicking the Hotspot, but before the Interaction itself is run (DoNothing, TurnToFace, WalkTo, WalkToMarker) */
		public PlayerAction playerAction = PlayerAction.DoNothing;

		/** If True, and playerAction = PlayerAction.WalkTo, then the Interaction will be run once the Player is within a certain distance of the Hotspot */
		public bool setProximity = false;
		/** The proximity the Player must be within, if setProximity = True */
		public float proximity = 1f;
		/** If True, and playerAction = PlayerAction.WalkToMarker, then the Player will face the Hotspot after reaching the Marker */
		public bool faceAfter = false;
		/** If True, and playerAction = PlayerAction.WalkTo / WalkToMarker, then gameplay will be blocked while the Player moves */
		public bool isBlocking = false;

		/** If >=0, The ID number of the GameObject ActionParameter in assetFile / interaction to set to the Hotspot that the Button is a part of */
		public int parameterID = -1;


		/**
		 * The default Constructor.
		 */
		public Button ()
		{ }


		/**
		 * <summary>Checks if any of the Button's values have been modified from their defaults.</summary>
		 * <returns>True if any of the Button's values have been modified from their defaults.</returns>
		 */
		public bool IsButtonModified ()
		{
			if (interaction != null ||
			    assetFile != null ||
			    customScriptObject != null ||
			    customScriptFunction != "" ||
			    isDisabled != false ||
			    playerAction != PlayerAction.DoNothing ||
			    setProximity != false ||
			    proximity != 1f ||
			    faceAfter != false ||
			    isBlocking != false)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Copies the values of another Button onto itself.</summary>
		 * <param name = "_button">The Button to copies values from</param>
		 */
		public void CopyButton (Button _button)
		{
			interaction = _button.interaction;
			assetFile = _button.assetFile;
			customScriptObject = _button.customScriptObject;
			customScriptFunction = _button.customScriptFunction;
			isDisabled = _button.isDisabled;
			invID = _button.invID;
			iconID = _button.iconID;
			playerAction = _button.playerAction;
			setProximity = _button.setProximity;
			proximity = _button.proximity;
			faceAfter = _button.faceAfter;
			isBlocking = _button.isBlocking;
			parameterID = _button.parameterID;
		}
		
	}

}