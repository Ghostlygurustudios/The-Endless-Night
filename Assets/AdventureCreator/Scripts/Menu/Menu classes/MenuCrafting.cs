/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"MenuCrafting.cs"
 * 
 *	This MenuElement stores multiple Inventory Items to be combined.
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
	 * A MenuElement that stores multiple inventory items to be combined to create new ones.
	 */
	public class MenuCrafting : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;

		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** What part of the crafting process this element is used for (Ingredients, Output) */
		public CraftingElementType craftingType = CraftingElementType.Ingredients;
		/** The List of InvItem instances that are currently on display */
		public List<InvItem> items = new List<InvItem>();
		/** How items are displayed (IconOnly, TextOnly) */
		public ConversationDisplayType displayType = ConversationDisplayType.IconOnly;
		/** The method which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;

		private Recipe activeRecipe;
		private bool[] isFilled;
		private string[] labels = null;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiSlots = null;
			isVisible = true;
			isClickable = true;
			numSlots = 4;
			SetSize (new Vector2 (6f, 10f));
			textEffects = TextEffects.None;
			outlineSize = 2f;
			craftingType = CraftingElementType.Ingredients;
			displayType = ConversationDisplayType.IconOnly;
			uiHideStyle = UIHideStyle.DisableObject;
			items = new List<InvItem>();
		}


		/**
		 * <summary>Creates and returns a new MenuCrafting that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <returns>A new MenuCrafting with the same values as itself</returns>
		 */
		public override MenuElement DuplicateSelf (bool fromEditor)
		{
			MenuCrafting newElement = CreateInstance <MenuCrafting>();
			newElement.Declare ();
			newElement.CopyCrafting (this);
			return newElement;
		}
		
		
		private void CopyCrafting (MenuCrafting _element)
		{
			uiSlots = _element.uiSlots;
			isClickable = _element.isClickable;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			numSlots = _element.numSlots;
			craftingType = _element.craftingType;
			displayType = _element.displayType;
			uiHideStyle = _element.uiHideStyle;

			PopulateList (MenuSource.AdventureCreator);
			
			base.Copy (_element);
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
				}
				i++;
			}
		}


		/**
		 * <summary>Gets the first linked Unity UI GameObject associated with this element.</summary>
		 * <returns>The first Unity UI GameObject associated with the element</returns>
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


		public override void SetUIInteractableState (bool state)
		{
			SetUISlotsInteractableState (uiSlots, state);
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
			craftingType = (CraftingElementType) EditorGUILayout.EnumPopup ("Crafting element type:", craftingType);

			if (craftingType == CraftingElementType.Ingredients)
			{
				numSlots = EditorGUILayout.IntSlider ("Number of slots:", numSlots, 1, 12);
				if (source == MenuSource.AdventureCreator)
				{
					slotSpacing = EditorGUILayout.Slider ("Slot spacing:", slotSpacing, 0f, 20f);
					orientation = (ElementOrientation) EditorGUILayout.EnumPopup ("Slot orientation:", orientation);
					if (orientation == ElementOrientation.Grid)
					{
						gridWidth = EditorGUILayout.IntSlider ("Grid size:", gridWidth, 1, 10);
					}
				}
			}
			else
			{
				numSlots = 1;
			}

			if (source != MenuSource.AdventureCreator)
			{
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
				uiHideStyle = (UIHideStyle) EditorGUILayout.EnumPopup ("When invisible:", uiHideStyle);
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				uiSlots = ResizeUISlots (uiSlots, numSlots);
				
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}
			}

			isClickable = true;
			EditorGUILayout.EndVertical ();
			
			PopulateList (source);
			base.ShowGUI (menu);
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
		
		
		/**
		 * <summary>Performs all calculations necessary to display the element.</summary>
		 * <param name = "_slot">The index number of the slot to display</param>
		 * <param name = "languageNumber">The index number of the language to display text in</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			string fullText = "";
			if (displayType == ConversationDisplayType.TextOnly)
			{
				InvItem item = GetItem (_slot);
				if (item != null)
				{
					fullText = item.label;
					if (KickStarter.runtimeInventory != null)
					{
						fullText = KickStarter.runtimeInventory.GetLabel (item, languageNumber);
					}
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

			if (craftingType == CraftingElementType.Ingredients)
			{
				if (isFilled == null || isFilled.Length != numSlots)
				{
					isFilled = new bool [numSlots];
				}
			
				// Is slot filled?
				isFilled [_slot] = false;
				foreach (InvItem _item in items)
				{
					if (_item.recipeSlot == _slot)
					{
						isFilled [_slot] = true;
						break;
					}
				}
			}

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);

					uiSlots[_slot].SetText (labels [_slot]);

					if (displayType == ConversationDisplayType.IconOnly)
					{
						if ((craftingType == CraftingElementType.Ingredients && isFilled [_slot]) || (craftingType == CraftingElementType.Output && items.Count > 0))
						{
							uiSlots[_slot].SetImage (GetTexture (_slot));
						}
						else
						{
							uiSlots[_slot].SetImage (null);
						}
					}
				}
			}
		}


		/**
		 * <summary>Draws the element using OnGUI</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">The index number of the slot to display</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			if (craftingType == CraftingElementType.Ingredients)
			{
				if (displayType == ConversationDisplayType.IconOnly)
				{
					GUI.Label (GetSlotRectRelative (_slot), "", _style);

					if (!isFilled [_slot])
					{
						return;
					}
					DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), _slot);
					_style.normal.background = null;
									}
				else
				{
					if (!isFilled [_slot])
					{
						GUI.Label (GetSlotRectRelative (_slot), "", _style);
						return;
					}
				}

				DrawText (_style, _slot, zoom);
			}
			else if (craftingType == CraftingElementType.Output)
			{
				GUI.Label (GetSlotRectRelative (_slot), "", _style);
				if (items.Count > 0)
				{
					if (displayType == ConversationDisplayType.IconOnly)
					{
						DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), _slot);
					}
					DrawText (_style, _slot, zoom);
				}
			}
		}


		private void DrawText (GUIStyle _style, int _slot, float zoom)
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


		private void HandleDefaultClick (MouseState _mouseState, int _slot)
		{
			if (craftingType == CraftingElementType.Ingredients)
			{
				if (_mouseState == MouseState.SingleClick)
				{
					if (KickStarter.runtimeInventory.selectedItem == null)
					{
						if (GetItem (_slot) != null)
						{
							KickStarter.runtimeInventory.TransferCraftingToLocal (GetItem (_slot).recipeSlot, true);
						}
					}
					else
					{
						if (GetItem (_slot) != null)
						{
							KickStarter.runtimeInventory.TransferCraftingToLocal (GetItem (_slot).recipeSlot, false);
						}

						KickStarter.runtimeInventory.TransferLocalToCrafting (KickStarter.runtimeInventory.selectedItem, _slot);
					}
				}
				else if (_mouseState == MouseState.RightClick)
				{
					if (KickStarter.runtimeInventory.selectedItem != null)
					{
						KickStarter.runtimeInventory.SetNull ();
					}
				}

				PlayerMenus.ResetInventoryBoxes ();
			}
		}
		

		private void ClickOutput (AC.Menu _menu, MouseState _mouseState)
		{
			if (items.Count > 0)
			{
				if (_mouseState == MouseState.SingleClick)
				{
					if (KickStarter.runtimeInventory.selectedItem == null)
					{
						// Pick up created item
						if (activeRecipe.onCreateRecipe == OnCreateRecipe.SelectItem)
						{
							KickStarter.runtimeInventory.PerformCrafting (activeRecipe, true);
						}
						else if (activeRecipe.onCreateRecipe == OnCreateRecipe.RunActionList)
						{
							KickStarter.runtimeInventory.PerformCrafting (activeRecipe, false);
							if (activeRecipe.invActionList != null)
							{
								AdvGame.RunActionListAsset (activeRecipe.invActionList);
							}
						}
						else
						{
							KickStarter.runtimeInventory.PerformCrafting (activeRecipe, false);
						}
					}
				}
				PlayerMenus.ResetInventoryBoxes ();
			}
		}


		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void RecalculateSize (MenuSource source)
		{
			PopulateList (source);

			isFilled = new bool [numSlots];

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
		
		
		private void PopulateList (MenuSource source)
		{
			if (Application.isPlaying)
			{
				if (craftingType == CraftingElementType.Ingredients)
				{
					items = new List<InvItem>();
					foreach (InvItem _item in KickStarter.runtimeInventory.craftingItems)
					{
						items.Add (_item);
					}
				}
				else if (craftingType == CraftingElementType.Output)
				{
					SetOutput (source, true);
					return;
				}
			}
			else
			{
				items = new List<InvItem>();
				return;
			}
		}


		/**
		 * <summary>Creates and displays the correct InvItem, based on the current Recipe, provided craftingType = CraftingElementType.Output.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 * <param name = "autoCreate">If True, then the Recipe must also have autoCreate = True for the InvItem to appear</param>
		 */
		public void SetOutput (MenuSource source, bool autoCreate)
		{
			if (craftingType != CraftingElementType.Output)
			{
				return;
			}

			items = new List<InvItem>();
			activeRecipe = KickStarter.runtimeInventory.CalculateRecipe (autoCreate);
			if (activeRecipe != null)
			{
				AdvGame.RunActionListAsset (activeRecipe.actionListOnCreate);//

				foreach (InvItem assetItem in AdvGame.GetReferences ().inventoryManager.items)
				{
					if (assetItem.id == activeRecipe.resultID)
					{
						InvItem newItem = new InvItem (assetItem);
						newItem.count = 1;
						items.Add (newItem);
					}
				}
			}

			if (!autoCreate)
			{
				base.RecalculateSize (source);
			}
		}

		
		private Texture2D GetTexture ( int i)
		{
			Texture2D tex = null;
			
			if (Application.isPlaying)
			{
				tex = GetItem (i).tex;
			}
			else if (items [i].tex != null)
			{
				tex = items [i].tex;
			}
			
			return tex;
		}

		
		private void DrawTexture (Rect rect, int i)
		{
			Texture2D tex = GetTexture (i);

			if (tex != null)
			{
				GUI.DrawTexture (rect, tex, ScaleMode.StretchToFill, true, 0f);
			}
		}
		

		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "i">The index number of the slot</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int i, int languageNumber)
		{
			if (languageNumber > 0)
			{
				return KickStarter.runtimeLanguages.GetTranslation (GetItem (i).label, GetItem (i).lineID, languageNumber);
			}
			if (GetItem (i).altLabel != "")
			{
				return GetItem (i).altLabel;
			}
			
			return GetItem (i).label;
		}
		

		/**
		 * <summary>Gets the InvItem displayed in a specific slot.</summary>
		 * <param name = "i">The index number of the slot</param>
		 * <returns>The InvItem displayed in the slot</returns>
		 */
		public InvItem GetItem (int i)
		{
			if (craftingType == CraftingElementType.Output)
			{
				if (items.Count > i)
				{
					return items [i];
				}
			}
			else if (craftingType == CraftingElementType.Ingredients)
			{
				foreach (InvItem _item in items)
				{
					if (_item.recipeSlot == i)
					{
						return _item;
					}
				}
			}
			return null;
		}
		
		
		private string GetCount (int i)
		{
			InvItem item = GetItem (i);
			if (item != null)
			{
				if (GetItem (i).count < 2)
				{
					return "";
				}
				return GetItem (i).count.ToString ();
			}
			return "";
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return;
			}

			base.ProcessClick (_menu, _slot, _mouseState);

			if (craftingType == CraftingElementType.Ingredients)
			{
				HandleDefaultClick (_mouseState, _slot);
			}
			else if (craftingType == CraftingElementType.Output)
			{
				ClickOutput (_menu, _mouseState);
			}

			_menu.Recalculate ();
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
			else
			{
				AutoSize (GUIContent.none);
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
					if (craftingType == CraftingElementType.Ingredients)
					{
						return invItem.recipeSlot;
					}
					return items.IndexOf (invItem) - offset;
				}
			}
			return 0;
		}
		
	}
	
}