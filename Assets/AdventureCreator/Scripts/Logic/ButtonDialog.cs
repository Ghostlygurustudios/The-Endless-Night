/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ButtonDialog.cs"
 * 
 *	This script is a container class for dialogue options
 *	that are linked to Conversations.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A data container for a dialogue option within a Conversation.
	 */
	[System.Serializable]
	public class ButtonDialog
	{

		/** The option's display label */
		public string label = "(Not set)";
		/** The translation ID number of the display label, as set by SpeechManager */
		public int lineID = -1;
		/** The option's display icon */
		public Texture2D icon;
		/** If True, the option is enabled, and will be displayed in a MenuDialogList element */
		public bool isOn;
		/** If True, the option is locked, and cannot be enabled or disabled */
		public bool isLocked;
		/** What happens when the DialogueOption ActionList has finished (ReturnToConversation, Stop, RunOtherConversation) */
		public ConversationAction conversationAction;
		/** The new Conversation to run, if conversationAction = ConversationAction.RunOtherConversation */
		public Conversation newConversation;
		/** An ID number unique to this instance of ButtonDialog within a Conversation */
		public int ID = 0;
		/** If True, then the option is currently selected for editing with the Conversation's Inspector */
		public bool isEditing = false;
		/** If True, then the option has been chosen at least once by the player */
		public bool hasBeenChosen = false;

		/** If True, then the option will only be visible if a given inventory item is being carried */
		public bool linkToInventory = false;
		/** The ID number of the associated inventory item, if linkToInventory = True */
		public int linkedInventoryID = 0;

		/** The DialogueOption ActionList to run, if the Conversation's interactionSource = InteractionSource.InScene */
		public DialogueOption dialogueOption;
		/** The ActionListAsset to run, if the Conversation's interactionSource = InteractionSource.AssetFile */
		public ActionListAsset assetFile = null;

		/** The GameObject with the custom script to run, if the Conversation's interactionSource = InteractionSource.CustomScript */
		public GameObject customScriptObject = null;
		/** The name of the function to run, if the Conversation's interactionSource = InteractionSource.CustomScript */
		public string customScriptFunction = "";


		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "idArray">An array of existing ID numbers, so that a unique ID number can be assigned</param>
		 */
		public ButtonDialog (int[] idArray)
		{
			label = "";
			icon = null;
			isOn = true;
			isLocked = false;
			conversationAction = ConversationAction.ReturnToConversation;
			assetFile = null;
			newConversation = null;
			dialogueOption = null;
			lineID = -1;
			ID = 1;
			isEditing = false;

			// Update id based on array
			foreach (int _id in idArray)
			{
				if (ID == _id)
				{
					ID ++;
				}
			}
		}


		/**
		 * <summary>Checks if the dialogue option can be currently shown.</summary>
		 * <returns>True if the dialogue option can be currently shown</returns>
		 */
		public bool CanShow ()
		{
			if (isOn)
			{
				if (!linkToInventory)
				{
					return true;
				}

				if (linkToInventory && KickStarter.runtimeInventory != null && KickStarter.runtimeInventory.IsCarryingItem (linkedInventoryID))
				{
					return true;
				}
			}
			return false;
		}

	}

}