/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"MenuInventoryBox.cs"
 * 
 *	This MenuElement lists all inventory items held by the player.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that lists inventory items (see: InvItem).
	 * It can be used to display all inventory items held by the player, those that are stored within a Container, or as part of an Interaction Menu.
	 */
	public class MenuInventoryBox : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** How the items to display are chosen (Default, HotspotBased, CustomScript, DisplaySelected, DisplayLastSelected, Container) */
		public AC_InventoryBoxType inventoryBoxType;
		/** The maximum number of inventory items that can be shown at once */
		public int maxSlots;
		/** If True, only inventory items (InvItem) with a specific category will be displayed */
		public bool limitToCategory;
		/** If True, then only inventory items that are listed in a Hotspot's / InvItem's interactions will be listed if inventoryBoxType = AC_InventoryBoxType.HotspotBased */
		public bool limitToDefinedInteractions = true;
		/** The category ID to limit the display of inventory items by, if limitToCategory = True (Deprecated) */
		public int categoryID;
		/** The category IDs to limit the display of inventory items by, if limitToCategory = True */
		public List<int> categoryIDs = new List<int>();

		/** The List of inventory items that are on display */
		public List<InvItem> items = new List<InvItem>();
		/** If True, and inventoryBoxType = AC_InventoryBoxType.Container, then items will be selected automatically when they are removed from the container */
		public bool selectItemsAfterTaking = true;
		/** How items are displayed (IconOnly, TextOnly) */
		public ConversationDisplayType displayType = ConversationDisplayType.IconOnly;
		/** The method which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;

		private string[] labels = null;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiSlots = null;

			isVisible = true;
			isClickable = true;
			inventoryBoxType = AC_InventoryBoxType.Default;
			numSlots = 0;
			SetSize (new Vector2 (6f, 10f));
			maxSlots = 10;
			limitToCategory = false;
			limitToDefinedInteractions = true;
			selectItemsAfterTaking = true;
			categoryID = -1;
			displayType = ConversationDisplayType.IconOnly;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			uiHideStyle = UIHideStyle.DisableObject;
			items = new List<InvItem>();
			categoryIDs = new List<int>();
		}


		/**
		 * <summary>Creates and returns a new MenuInventoryBox that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <returns>A new MenuInventoryBox with the same values as itself</return>
		 */
		public override MenuElement DuplicateSelf (bool fromEditor)
		{
			MenuInventoryBox newElement = CreateInstance <MenuInventoryBox>();
			newElement.Declare ();
			newElement.CopyInventoryBox (this);
			return newElement;
		}
		
		
		private void CopyInventoryBox (MenuInventoryBox _element)
		{
			uiSlots = _element.uiSlots;

			isClickable = _element.isClickable;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			inventoryBoxType = _element.inventoryBoxType;
			numSlots = _element.numSlots;
			maxSlots = _element.maxSlots;
			limitToCategory = _element.limitToCategory;
			limitToDefinedInteractions = _element.limitToDefinedInteractions;
			categoryID = _element.categoryID;
			selectItemsAfterTaking = _element.selectItemsAfterTaking;
			displayType = _element.displayType;
			uiHideStyle = _element.uiHideStyle;
			categoryIDs = _element.categoryIDs;

			UpdateLimitCategory ();

			PopulateList ();

			base.Copy (_element);

			Upgrade ();
		}


		private void Upgrade ()
		{
			if (limitToCategory && categoryID >= 0)
			{
				categoryIDs.Add (categoryID);
				categoryID = -1;

				if (Application.isPlaying)
				{
					ACDebug.Log ("The inventory box element '" + title + "' has been upgraded - please view it in the Menu Manager and Save.");
				}
			}
		}


		private void UpdateLimitCategory ()
		{
			if (Application.isPlaying && AdvGame.GetReferences ().inventoryManager != null && AdvGame.GetReferences ().inventoryManager.bins != null)
			{
				foreach (InvBin invBin in KickStarter.inventoryManager.bins)
				{
					if (categoryIDs.Contains (invBin.id))
					{
						// Fine!
					}
					else
					{
						categoryIDs.Remove (invBin.id);
					}
				}
			}
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObjects.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu)
		{
			int i=0;
			foreach (UISlot uiSlot in uiSlots)
			{
				uiSlot.LinkUIElements ();
				if (uiSlot != null && uiSlot.uiButton != null)
				{
					int j=i;
					uiSlot.uiButton.onClick.AddListener (() => {
						ProcessClickUI (_menu, j, MouseState.SingleClick);
					});
					uiSlot.AddClickHandler (_menu, this, j);
				}
				i++;
			}
		}


		/**
		 * <summary>Gets the linked Unity UI GameObject associated with this element.</summary>
		 * <returns>The Unity UI GameObject associated with the element</returns>
		 */
		public override GameObject GetObjectToSelect ()
		{
			if (uiSlots != null && uiSlots.Length > 0 && uiSlots[0].uiButton != null)
			{
				return uiSlots[0].uiButton.gameObject;
			}
			return null;
		}
		

		/**
		 * <summary>Gets the boundary of the slot</summary>
		 * <param name = "_slot">The index number of the slot to get the boundary of</param>
		 * <returns>The boundary Rect of the slot</returns>
		 */
		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiSlots != null && uiSlots.Length > _slot)
			{
				return uiSlots[_slot].GetRectTransform ();
			}
			return null;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");
			if (source == MenuSource.AdventureCreator)
			{
				textEffects = (TextEffects) EditorGUILayout.EnumPopup ("Text effect:", textEffects);
				if (textEffects != TextEffects.None)
				{
					outlineSize = EditorGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f);
				}
			}
			displayType = (ConversationDisplayType) EditorGUILayout.EnumPopup ("Display:", displayType);
			inventoryBoxType = (AC_InventoryBoxType) EditorGUILayout.EnumPopup ("Inventory box type:", inventoryBoxType);
			if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.CustomScript)
			{
				maxSlots = EditorGUILayout.IntSlider ("Max number of slots:", maxSlots, 1, 30);
				isClickable = true;
			}
			else if (inventoryBoxType == AC_InventoryBoxType.DisplaySelected)
			{
				isClickable = false;
				maxSlots = 1;
			}
			else if (inventoryBoxType == AC_InventoryBoxType.DisplayLastSelected)
			{
				isClickable = true;
				maxSlots = 1;
			}
			else if (inventoryBoxType == AC_InventoryBoxType.Container)
			{
				isClickable = true;
				maxSlots = EditorGUILayout.IntSlider ("Max number of slots:", maxSlots, 1, 30);
				selectItemsAfterTaking = EditorGUILayout.Toggle ("Select item after taking?", selectItemsAfterTaking);
			}
			else
			{
				isClickable = true;
				if (source == MenuSource.AdventureCreator)
				{
					numSlots = EditorGUILayout.IntField ("Test slots:", numSlots);
				}
				maxSlots = EditorGUILayout.IntSlider ("Max number of slots:", maxSlots, 1, 30);
			}

			if (inventoryBoxType == AC_InventoryBoxType.HotspotBased)
			{
				limitToDefinedInteractions = EditorGUILayout.ToggleLeft ("Only show items referenced in Interactions?", limitToDefinedInteractions);
			}

			if (inventoryBoxType != AC_InventoryBoxType.DisplaySelected && inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected && source == MenuSource.AdventureCreator)
			{
				slotSpacing = EditorGUILayout.Slider ("Slot spacing:", slotSpacing, 0f, 20f);
				orientation = (ElementOrientation) EditorGUILayout.EnumPopup ("Slot orientation:", orientation);
				if (orientation == ElementOrientation.Grid)
				{
					gridWidth = EditorGUILayout.IntSlider ("Grid size:", gridWidth, 1, 10);
				}
			}
			
			if (inventoryBoxType == AC_InventoryBoxType.CustomScript)
			{
				ShowClipHelp ();
			}

			uiHideStyle = (UIHideStyle) EditorGUILayout.EnumPopup ("When slot is empty:", uiHideStyle);

			if (source != MenuSource.AdventureCreator)
			{
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				uiSlots = ResizeUISlots (uiSlots, maxSlots);
				
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}
			}
			EditorGUILayout.EndVertical ();

			ShowCategoriesUI ();

			base.ShowGUI (menu);
		}


		private void ShowCategoriesUI ()
		{
			EditorGUILayout.BeginVertical ("Button");
			limitToCategory = EditorGUILayout.Toggle ("Limit by category?", limitToCategory);
			if (limitToCategory)
			{
				Upgrade ();

				if (AdvGame.GetReferences ().inventoryManager)
				{
					List<InvBin> bins = AdvGame.GetReferences ().inventoryManager.bins;

					if (bins == null || bins.Count == 0)
					{
						categoryIDs.Clear ();
						EditorGUILayout.HelpBox ("No categories defined!", MessageType.Warning);
					}
					else
					{
						for (int i=0; i<bins.Count; i++)
						{
							bool include = (categoryIDs.Contains (bins[i].id)) ? true : false;
							include = EditorGUILayout.ToggleLeft (" " + i.ToString () + ": " + bins[i].label, include);

							if (include)
							{
								if (!categoryIDs.Contains (bins[i].id))
								{
									categoryIDs.Add (bins[i].id);
								}
							}
							else
							{
								if (categoryIDs.Contains (bins[i].id))
								{
									categoryIDs.Remove (bins[i].id);
								}
							}
						}

						if (categoryIDs.Count == 0)
						{
							EditorGUILayout.HelpBox ("At least one category must be checked for this to take effect.", MessageType.Info);
						}
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("No Inventory Manager defined!", MessageType.Warning);
					categoryIDs.Clear ();
				}
			}
			EditorGUILayout.EndVertical ();
		}


		private int GetBinSlot (int _id, List<InvBin> bins)
		{
			int i = 0;
			foreach (InvBin bin in bins)
			{
				if (bin.id == _id)
				{
					return i;
				}
				i++;
			}
			
			return 0;
		}
		
		#endif


		/**
		 * Hides all linked Unity UI GameObjects associated with the element.
		 */
		public override void HideAllUISlots ()
		{
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
		}


		public override void SetUIInteractableState (bool state)
		{
			SetUISlotsInteractableState (uiSlots, state);
		}
		

		/**
		 * <summary>Performs all calculations necessary to display the element.</summary>
		 * <param name = "_slot">The index number of the slot to display</param>
		 * <param name = "languageNumber">The index number of the language to display text in</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (items.Count > 0 && items.Count > (_slot+offset) && items [_slot+offset] != null)
			{
				string fullText = "";

				if (displayType == ConversationDisplayType.TextOnly)
				{
					fullText = items [_slot+offset].label;
					if (KickStarter.runtimeInventory != null)
					{
						fullText = KickStarter.runtimeInventory.GetLabel (items [_slot+offset], languageNumber);
					}
					string countText = GetCount (_slot);
					if (countText != "")
					{
						fullText += " (" + countText + ")";
					}
				}
				else
				{
					string countText = GetCount (_slot);
					if (countText != "")
					{
						fullText = countText;
					}
				}

				if (labels == null || labels.Length != numSlots)
				{
					labels = new string [numSlots];
				}
				labels [_slot] = fullText;
			}

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);

					uiSlots[_slot].SetText (labels [_slot]);
					if (displayType == ConversationDisplayType.IconOnly)
					{
						Texture2D tex = null;
						if (items.Count > (_slot+offset) && items [_slot+offset] != null)
						{
							if (inventoryBoxType != AC_InventoryBoxType.DisplaySelected && inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected)
							{
								if (KickStarter.settingsManager.selectInventoryDisplay == SelectInventoryDisplay.HideFromMenu && ItemIsSelected (items [_slot+offset]))
								{
									uiSlots[_slot].SetImage (null);
									labels [_slot] = "";
									return;
								}
								tex = GetTexture (items [_slot+offset], isActive);
							}

							if (tex == null)
							{
								tex = items [_slot+offset].tex;
							}
						}
						uiSlots[_slot].SetImage (tex);
					}
				}
			}
		}


		private bool ItemIsSelected (InvItem item)
		{
			if (item != null && item == KickStarter.runtimeInventory.selectedItem && (!KickStarter.settingsManager.inventoryDragDrop || KickStarter.playerInput.GetDragState () == DragState.Inventory))
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Draws the element using OnGUI</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">The index number of the slot to display</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);
			_style.wordWrap = true;
			
			if (items.Count > 0 && items.Count > (_slot+offset) && items [_slot+offset] != null)
			{
				if (Application.isPlaying && KickStarter.settingsManager.selectInventoryDisplay == SelectInventoryDisplay.HideFromMenu && ItemIsSelected (items [_slot+offset]))
				{
					return;
				}
			
				if (displayType == ConversationDisplayType.IconOnly)
				{
					GUI.Label (GetSlotRectRelative (_slot), "", _style);
					DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), items [_slot+offset], isActive);
					_style.normal.background = null;
					
					if (textEffects != TextEffects.None)
					{
						AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), GetCount (_slot), _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
					}
					else
					{
						GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), GetCount (_slot), _style);
					}
				}
				else if (displayType == ConversationDisplayType.TextOnly)
				{
					if (textEffects != TextEffects.None)
					{
						AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
					}
					else
					{
						GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style);
					}
				}
			}
		}
		

		/**
		 * <summary>Performs what should happen when the element is clicked on, if inventoryBoxType = AC_InventoryBoxType.Default.</summary>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 * <param name = "_slot">The index number of the slot that was clicked on</param>
		 * <param name = "interactionMethod">The game's interaction method (ContextSensitive, ChooseHotspotThenInteraction, ChooseInteractionThenHotspot)</param>
		 */
		public void HandleDefaultClick (MouseState _mouseState, int _slot, AC_InteractionMethod interactionMethod)
		{
			if (!KickStarter.settingsManager.allowInventoryInteractionsDuringConversations && KickStarter.stateHandler.gameState == GameState.DialogOptions)
			{
				return;
			}

			if (KickStarter.runtimeInventory != null)
			{
				KickStarter.playerMenus.SetInteractionMenus (false);

				KickStarter.runtimeInventory.HighlightItemOffInstant ();
				KickStarter.runtimeInventory.SetFont (font, GetFontSize (), fontColor, textEffects);

				if (inventoryBoxType == AC_InventoryBoxType.Default)
				{
					if (items.Count <= (_slot + offset) || items[_slot+offset] == null)
					{
						// Black space
						if (KickStarter.runtimeInventory.selectedItem != null && KickStarter.settingsManager.canReorderItems)
						{
							if (!limitToCategory)
							{
								KickStarter.runtimeInventory.localItems = KickStarter.runtimeInventory.MoveItemToIndex (KickStarter.runtimeInventory.selectedItem, KickStarter.runtimeInventory.localItems, _slot + offset);
							}
							else
							{
								ACDebug.Log ("Cannot re-order inventory items in InventoryBox elements that are limited to a particular inventory category.");
							}
						}
						KickStarter.runtimeInventory.SetNull ();
						return;
					}
				}

				if (interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.runtimeInventory.selectedItem != null)
					{
						if (_mouseState == MouseState.SingleClick)
						{
							KickStarter.runtimeInventory.Combine (KickStarter.runtimeInventory.selectedItem, items [_slot + offset]);
						}
						else if (_mouseState == MouseState.RightClick)
						{
							KickStarter.runtimeInventory.SetNull ();
						}
					}
					else
					{
						KickStarter.runtimeInventory.ShowInteractions (items [_slot + offset]);
					}
				}
				else if (interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					if (_mouseState == MouseState.SingleClick)
					{
						int cursorID = KickStarter.playerCursor.GetSelectedCursorID ();
						int cursor = KickStarter.playerCursor.GetSelectedCursor ();

						if (cursor == -2 && KickStarter.runtimeInventory.selectedItem != null)
						{
							if (items [_slot + offset] == KickStarter.runtimeInventory.selectedItem)
							{
								KickStarter.runtimeInventory.SelectItem (items [_slot + offset], SelectItemMode.Use);
							}
							else
							{
								KickStarter.runtimeInventory.Combine (KickStarter.runtimeInventory.selectedItem, items [_slot + offset]);
							}
						}
						else if (cursor == -1 && !KickStarter.settingsManager.selectInvWithUnhandled)
						{
							KickStarter.runtimeInventory.SelectItem (items [_slot + offset], SelectItemMode.Use);
						}
						else if (cursorID > -1)
						{
							KickStarter.runtimeInventory.RunInteraction (items [_slot + offset], cursorID);
						}
					}
				}
				else if (interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					if (_mouseState == MouseState.SingleClick)
					{
						if (KickStarter.runtimeInventory.selectedItem == null)
						{
							KickStarter.runtimeInventory.Use (items [_slot + offset]);
						}
						else
						{
							KickStarter.runtimeInventory.Combine (KickStarter.runtimeInventory.selectedItem, items [_slot + offset]);
						}
					}
					else if (_mouseState == MouseState.RightClick)
					{
						if (KickStarter.runtimeInventory.selectedItem == null)
						{
							KickStarter.runtimeInventory.Look (items [_slot + offset]);
						}
						else
						{
							KickStarter.runtimeInventory.SetNull ();
						}
					}
				}
			}
		}
		

		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void RecalculateSize (MenuSource source)
		{
			PopulateList ();

			if (inventoryBoxType == AC_InventoryBoxType.HotspotBased)
			{
				if (Application.isPlaying)
				{
					numSlots = Mathf.Clamp (items.Count, 0, maxSlots);
				}
				else
				{
					numSlots = Mathf.Clamp (numSlots, 0, maxSlots);
				}
			}
			else
			{
				numSlots = maxSlots;
			}

			if (uiHideStyle == UIHideStyle.DisableObject)
			{
				if (numSlots > items.Count)
				{
					offset = 0;
					numSlots = items.Count;
				}
			}

			LimitOffset (items.Count);

			labels = new string [numSlots];

			if (Application.isPlaying && uiSlots != null)
			{
				ClearSpriteCache (uiSlots);
			}

			if (!isVisible)
			{
				LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
			}

			base.RecalculateSize (source);
		}
		
		
		private void PopulateList ()
		{
			if (Application.isPlaying)
			{
				if (inventoryBoxType == AC_InventoryBoxType.HotspotBased)
				{
					if (limitToDefinedInteractions)
					{
						items = KickStarter.runtimeInventory.MatchInteractions ();
					}
					else
					{
						items = KickStarter.runtimeInventory.localItems;
					}
				}
				else if (inventoryBoxType == AC_InventoryBoxType.DisplaySelected)
				{
					items = KickStarter.runtimeInventory.GetSelected ();
				}
				else if (inventoryBoxType == AC_InventoryBoxType.DisplayLastSelected)
				{
					if (KickStarter.runtimeInventory.selectedItem != null)
					{
						items = new List<InvItem>();
						items = KickStarter.runtimeInventory.GetSelected ();
					}
					else if (items.Count == 1 && !KickStarter.runtimeInventory.IsItemCarried (items[0]))
					{
						items.Clear ();
					}
				}
				else if (inventoryBoxType == AC_InventoryBoxType.Container)
				{
					if (KickStarter.playerInput.activeContainer)
					{
						items.Clear ();
						foreach (ContainerItem containerItem in KickStarter.playerInput.activeContainer.items)
						{
							InvItem referencedItem = new InvItem (KickStarter.inventoryManager.GetItem (containerItem.linkedID));
							referencedItem.count = containerItem.count;
							items.Add (referencedItem);
						}
					}
				}
				else
				{
					items = new List<InvItem>();
					foreach (InvItem _item in KickStarter.runtimeInventory.localItems)
					{
						items.Add (_item);
					}
				}
			}
			else
			{
				items = new List<InvItem>();
				if (AdvGame.GetReferences ().inventoryManager)
				{
					foreach (InvItem _item in AdvGame.GetReferences ().inventoryManager.items)
					{
						items.Add (_item);
						if (_item != null)
						{
							_item.recipeSlot = -1;
						}
					}
				}
			}

			if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.CustomScript)
			{
				LimitByCategory ();

				while (AreAnyItemsInRecipe ())
				{
					foreach (InvItem _item in items)
					{
						if (_item != null && _item.recipeSlot > -1)
						{
							if (AdvGame.GetReferences ().settingsManager.canReorderItems)
								items [items.IndexOf (_item)] = null;
							else
								items.Remove (_item);
							break;
						}
					}
				}
			}
		}


		/**
		 * <summary>Checks if the element's slots can be shifted in a particular direction.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <returns>True if the element's slots can be shifted in the particular direction</returns>
		 */
		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (items.Count == 0)
			{
				return false;
			}

			if (shiftType == AC_ShiftInventory.ShiftLeft)
			{
				if (offset == 0)
				{
					return false;
				}
			}
			else
			{
				if ((maxSlots + offset) >= items.Count)
				{
					return false;
				}
			}
			return true;
		}


		private bool AreAnyItemsInRecipe ()
		{
			foreach (InvItem item in items)
			{
				if (item != null && item.recipeSlot >= 0)
				{
					return true;
				}
			}
			return false;
		}


		private void LimitByCategory ()
		{
			if (limitToCategory && categoryIDs.Count > 0)
			{
				for (int i=0; i<items.Count; i++)
				{
					if (items[i] != null && !categoryIDs.Contains (items[i].binID))
					{
						items.RemoveAt (i);
						i = -1;
					}
				}
			}
		}
		

		/**
		 * <summary>Shifts which slots are on display, if the number of slots the element has exceeds the number of slots it can show at once.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <param name = "amount">The amount to shift slots by</param>
		 */
		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (numSlots >= maxSlots)
			{
				Shift (shiftType, maxSlots, items.Count, amount);
			}
		}


		private Texture2D GetTexture (InvItem _item, bool isActive)
		{
			if (ItemIsSelected (_item))
			{
				switch (KickStarter.settingsManager.selectInventoryDisplay)
				{
				case SelectInventoryDisplay.ShowSelectedGraphic:
					return _item.selectedTex;

				case SelectInventoryDisplay.ShowHoverGraphic:
					return _item.activeTex;

				default:
					break;
				}
			}
			else if (isActive && KickStarter.settingsManager.activeWhenHover)
			{
				return _item.activeTex;
			}
			return _item.tex;
		}
		
		
		private void DrawTexture (Rect rect, InvItem _item, bool isActive)
		{
			if (_item == null) return;

			Texture2D tex = null;
			if (Application.isPlaying && KickStarter.runtimeInventory != null && inventoryBoxType != AC_InventoryBoxType.DisplaySelected)
			{
				if (_item == KickStarter.runtimeInventory.highlightItem && _item.activeTex != null)
				{
					KickStarter.runtimeInventory.DrawHighlighted (rect);
					return;
				}

				if (inventoryBoxType != AC_InventoryBoxType.DisplaySelected && inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected)
				{
					tex = GetTexture (_item, isActive);
				}

				if (tex == null)
				{
					tex = _item.tex;
				}
			}
			else if (_item.tex != null)
			{
				tex = _item.tex;
			}

			if (tex != null)
			{
				GUI.DrawTexture (rect, tex, ScaleMode.StretchToFill, true, 0f);
			}
		}


		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "slot">The index number of the slot</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int i, int languageNumber)
		{
			if (items.Count <= (i+offset) || items [i+offset] == null)
			{
				return null;
			}

			return items [i+offset].GetLabel (languageNumber);
		}


		/**
		 * <summary>Gets the inventory item shown in a specific slot</summary>
		 * <param name = "i">The index number of the slot</param>
		 * <returns>The inventory item shown in the slot</returns>
		 */
		public InvItem GetItem (int i)
		{
			if (items.Count <= (i+offset) || items [i+offset] == null)
			{
				return null;
			}

			return items [i+offset];
		}


		private string GetCount (int i)
		{
			if (items.Count <= (i+offset) || items [i+offset] == null)
			{
				return "";
			}

			if (items [i+offset].count < 2)
			{
				return "";
			}
			return items [i + offset].count.ToString ();
		}


		/**
		 * Re-sets the "shift" offset, so that the first InvItem shown is the first InvItem in items.
		 */
		public void ResetOffset ()
		{
			offset = 0;
		}
		
		
		protected override void AutoSize ()
		{
			if (items.Count > 0)
			{
				foreach (InvItem _item in items)
				{
					if (_item != null)
					{
						if (displayType == ConversationDisplayType.IconOnly)
						{
							AutoSize (new GUIContent (_item.tex));
						}
						else if (displayType == ConversationDisplayType.TextOnly)
						{
							AutoSize (new GUIContent (_item.label));
						}
						return;
					}
				}
			}
			AutoSize (GUIContent.none);
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on, if inventoryBoxType = AC_InventoryBoxType.Container.</summary>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 * <param name = "_slot">The index number of the slot that was clicked on</param>
		 * <param name = "container">The Container that the element references</param>
		 */
		public void ClickContainer (MouseState _mouseState, int _slot, Container container)
		{
			if (container == null || KickStarter.runtimeInventory == null) return;

			KickStarter.runtimeInventory.SetFont (font, GetFontSize (), fontColor, textEffects);

			if (_mouseState == MouseState.SingleClick)
			{
				if (KickStarter.runtimeInventory.selectedItem == null)
				{
					if (container.items.Count > (_slot+offset) && container.items [_slot+offset] != null)
					{
						ContainerItem containerItem = container.items [_slot + offset];
						KickStarter.runtimeInventory.Add (containerItem.linkedID, containerItem.count, selectItemsAfterTaking, -1);
						container.items.Remove (containerItem);
					}
				}
				else
				{
					// Placing an item inside the container
					container.InsertAt (KickStarter.runtimeInventory.selectedItem, _slot+offset);
					KickStarter.runtimeInventory.Remove (KickStarter.runtimeInventory.selectedItem);
				}
			}

			else if (_mouseState == MouseState.RightClick)
			{
				if (KickStarter.runtimeInventory.selectedItem != null)
				{
					KickStarter.runtimeInventory.SetNull ();
				}
			}
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_slot">The index number of ths slot that was clicked</param>
		 * <param name = "_mouseState The state of the mouse button</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return;
			}

			base.ProcessClick (_menu, _slot, _mouseState);

			if (_mouseState == MouseState.SingleClick)
			{
				KickStarter.runtimeInventory.lastClickedItem = GetItem (_slot);
			}

			if (inventoryBoxType == AC_InventoryBoxType.CustomScript)
			{
				MenuSystem.OnElementClick (_menu, this, _slot, (int) _mouseState);
			}
			else
			{
				KickStarter.runtimeInventory.ProcessInventoryBoxClick (_menu, this, _slot, _mouseState);
			}
		}


		/**
		 * <summary>Gets the slot index number that a given InvItem (inventory item) appears in.</summary>
		 * <param name = "itemID">The ID number of the InvItem to search for</param>
		 * <returns>The slot index number that the inventory item appears in</returns>
		 */
		public int GetItemSlot (int itemID)
		{
			foreach (InvItem invItem in items)
			{
				if (invItem.id == itemID)
				{
					return items.IndexOf (invItem) - offset;
				}
			}
			return 0;
		}

	}

}