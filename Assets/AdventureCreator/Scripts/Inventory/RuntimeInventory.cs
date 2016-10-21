/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"RuntimeInventory.cs"
 * 
 *	This script creates a local copy of the InventoryManager's items.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component is where inventory items (see InvItem) are stored at runtime.
	 * When the player aquires an item, it is transferred here (into localItems) from the InventoryManager asset.
	 * It should be placed on the PersistentEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_runtime_inventory.html")]
	#endif
	public class RuntimeInventory : MonoBehaviour
	{

		/** A List of inventory items (InvItem) carried by the player */
		[HideInInspector] public List<InvItem> localItems = new List<InvItem>();
		/** A List of inventory items (InvItem) being used in the current Recipe being crafted */
		[HideInInspector] public List<InvItem> craftingItems = new List<InvItem>();
		/** The default ActionListAsset to run if an inventory combination is unhandled */
		[HideInInspector] public ActionListAsset unhandledCombine;
		/** The default ActionListAsset to run if using an inventory item on a Hotspot is unhandled */
		[HideInInspector] public ActionListAsset unhandledHotspot;
		/** The default ActionListAsset to run if giving an inventory item to an NPC is unhandled */
		[HideInInspector] public ActionListAsset unhandledGive;

		/** The inventory item that is currently selected */
		[HideInInspector] public InvItem selectedItem = null;
		/** The inventory item that is currently being hovered over by the cursor */
		[HideInInspector] public InvItem hoverItem = null;
		/** The inventory item that is currently being highlighted within an MenuInventoryBox element */
		[HideInInspector] public InvItem highlightItem = null;
		/** If True, then the Hotspot label will show the name of the inventory item that the mouse is hovering over */ 
		[HideInInspector] public bool showHoverLabel = true;
		/** A List of index numbers within a Button's invButtons List that represent inventory interactions currently available to the player */
		[HideInInspector] public List<int> matchingInvInteractions = new List<int>();

		/** The last inventory item that the player clicked on, in any MenuInventoryBox element type */
		[HideInInspector] public InvItem lastClickedItem;

		private SelectItemMode selectItemMode = SelectItemMode.Use;
		private GUIStyle countStyle;
		private TextEffects countTextEffects;
		
		private HighlightState highlightState = HighlightState.None;
		private float pulse = 0f;
		private int pulseDirection = 0; // 0 = none, 1 = in, -1 = out
		

		/**
		 * Transfers any relevant data from InventoryManager when the game begins or restarts.
		 */
		public void OnStart ()
		{
			SetNull ();
			hoverItem = null;
			showHoverLabel = true;
			
			craftingItems.Clear ();
			localItems.Clear ();
			GetItemsOnStart ();

			if (KickStarter.inventoryManager)
			{
				unhandledCombine = KickStarter.inventoryManager.unhandledCombine;
				unhandledHotspot = KickStarter.inventoryManager.unhandledHotspot;
				unhandledGive = KickStarter.inventoryManager.unhandledGive;
			}
		}
		

		/**
		 * Initialises the inventory after a scene change. This is called manually by SaveSystem so that the order is correct.
		 */
		public void AfterLoad ()
		{
			if (!KickStarter.settingsManager.IsInLoadingScene () && KickStarter.sceneSettings != null)
			{
				SetNull ();
			}
		}
		

		/**
		 * De-selects the active inventory item.
		 */
		public void SetNull ()
		{
			if (selectedItem != null && localItems.Contains (selectedItem))
			{
				KickStarter.eventManager.Call_OnChangeInventory (selectedItem, InventoryEventType.Deselect);
			}

			selectedItem = null;
			highlightItem = null;
			lastClickedItem = null;
			PlayerMenus.ResetInventoryBoxes ();
		}
		

		/**
		 * <summary>Selects an inventory item (InvItem) by referencing its ID number.</summary>
		 * <param name = "_id">The inventory item's ID number</param>
		 * <param name = "_mode">What mode the item is selected in (Use, Give)</param>
		 */
		public void SelectItemByID (int _id, SelectItemMode _mode = SelectItemMode.Use)
		{
			if (_id == -1)
			{
				SetNull ();
				return;
			}

			foreach (InvItem item in localItems)
			{
				if (item != null && item.id == _id)
				{
					SetSelectItemMode (_mode);
					selectedItem = item;
					PlayerMenus.ResetInventoryBoxes ();
					KickStarter.eventManager.Call_OnChangeInventory (selectedItem, InventoryEventType.Select);
					return;
				}
			}
			
			SetNull ();
			ACDebug.LogWarning ("Want to select inventory item " + KickStarter.inventoryManager.GetLabel (_id) + " but player is not carrying it.");
		}
		

		/**
		 * <summary>Selects an inventory item (InvItem)</summary>
		 * <param name = "_id">The inventory item to selet</param>
		 * <param name = "_mode">What mode the item is selected in (Use, Give)</param>
		 */
		public void SelectItem (InvItem item, SelectItemMode _mode = SelectItemMode.Use)
		{
			if (selectedItem == item)
			{
				SetNull ();
				KickStarter.playerCursor.ResetSelectedCursor ();
			}
			else
			{
				SetSelectItemMode (_mode);
				selectedItem = item;
				KickStarter.eventManager.Call_OnChangeInventory (selectedItem, InventoryEventType.Select);
				PlayerMenus.ResetInventoryBoxes ();
			}
		}
		
		
		private void SetSelectItemMode (SelectItemMode _mode)
		{
			if (KickStarter.settingsManager.CanGiveItems ())
			{
				selectItemMode = _mode;
			}
			else
			{
				selectItemMode = SelectItemMode.Use;
			}
		}
		

		/**
		 * <summary>Checks if the currently-selected item is in "give" mode, as opposed to "use".</summary>
		 * <returns>True if the currently-selected item is in "give" mode, as opposed to "use"</returns>
		 */
		public bool IsGivingItem ()
		{
			if (selectItemMode == SelectItemMode.Give)
			{
				return true;
			}
			return false;
		}
		
		
		private void GetItemsOnStart ()
		{
			if (KickStarter.inventoryManager)
			{
				foreach (InvItem item in KickStarter.inventoryManager.items)
				{
					if (item.carryOnStart)
					{
						int playerID = -1;
						if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && !KickStarter.settingsManager.shareInventory && item.carryOnStartNotDefault && KickStarter.player != null && item.carryOnStartID != KickStarter.player.ID)
						{
							playerID = item.carryOnStartID;
						}

						if (!item.canCarryMultiple)
						{
							item.count = 1;
						}
						
						if (item.count < 1)
						{
							continue;
						}
						
						item.recipeSlot = -1;
						
						if (item.canCarryMultiple && item.useSeparateSlots)
						{
							for (int i=0; i<item.count; i++)
							{
								InvItem newItem = new InvItem (item);
								newItem.count = 1;
								
								if (playerID != -1)
								{
									Add (newItem.id, newItem.count, false, playerID);
								}
								else
								{
									localItems.Add (newItem);
								}
							}
						}
						else
						{
							if (playerID != -1)
							{
								Add (item.id, item.count, false, playerID);
							}
							else
							{
								localItems.Add (new InvItem (item));
							}
						}
					}
				}
			}
			else
			{
				ACDebug.LogError ("No Inventory Manager found - please use the Adventure Creator window to create one.");
			}
		}


		/**
		 * <summary>Replaces one inventory item carried by the player with another, retaining its position in its MenuInventoryBox element.</summary>
		 * <param name = "_addID">The ID number of the inventory item (InvItem) to add</param>
		 * <param name = "_removeID">The ID number of the inventory item (InvItem) to remove</param>
		 * <param name = "addAmount">The amount if the new inventory item to add, if the InvItem's canCarryMultiple = True</param>
		 */
		public void Replace (int _addID, int _removeID, int addAmount = 1)
		{
			int _index = -1;
			foreach (InvItem item in localItems)
			{
				if (item.id == _removeID && _index == -1)
				{
					_index = localItems.IndexOf (item);
				}

				if (item.id == _addID)
				{
					// Already carrying
					return;
				}
			}

			if (_index == -1)
			{
				// Not carrying
				Add (_addID, addAmount, false, -1);
				return;
			}

			foreach (InvItem item in KickStarter.inventoryManager.items)
			{
				if (item.id == _addID)
				{
					InvItem newItem = new InvItem (item);
					if (!newItem.canCarryMultiple)
					{
						addAmount = 1;
					}
					newItem.count = addAmount;
					localItems [_index] = newItem;
					return;
				}
			}
		}


		/**
		 * <summary>Adds an inventory item to the player's inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to add</param>
		 * <param name = "amount">The amount if the inventory item to add, if the InvItem's canCarryMultiple = True</param>
		 * <param name = "selectAfter">If True, then the inventory item will be automatically selected</param>
		 * <param name = "playerID">The ID number of the Player to receive the item, if multiple Player prefabs are supported. If playerID = 0, the current player will receive the item</param>
		 */
		public void Add (int _id, int amount = 1, bool selectAfter = false, int playerID = 0)
		{
			if (playerID >= 0 && KickStarter.player.ID != playerID)
			{
				AddToOtherPlayer (_id, amount, playerID);
			}
			else
			{
				localItems = Add (_id, amount, localItems, selectAfter);
				KickStarter.eventManager.Call_OnChangeInventory (GetItem (_id), InventoryEventType.Add, amount);
			}
		}


		/**
		 * <summary>Adds an inventory item to a generic inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to add</param>
		 * <param name = "amount">The amount if the inventory item to add, if the InvItem's canCarryMultiple = True</param>
		 * <param name = "itemList">The list of inventory items to add the new item to</param>
		 * <param name = "selectAfter">If True, then the inventory item will be automatically selected</param>
		 * <returns>The modified List of inventory items</returns>
		 */
		public List<InvItem> Add (int _id, int amount, List<InvItem> itemList, bool selectAfter)
		{
			itemList = ReorderItems (itemList);
			
			// Raise "count" by 1 for appropriate ID
			foreach (InvItem item in itemList)
			{
				if (item != null && item.id == _id)
				{
					if (item.canCarryMultiple)
					{
						if (item.useSeparateSlots)
						{
							break;
						}
						else
						{
							item.count += amount;
						}
					}
					
					if (selectAfter)
					{
						SelectItem (item, SelectItemMode.Use);
					}
					return itemList;
				}
			}
			
			// Not already carrying the item
			foreach (InvItem assetItem in KickStarter.inventoryManager.items)
			{
				if (assetItem.id == _id)
				{
					InvItem newItem = new InvItem (assetItem);
					if (!newItem.canCarryMultiple)
					{
						amount = 1;
					}
					newItem.recipeSlot = -1;
					newItem.count = amount;
					
					if (KickStarter.settingsManager.canReorderItems)
					{
						// Insert into first "blank" space
						for (int i=0; i<itemList.Count; i++)
						{
							if (itemList[i] == null)
							{
								itemList[i] = newItem;
								if (selectAfter)
								{
									SelectItem (newItem, SelectItemMode.Use);
								}
								
								if (newItem.canCarryMultiple && newItem.useSeparateSlots)
								{
									int count = newItem.count-1;
									newItem.count = 1;
									for (int j=0; j<count; j++)
									{
										itemList.Add (newItem);
									}
								}
								return itemList;
							}
						}
					}
					
					if (newItem.canCarryMultiple && newItem.useSeparateSlots)
					{
						int count = newItem.count;
						newItem.count = 1;
						for (int i=0; i<count; i++)
						{
							itemList.Add (newItem);
						}
					}
					else
					{
						itemList.Add (newItem);
					}
					
					if (selectAfter)
					{
						SelectItem (newItem, SelectItemMode.Use);
					}
					return itemList;
				}
			}
			
			itemList = RemoveEmptySlots (itemList);
			return itemList;
		}
		

		/**
		 * <summary>Removes an inventory item from the player's inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to remove</param>
		 * <param name = "amount">The amount if the inventory item to remove, if the InvItem's canCarryMultiple = True</param>
		 * <param name = "setAmount">If False, then all instances of the inventory item will be removed, even if the InvItem's canCarryMultiple = True</param>
		 * <param name = "playerID">The ID number of the Player to lose the item, if multiple Player prefabs are supported. If playerID = 0, the current player will lose the item</param>
		 */
		public void Remove (int _id, int amount, bool setAmount, int playerID = 0)
		{
			if (playerID >= 0 && KickStarter.player.ID != playerID)
			{
				RemoveFromOtherPlayer (_id, amount, setAmount, playerID);
			}
			else
			{
				localItems = Remove (_id, amount, setAmount, localItems);
				KickStarter.eventManager.Call_OnChangeInventory (GetItem (_id), InventoryEventType.Remove, amount);
			}
		}
		
		
		private void AddToOtherPlayer (int invID, int amount, int playerID)
		{
			SaveSystem saveSystem = GetComponent <SaveSystem>();
			
			List<InvItem> otherPlayerItems = saveSystem.GetItemsFromPlayer (playerID);
			otherPlayerItems = Add (invID, amount, otherPlayerItems, false);
			saveSystem.AssignItemsToPlayer (otherPlayerItems, playerID);
		}
		
		
		private void RemoveFromOtherPlayer (int invID, int amount, bool setAmount, int playerID)
		{
			SaveSystem saveSystem = GetComponent <SaveSystem>();
			
			List<InvItem> otherPlayerItems = saveSystem.GetItemsFromPlayer (playerID);
			otherPlayerItems = Remove (invID, amount, setAmount, otherPlayerItems);
			saveSystem.AssignItemsToPlayer (otherPlayerItems, playerID);
		}
		

		/**
		 * <summary>Removes an inventory item from the player's inventory.</summary>
		 * <param name = "_item">The inventory item (InvItem) to remove</param>
		 */
		public void Remove (InvItem _item)
		{
			if (_item != null && localItems.Contains (_item))
			{
				if (_item == selectedItem)
				{
					SetNull ();
				}
				
				localItems [localItems.IndexOf (_item)] = null;
				
				localItems = ReorderItems (localItems);
				localItems = RemoveEmptySlots (localItems);

				KickStarter.eventManager.Call_OnChangeInventory (_item, InventoryEventType.Remove);
			}
		}
		
		
		private List<InvItem> Remove (int _id, int amount, bool setAmount, List<InvItem> itemList)
		{
			if (amount <= 0)
			{
				return itemList;
			}
			
			foreach (InvItem item in itemList)
			{
				if (item != null && item.id == _id)
				{
					KickStarter.eventManager.Call_OnChangeInventory (item, InventoryEventType.Remove, amount);

					if (item.canCarryMultiple && item.useSeparateSlots)
					{
						itemList [itemList.IndexOf (item)] = null;
						amount --;
						
						if (amount == 0)
						{
							break;
						}
						
						continue;
					}
					
					if (!item.canCarryMultiple || !setAmount)
					{
						itemList [itemList.IndexOf (item)] = null;
						amount = 0;
					}
					else
					{
						if (item.count > 0)
						{
							int numLeft = item.count - amount;
							item.count -= amount;
							amount = numLeft;
						}
						if (item.count < 1)
						{
							itemList [itemList.IndexOf (item)] = null;
						}
					}
					
					itemList = ReorderItems (itemList);
					itemList = RemoveEmptySlots (itemList);

					if (itemList.Count == 0)
					{
						return itemList;
					}
					
					if (amount <= 0)
					{
						return itemList;
					}
				}
			}
			
			itemList = ReorderItems (itemList);
			itemList = RemoveEmptySlots (itemList);
			
			return itemList;
		}


		/**
		 * <summary>Gets the full prefix to a Hotpsot label when an item is selected, e.g. "Use X on " / "Give X to ".</summary>
		 * <param name = "item">The inventory item that is selected</param>
		 * <param name = "itemName">The display name of the inventory item, in the current language</param>
		 * <param name = "languageNumber">The index of the current language, as set in SpeechManager</param>
		 * <param name = "canGive">If True, the the item is assumed to be in "give" mode, as opposed to "use".</param>
		 * <returns>The full prefix to a Hotspot label when the item is selected</returns>
		 */
		public string GetHotspotPrefixLabel (InvItem item, string itemName, int languageNumber, bool canGive = false)
		{
			string prefix1 = "";
			string prefix2 = "";
			
			if (canGive && IsGivingItem ())
			{
				prefix1 = KickStarter.runtimeLanguages.GetTranslation (KickStarter.cursorManager.hotspotPrefix3.label, KickStarter.cursorManager.hotspotPrefix3.lineID, languageNumber);
				prefix2 = KickStarter.runtimeLanguages.GetTranslation (KickStarter.cursorManager.hotspotPrefix4.label, KickStarter.cursorManager.hotspotPrefix4.lineID, languageNumber);
			}
			else
			{
				if (item != null && item.overrideUseSyntax)
				{
					prefix1 = KickStarter.runtimeLanguages.GetTranslation (item.hotspotPrefix1.label, item.hotspotPrefix1.lineID, languageNumber);
					prefix2 = KickStarter.runtimeLanguages.GetTranslation (item.hotspotPrefix2.label, item.hotspotPrefix2.lineID, languageNumber);
				}
				else
				{
					prefix1 = KickStarter.runtimeLanguages.GetTranslation (KickStarter.cursorManager.hotspotPrefix1.label, KickStarter.cursorManager.hotspotPrefix1.lineID, languageNumber);
					prefix2 = KickStarter.runtimeLanguages.GetTranslation (KickStarter.cursorManager.hotspotPrefix2.label, KickStarter.cursorManager.hotspotPrefix2.lineID, languageNumber);
				}
			}

			if (prefix1 == "" && prefix2 != "")
			{
				return (prefix2 + " ");
			}
			if (prefix1 != "" && prefix2 == "")
			{
				return (prefix1 + " " + itemName + " ");
			}
			if (prefix1 == " " && prefix2 != "")
			{
				return (itemName + " " + prefix2 + " ");
			}
			return (prefix1 + " " + itemName + " " + prefix2 + " ");
		}


		private List<InvItem> ReorderItems (List<InvItem> invItems)
		{
			if (!KickStarter.settingsManager.canReorderItems)
			{
				for (int i=0; i<invItems.Count; i++)
				{
					if (invItems[i] == null)
					{
						invItems.RemoveAt (i);
						i=0;
					}
				}
			}
			return invItems;
		}
		
		
		private void RemoveEmptyCraftingSlots ()
		{
			// Remove empty slots on end
			for (int i=craftingItems.Count-1; i>=0; i--)
			{
				if (localItems[i] == null)
				{
					localItems.RemoveAt (i);
				}
				else
				{
					return;
				}
			}
		}
		
		
		private List<InvItem> RemoveEmptySlots (List<InvItem> itemList)
		{
			// Remove empty slots on end
			for (int i=itemList.Count-1; i>=0; i--)
			{
				if (itemList[i] == null)
				{
					itemList.RemoveAt (i);
				}
				else
				{
					return itemList;
				}
			}
			return itemList;
		}
		

		/**
		 * <summary>Gets an inventory item's display name.</summary>
		 * <param name = "item">The inventory item to get the display name of</param>
		 * <param name = "languageNumber">The index of the current language, as set in SpeechManager</param>
		 * <returns>The inventory item's display name</returns>
		 */
		public string GetLabel (InvItem item, int languageNumber)
		{
			if (languageNumber > 0)
			{
				return (KickStarter.runtimeLanguages.GetTranslation (item.label, item.lineID, languageNumber));
			}
			else if (item.altLabel != "")
			{
				return (item.altLabel);
			}
			return (item.label);
		}
		

		/**
		 * <summary>Gets the amount of a particular inventory item within the player's inventory.</summary>
		 * <param name = "_invID">The ID number of the inventory item (InvItem) in question</param>
		 * <returns>The amount of the inventory item within the player's inventory.</returns>
		 */
		public int GetCount (int _invID)
		{
			foreach (InvItem item in localItems)
			{
				if (item != null && item.id == _invID)
				{
					return (item.count);
				}
			}
			
			return 0;
		}
		

		/**
		 * <summary>Gets the amount of a particular inventory item within any player's inventory, if multiple Player prefabs are supported.</summary>
		 * <param name = "_invID">The ID number of the inventory item (InvItem) in question</param>
		 * <param name = "playerID">The ID number of the Player to refer to</param>
		 * <returns>The amount of the inventory item within the player's inventory.</returns>
		 */
		public int GetCount (int _invID, int _playerID)
		{
			List<InvItem> otherPlayerItems = GetComponent <SaveSystem>().GetItemsFromPlayer (_playerID);
			
			if (otherPlayerItems != null)
			{
				foreach (InvItem item in otherPlayerItems)
				{
					if (item != null && item.id == _invID)
					{
						return (item.count);
					}
				}
			}
			return 0;
		}
		

		/**
		 * <summary>Gets an inventory item within the current Recipe being crafted.</summary>
		 * <param name "_id">The ID number of the inventory item</param>
		 * <returns>The inventory item, if it is within the current Recipe being crafted</returns>
		 */
		public InvItem GetCraftingItem (int _id)
		{
			foreach (InvItem item in craftingItems)
			{
				if (item.id == _id)
				{
					return item;
				}
			}
			
			return null;
		}
		

		/**
		 * <summary>Gets an inventory item within the player's current inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item</param>
		 * <returns>The inventory item, if it is held by the player</returns>
		 */
		public InvItem GetItem (int _id)
		{
			foreach (InvItem item in localItems)
			{
				if (item != null && item.id == _id)
				{
					return item;
				}
			}
			return null;
		}


		/**
		 * <summary>Checks if an inventory item is within the player's current inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item</param>
		 * <returns>True if the inventory item is within the player's current inventory</returns>
		 */
		public bool IsCarryingItem (int _id)
		{
			foreach (InvItem item in localItems)
			{
				if (item != null && item.id == _id)
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Runs an inventory item's "Examine" interaction.</summary>
		 * <param name = "item">The inventory item to examine</param>
		 */
		public void Look (InvItem item)
		{
			if (item == null || item.recipeSlot > -1) return;
			
			if (item.lookActionList)
			{
				AdvGame.RunActionListAsset (item.lookActionList);
				KickStarter.eventManager.Call_OnUseInventory (item, KickStarter.cursorManager.lookCursor_ID);
			}
		}
		

		/**
		 * <summary>Runs an inventory item's "Use" interaction.</summary>
		 * <param name ="item">The inventory item to use</param>
		 */
		public void Use (InvItem item)
		{
			if (item == null || item.recipeSlot > -1) return;
			
			if (item.useActionList)
			{
				//selectedItem = null;
				SetNull ();
				AdvGame.RunActionListAsset (item.useActionList);
				KickStarter.eventManager.Call_OnUseInventory (item, 0);
			}
			else if (KickStarter.settingsManager.CanSelectItems (true))
			{
				SelectItem (item, SelectItemMode.Use);
			}
		}
		

		/**
		 * <summary>Runs an inventory item's interaction, when multiple "use" interactions are defined.</summary>
		 * <param name = "invItem">The relevant inventory item</param>
		 * <param name = "iconID">The ID number of the interaction's icon, defined in CursorManager</param>
		 */
		public void RunInteraction (InvItem invItem, int iconID)
		{
			if (!KickStarter.settingsManager.allowInventoryInteractionsDuringConversations && KickStarter.stateHandler.gameState == GameState.DialogOptions)
			{
				return;
			}

			if (invItem == null || invItem.recipeSlot > -1) return;
			
			foreach (InvInteraction interaction in invItem.interactions)
			{
				if (interaction.icon.id == iconID)
				{
					if (interaction.actionList)
					{
						AdvGame.RunActionListAsset (interaction.actionList);
						KickStarter.eventManager.Call_OnUseInventory (invItem, iconID);
						return;
					}
					break;
				}
			}
			
			// Unhandled
			if (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ContextSensitive && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple && KickStarter.settingsManager.CanSelectItems (false))
			{
				// Auto-select
				if (KickStarter.settingsManager.selectInvWithUnhandled && iconID == KickStarter.settingsManager.selectInvWithIconID)
				{
					SelectItem (invItem, SelectItemMode.Use);
					return;
				}
				if (KickStarter.settingsManager.giveInvWithUnhandled && iconID == KickStarter.settingsManager.giveInvWithIconID)
				{
					SelectItem (invItem, SelectItemMode.Give);
					return;
				}
			}
			
			AdvGame.RunActionListAsset (KickStarter.cursorManager.GetUnhandledInteraction (iconID));
			KickStarter.eventManager.Call_OnUseInventory (invItem, iconID);
		}
		

		/**
		 * <summary>Runs an interaction on the "hoverItem" inventory item, when multiple "use" interactions are defined.</summary>
		 * <param name = "iconID">The ID number of the interaction's icon, defined in CursorManager</param>
		 * <param name = "clickedItem">If assigned, hoverItem will be become this before the interaction is run</param>
		 */
		public void RunInteraction (int iconID, InvItem clickedItem = null)
		{
			if (clickedItem != null)
			{
				hoverItem = clickedItem;
			}

			RunInteraction (hoverItem, iconID);
		}
		

		/**
		 * <summary>Sets up all "Interaction" menus according to a specific inventory item.</summary>
		 * <param name = "item">The relevant inventory item</param>
		 */
		public void ShowInteractions (InvItem item)
		{
			hoverItem = item;
			KickStarter.playerMenus.SetInteractionMenus (true);
		}


		/**
		 * <summary>Sets the item currently being hovered over by the mouse cursor.</summary>
		 * <param name = "item">The item to set</param>
		 * <param name = "menuInventoryBox">The MenuInventoryBox that the item is displayed within</param>
		 */
		public void SetHoverItem (InvItem item, MenuInventoryBox menuInventoryBox)
		{
			hoverItem = item;

			if (menuInventoryBox.displayType == ConversationDisplayType.IconOnly)
			{
				if (menuInventoryBox.inventoryBoxType == AC_InventoryBoxType.Container && selectedItem != null)
				{
					showHoverLabel = false;
				}
				else
				{
					showHoverLabel = true;
				}
			}
			else
			{
				showHoverLabel = false;
			}
		}


		/**
		 * <summary>Sets the item currently being hovered over by the mouse cursor.</summary>
		 * <param name = "item">The item to set</param>
		 * <param name = "menuCrafting">The MenuInventoryBox that the item is displayed within</param>
		 */
		public void SetHoverItem (InvItem item, MenuCrafting menuCrafting)
		{
			hoverItem = item;

			if (menuCrafting.displayType == ConversationDisplayType.IconOnly)
			{
				showHoverLabel = true;
			}
			else
			{
				showHoverLabel = false;
			}
		}


		/**
		 * <summary>Combines two inventory items.</summary>
		 * <param name = "item1">The first inventory item to combine</param>
		 * <param name = "item2ID">The ID number of the second inventory item to combine</param>
		 */
		public void Combine (InvItem item1, int item2ID)
		{
			Combine (item1, GetItem (item2ID));
		}
		

		/**
		 * <summary>Combines two inventory items.</summary>
		 * <param name = "item1">The first inventory item to combine</param>
		 * <param name = "item2ID">The second inventory item to combine</param>
		 */
		public void Combine (InvItem item1, InvItem item2)
		{
			if (item2 == null || item1 == null || item2.recipeSlot > -1)
			{
				return;
			}
			
			if (item2 == item1)
			{
				if ((KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction || KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Single) && KickStarter.settingsManager.inventoryDragDrop && KickStarter.settingsManager.inventoryDropLook)
				{
					Look (item2);
				}

				//selectedItem = null;
				SetNull ();
			}
			else
			{
				if (selectedItem == null)
				{
					InvItem tempItem = item1;
					item1 = item2;
					item2 = tempItem;
				}

				KickStarter.eventManager.Call_OnUseInventory (item1, 0, item2);

				for (int i=0; i<item2.combineID.Count; i++)
				{
					if (item2.combineID[i] == item1.id && item2.combineActionList[i] != null)
					{
						selectedItem = null;

						PlayerMenus.ForceOffAllMenus (true);
						AdvGame.RunActionListAsset (item2.combineActionList [i]);
						return;
					}
				}
				
				if (KickStarter.settingsManager.reverseInventoryCombinations || (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple))
				{
					// Try opposite: search selected item instead
					for (int i=0; i<item1.combineID.Count; i++)
					{
						if (item1.combineID[i] == item2.id && item1.combineActionList[i] != null)
						{
							selectedItem = null;

							ActionListAsset assetFile = item1.combineActionList[i];
							PlayerMenus.ForceOffAllMenus (true);
							AdvGame.RunActionListAsset (assetFile);
							return;
						}
					}
				}
				
				// Found no combine match
				if (KickStarter.settingsManager.inventoryDisableUnhandled)
				{
					selectedItem = null;
				}

				if (item1.unhandledCombineActionList)
				{
					ActionListAsset unhandledActionList = item1.unhandledCombineActionList;
					AdvGame.RunActionListAsset (unhandledActionList);	
				}
				else if (unhandledCombine)
				{
					PlayerMenus.ForceOffAllMenus (true);
					AdvGame.RunActionListAsset (unhandledCombine);
				}
			}
			
			KickStarter.playerCursor.ResetSelectedCursor ();
		}
		

		/**
		 * <summary>Gets the currently selected inventory item as a List with a single entry.</summary>
		 * <returns>The currently selected inventory item as a List with a single entry.</returns>
		 */
		public List<InvItem> GetSelected ()
		{
			List<InvItem> items = new List<InvItem>();
			
			if (selectedItem != null)
			{
				items.Add (selectedItem);
			}
			
			return items;
		}
		

		/**
		 * <summary>Checks if a particular inventory item is currently held by the player.</summary>
		 * <param name = "_item">The inventory item to check for</param>
		 * <returns>True if the inventory item is currently held by the player</returns>
		 */
		public bool IsItemCarried (InvItem _item)
		{
			if (_item == null) return false;
			foreach (InvItem item in localItems)
			{
				if (item == _item)
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * Resets any active recipe, and clears all MenuCrafting elements.
		 */
		public void RemoveRecipes ()
		{
			while (craftingItems.Count > 0)
			{
				for (int i=0; i<craftingItems.Count; i++)
				{
					Add (craftingItems[i].id, craftingItems[i].count, false, -1);
					craftingItems.RemoveAt (i);
				}
			}
			PlayerMenus.ResetInventoryBoxes ();
		}
		

		/**
		 * <summary>Moves an ingredient from a crafting recipe back into the player's inventory.</summary>
		 * <param name = "_recipeSlot">The index number of the MenuCrafting slot that the ingredient was placed in</param>
		 * <param name = "selectAfter">If True, the inventory item will be selected once the transfer is complete</param>
		 */
		public void TransferCraftingToLocal (int _recipeSlot, bool selectAfter)
		{
			foreach (InvItem item in craftingItems)
			{
				if (item.recipeSlot == _recipeSlot)
				{
					Add (item.id, item.count, selectAfter, -1);
					SelectItemByID (item.id, SelectItemMode.Use);
					craftingItems.Remove (item);
					return;
				}
			}
		}


		/**
		 * <summary>Moves an ingredient from the player's inventory into a crafting recipe as an ingredient.</summary>
		 * <param name = "_item">The inventory item to transfer</param>
		 * <param name = "_slot">The index number of the MenuCrafting slot to place the item in</param>
		 */
		public void TransferLocalToCrafting (InvItem _item, int _slot)
		{
			if (_item != null && localItems.Contains (_item))
			{
				_item.recipeSlot = _slot;
				craftingItems.Add (_item);
				
				localItems [localItems.IndexOf (_item)] = null;
				localItems = ReorderItems (localItems);
				localItems = RemoveEmptySlots (localItems);
				
				SetNull ();
			}
		}
		

		/**
		 * <summary>Gets a list of inventory items associated with the interactions of the current Hotspot or item being hovered over.</summary>
		 * <returns>A list of inventory items associated with the interactions of the current Hotspot or item being hovered over</returns>
		 */
		public List<InvItem> MatchInteractions ()
		{
			List<InvItem> items = new List<InvItem>();
			matchingInvInteractions = new List<int>();
			
			if (!KickStarter.settingsManager.cycleInventoryCursors)
			{
				return items;
			}
			
			if (hoverItem != null)
			{
				items = MatchInteractionsFromItem (items, hoverItem);
			}
			else if (KickStarter.playerInteraction.GetActiveHotspot ())
			{
				List<Button> invButtons = KickStarter.playerInteraction.GetActiveHotspot ().invButtons;
				foreach (Button button in invButtons)
				{
					foreach (InvItem item in localItems)
					{
						if (item != null && item.id == button.invID && !button.isDisabled)
						{
							matchingInvInteractions.Add (invButtons.IndexOf (button));
							items.Add (item);
							break;
						}
					}
				}
			}
			return items;
		}
		
		
		private List<InvItem> MatchInteractionsFromItem (List<InvItem> items, InvItem _item)
		{
			if (_item != null && _item.combineID != null)
			{
				foreach (int combineID in _item.combineID)
				{
					foreach (InvItem item in localItems)
					{
						if (item != null && item.id == combineID)
						{
							matchingInvInteractions.Add (_item.combineID.IndexOf (combineID));
							items.Add (item);
							break;
						}
					}
				}
			}
			return items;
		}
		

		/**
		 * <summary>Works out which Recipe, if any, for which all ingredients have been correctly arranged.</summary>
		 * <param name = "autoCreateMatch">If True, then any Recipes with autoCreateMatch = False will be ignored</param>
		 * <returns>The Recipe, if any, for which all ingredients have been correctly arranged</returns>
		 */
		public Recipe CalculateRecipe (bool autoCreateMatch)
		{
			if (KickStarter.inventoryManager == null)
			{
				return null;
			}
			
			foreach (Recipe recipe in KickStarter.inventoryManager.recipes)
			{
				if (autoCreateMatch)
				{
					if (!recipe.autoCreate)
					{
						break;
					}
				}

				if (IsRecipeInvalid (recipe) || recipe.ingredients.Count == 0)
				{
					continue;
				}
				
				bool canCreateRecipe = true;
				while (canCreateRecipe)
				{
					foreach (Ingredient ingredient in recipe.ingredients)
					{
						// Is ingredient present (and optionally, in correct slot)
						InvItem ingredientItem = GetCraftingItem (ingredient.itemID);
						if (ingredientItem == null)
						{
							canCreateRecipe = false;
							break;
						}
						
						if ((recipe.useSpecificSlots && ingredientItem.recipeSlot == (ingredient.slotNumber -1)) || !recipe.useSpecificSlots)
						{
							if ((ingredientItem.canCarryMultiple && ingredientItem.count >= ingredient.amount) || !ingredientItem.canCarryMultiple)
							{
								if (canCreateRecipe && recipe.ingredients.IndexOf (ingredient) == (recipe.ingredients.Count -1))
								{
									return recipe;
								}
							}
							else canCreateRecipe = false;
						}
						else canCreateRecipe = false;
					}
				}
			}
			
			return null;
		}


		private bool IsRecipeInvalid (Recipe recipe)
		{
			// Are any invalid ingredients present?
			foreach (InvItem item in craftingItems)
			{
				bool found = false;
				foreach (Ingredient ingredient in recipe.ingredients)
				{
					if (ingredient.itemID == item.id)
					{
						found = true;
					}
				}
				if (!found)
				{
					// Not present in recipe
					return true;
				}
			}
			return false;
		}
		

		/**
		 * <summary>Crafts a new inventory item, and removes the relevent ingredients, according to a Recipe.</summary>
		 * <param name = "recipe">The Recipe to perform</param>
		 * <param name = "selectAfter">If True, then the resulting inventory item will be selected once the crafting is complete</param>
		 */
		public void PerformCrafting (Recipe recipe, bool selectAfter)
		{
			foreach (Ingredient ingredient in recipe.ingredients)
			{
				for (int i=0; i<craftingItems.Count; i++)
				{
					if (craftingItems [i].id == ingredient.itemID)
					{
						if (craftingItems [i].canCarryMultiple && ingredient.amount > 0)
						{
							craftingItems [i].count -= ingredient.amount;
							if (craftingItems [i].count < 1)
							{
								craftingItems.RemoveAt (i);
							}
						}
						else
						{
							craftingItems.RemoveAt (i);
						}
					}
				}
			}
			
			RemoveEmptyCraftingSlots ();
			Add (recipe.resultID, 1, selectAfter, -1);
		}
		

		/**
		 * <summary>Moves an item already in an inventory to a different slot.</summary>
		 * <param name = "item">The inventory item to move</param>
		 * <param name = "items">The List of inventory items that the item is to be moved within</param>
		 * <param name = "index">The index number of the MenuInventoryBox slot to move the item to</param>
		 * <returns>The re-ordered List of inventory items</returns>
		 */
		public List<InvItem> MoveItemToIndex (InvItem item, List<InvItem> items, int index)
		{
			if (item != null)
			{
				// Check nothing in place already
				int oldIndex = items.IndexOf (item);
				while (items.Count <= Mathf.Max (index, oldIndex))
				{
					items.Add (null);
				}
				
				if (items [index] == null)
				{
					items [index] = item;
					items [oldIndex] = null;
				}
				
				SetNull ();
				items = RemoveEmptySlots (items);
			}
			return items;
		}
		

		/**
		 * <summary>Sets the font style of the "amount" numbers displayed over an inventory item in OnGUI menus</summary>
		 * <param name = "font">The font to use<param>
		 * <param name = "size">The font's size</param>
		 * <param name = "color">The colour to set the font</param>
		 * <param name = "textEffects">What text effect to apply (Outline, Shadow, OutlineAndShadow)</param>
		 */
		public void SetFont (Font font, int size, Color color, TextEffects textEffects)
		{
			countStyle = new GUIStyle();
			countStyle.font = font;
			countStyle.fontSize = size;
			countStyle.normal.textColor = color;
			countStyle.alignment = TextAnchor.MiddleCenter;
			countTextEffects = textEffects;
		}
		

		/**
		 * <summary>Draws the currently-highlight item across a set region of the screen.</summary>
		 * <param name = "_rect">The Screen-Space co-ordinates at which to draw the highlight item</param>
		 */
		public void DrawHighlighted (Rect _rect)
		{
			if (highlightItem == null || highlightItem.activeTex == null) return;
			
			if (highlightState == HighlightState.None)
			{
				GUI.DrawTexture (_rect, highlightItem.activeTex, ScaleMode.StretchToFill, true, 0f);
				return;
			}
			
			if (pulseDirection == 0)
			{
				pulse = 0f;
				pulseDirection = 1;
			}
			else if (pulseDirection == 1)
			{
				pulse += KickStarter.settingsManager.inventoryPulseSpeed * Time.deltaTime;
			}
			else if (pulseDirection == -1)
			{
				pulse -= KickStarter.settingsManager.inventoryPulseSpeed * Time.deltaTime;
			}
			
			if (pulse > 1f)
			{
				pulse = 1f;
				
				if (highlightState == HighlightState.Normal)
				{
					highlightState = HighlightState.None;
					GUI.DrawTexture (_rect, highlightItem.activeTex, ScaleMode.StretchToFill, true, 0f);
					return;
				}
				else
				{
					pulseDirection = -1;
				}
			}
			else if (pulse < 0f)
			{
				pulse = 0f;
				
				if (highlightState == HighlightState.Pulse)
				{
					pulseDirection = 1;
				}
				else
				{
					highlightState = HighlightState.None;
					GUI.DrawTexture (_rect, highlightItem.tex, ScaleMode.StretchToFill, true, 0f);
					highlightItem = null;
					return;
				}
			}

			Color backupColor = GUI.color;
			Color tempColor = GUI.color;
			
			tempColor.a = pulse;
			GUI.color = tempColor;
			GUI.DrawTexture (_rect, highlightItem.activeTex, ScaleMode.StretchToFill, true, 0f);
			GUI.color = backupColor;
			GUI.DrawTexture (_rect, highlightItem.tex, ScaleMode.StretchToFill, true, 0f);
		}
		

		/**
		 * <summary>Fully highlights an inventory item instantly.</summary>
		 * <param name = "_id">The ID number of the inventory item (see InvItem) to highlight</param>
		 */
		public void HighlightItemOnInstant (int _id)
		{
			highlightItem = GetItem (_id);
			highlightState = HighlightState.None;
			pulse = 1f;
		}
		

		/**
		 * Removes all highlighting from the inventory item curently being highlighted.
		 */
		public void HighlightItemOffInstant ()
		{
			highlightItem = null;
			highlightState = HighlightState.None;
			pulse = 0f;
		}
		

		/**
		 * <summary>Highlights an inventory item.</summary>
		 * <param name = "_id">The ID number of the inventory item (see InvItem) to highlight</param>
		 * <param name = "_type">The type of highlighting effect to perform (Enable, Disable, PulseOnce, PulseContinuously)</param>
		 */
		public void HighlightItem (int _id, HighlightType _type)
		{
			highlightItem = GetItem (_id);
			if (highlightItem == null) return;
			
			if (_type == HighlightType.Enable)
			{
				highlightState = HighlightState.Normal;
				pulseDirection = 1;
			}
			else if (_type == HighlightType.Disable)
			{
				highlightState = HighlightState.Normal;
				pulseDirection = -1;
			}
			else if (_type == HighlightType.PulseOnce)
			{
				highlightState = HighlightState.Flash;
				pulse = 0f;
				pulseDirection = 1;
			}
			else if (_type ==  HighlightType.PulseContinually)
			{
				highlightState = HighlightState.Pulse;
				pulse = 0f;
				pulseDirection = 1;
			}
		}
		

		/**
		 * <summary>Draws a number at the cursor position. This should be called within an OnGUI function.</summary>
		 * <param name = "cursorPosition">The position of the cursor</param>
		 * <param name = "cursorSize">The size to draw the number<param>
		 * <param name = "count">The number to display</param>
		 */
		public void DrawInventoryCount (Vector2 cursorPosition, float cursorSize, int count)
		{
			if (count > 1)
			{
				if (countTextEffects != TextEffects.None)
				{
					AdvGame.DrawTextEffect (AdvGame.GUIBox (cursorPosition, cursorSize), count.ToString (), countStyle, Color.black, countStyle.normal.textColor, 2, countTextEffects);
				}
				else
				{
					GUI.Label (AdvGame.GUIBox (cursorPosition, cursorSize), count.ToString (), countStyle);
				}
			}
		}

		private void ClickInvItemToInteract ()
		{
			int invID = KickStarter.playerInteraction.GetActiveInvButtonID ();
			if (invID == -1)
			{
				RunInteraction (KickStarter.playerInteraction.GetActiveUseButtonIconID ());
			}
			else
			{
				Combine (hoverItem, invID);
			}
		}
		

		/**
		 * <summary>Processes the clicking of an inventory item within a MenuInventoryBox element</summary>
		 * <param name = "_menu">The Menu that contains the MenuInventoryBox element</param>
		 * <param name = "inventoryBox">The MenuInventoryBox element that was clicked on</param>
		 * <param name = "_slot">The index number of the MenuInventoryBox slot that was clicked on</param>
		 * <param name = "_mouseState">The state of the mouse when the click occured (Normal, SingleClick, RightClick, DoubleClick, HeldDown, LetGo)</param>
		 */
		public void ProcessInventoryBoxClick (AC.Menu _menu, MenuInventoryBox inventoryBox, int _slot, MouseState _mouseState)
		{
			if (inventoryBox.inventoryBoxType == AC_InventoryBoxType.Default || inventoryBox.inventoryBoxType == AC_InventoryBoxType.DisplayLastSelected)
			{
				if (KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple && KickStarter.playerMenus.IsInteractionMenuOn ())
				{
					KickStarter.playerMenus.SetInteractionMenus (false);
					ClickInvItemToInteract ();
				}
				else if (KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple && KickStarter.settingsManager.SelectInteractionMethod () == AC.SelectInteractions.CyclingCursorAndClickingHotspot)
				{
					if (KickStarter.settingsManager.autoCycleWhenInteract && _mouseState == MouseState.SingleClick && (selectedItem == null || KickStarter.settingsManager.cycleInventoryCursors))
					{
						int originalIndex = KickStarter.playerInteraction.GetInteractionIndex ();
						KickStarter.playerInteraction.SetNextInteraction ();
						KickStarter.playerInteraction.SetInteractionIndex (originalIndex);
					}

					if (!KickStarter.settingsManager.cycleInventoryCursors && selectedItem != null)
					{
						inventoryBox.HandleDefaultClick (_mouseState, _slot, KickStarter.settingsManager.interactionMethod);
					}
					else if (_mouseState != MouseState.RightClick)
					{
						KickStarter.playerMenus.SetInteractionMenus (false);
						ClickInvItemToInteract ();
					}
					
					if (KickStarter.settingsManager.autoCycleWhenInteract && _mouseState == MouseState.SingleClick)
					{
						KickStarter.playerInteraction.RestoreInventoryInteraction ();
					}
				}
				else if (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ContextSensitive && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Single)
				{
					inventoryBox.HandleDefaultClick (_mouseState, _slot, AC_InteractionMethod.ContextSensitive);
				}
				else
				{
					inventoryBox.HandleDefaultClick (_mouseState, _slot, KickStarter.settingsManager.interactionMethod);
				}
				
				_menu.Recalculate ();
			}
			else if (inventoryBox.inventoryBoxType == AC_InventoryBoxType.Container)
			{
				inventoryBox.ClickContainer (_mouseState, _slot, KickStarter.playerInput.activeContainer);
				_menu.Recalculate ();
			}
			else if (inventoryBox.inventoryBoxType == AC_InventoryBoxType.HotspotBased)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (_menu.GetTargetInvItem () != null)
					{
						//Combine (hoverItem, inventoryBox.items [_slot]);
						Combine (_menu.GetTargetInvItem (), inventoryBox.items [_slot + inventoryBox.GetOffset ()]);
					}
					else if (_menu.GetTargetHotspot ())
					{
						InvItem _item = inventoryBox.items [_slot + inventoryBox.GetOffset ()];
						if (_item != null)
						{
							//SelectItem (_item, SelectItemMode.Use);
							_menu.TurnOff (false);
							KickStarter.playerInteraction.ClickButton (InteractionType.Inventory, -2, _item.id, _menu.GetTargetHotspot ());
							KickStarter.playerCursor.ResetSelectedCursor ();
						}
					}
					else
					{
						ACDebug.LogWarning ("Cannot handle inventory click since there is no active Hotspot.");
					}
				}
				else
				{
					ACDebug.LogWarning ("This type of InventoryBox only works with the Choose Hotspot Then Interaction method of interaction.");
				}
			}
		}


		/**
		 * <summary>Gets the total value of all instances of an Integer inventory property (e.g. currency) within the player's inventory.</summary>
		 * <param name = "ID">The ID number of the Inventory property (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Integer inventory property within the player's inventory</returns>
		 */
		public int GetTotalIntProperty (int ID)
		{
			return GetTotalIntProperty (localItems.ToArray (), ID);
		}


		/**
		 * <summary>Gets the total value of all instances of an Integer inventory property (e.g. currency) within a set of inventory items.</summary>
		 * <param name = "items">The inventory items to get the total value from</param>
		 * <param name = "ID">The ID number of the Inventory property (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Integer inventory property within the set of inventory items</returns>
		 */
		public int GetTotalIntProperty (InvItem[] items, int ID)
		{
			int result = 0;
			foreach (InvItem item in items)
			{
				foreach (InvVar var in item.vars)
				{
					if (var.id == ID && var.type == VariableType.Integer)
					{
						result += var.val;
						break;
					}
				}
			}
			return result;
		}


		/**
		 * <summary>Gets the total value of all instances of an Float inventory property (e.g. weight) within the player's inventory.</summary>
		 * <param name = "ID">The ID number of the Inventory Float (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Float inventory property within the player's inventory</returns>
		 */
		public float GetTotalFloatProperty (int ID)
		{
			return GetTotalFloatProperty (localItems.ToArray (), ID);
		}
		
		
		/**
		 * <summary>Gets the total value of all instances of an Float inventory property (e.g. weight) within a set of inventory items.</summary>
		 * <param name = "items">The inventory items to get the total value from</param>
		 * <param name = "ID">The ID number of the Inventory property (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Float inventory property within the set of inventory items</returns>
		 */
		public float GetTotalFloatProperty (InvItem[] items, int ID)
		{
			float result = 0f;
			foreach (InvItem item in items)
			{
				foreach (InvVar var in item.vars)
				{
					if (var.id == ID && var.type == VariableType.Float)
					{
						result += var.floatVal;
						break;
					}
				}
			}
			return result;
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			if (selectedItem != null)
			{
				mainData.selectedInventoryID = selectedItem.id;
				mainData.isGivingItem = IsGivingItem ();
			}
			else
			{
				mainData.selectedInventoryID = -1;
			}

			return mainData;
		}


		public InvVar GetPropertyTotals (int ID)
		{
			InvVar totalVar = new InvVar ();

			foreach (InvItem item in localItems)
			{
				InvVar var = item.GetProperty (ID);
				if (var != null)
				{
					totalVar.val += var.val;
					totalVar.floatVal += var.floatVal;
				}
			}
			return totalVar;
		}
		
	}
	
}
