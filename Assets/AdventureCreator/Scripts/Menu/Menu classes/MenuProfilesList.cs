/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"MenuProfilesList.cs"
 * 
 *	This MenuElement handles the display of any save profiles recorded.
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
	 * This MenuElement lists any save profiles found on by SaveSystem.
	 */
	public class MenuProfilesList : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The text alignment */
		public TextAnchor anchor;
		/** The maximum number of profiles that can be displayed at once */
		public int maxSlots = 5;
		/** An ActionListAsset that can be run when a profile is clicked on */
		public ActionListAsset actionListOnClick;
		/** If True, then the current active profile will also be listed */
		public bool showActive = true;
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
			numSlots = 1;
			maxSlots = 5;
			showActive = true;

			SetSize (new Vector2 (20f, 5f));
			anchor = TextAnchor.MiddleCenter;

			actionListOnClick = null;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			uiHideStyle = UIHideStyle.DisableObject;

			base.Declare ();
		}
		

		/**
		 * <summary>Creates and returns a new MenuProfilesList that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <returns>A new MenuProfilesList with the same values as itself</returns>
		 */
		public override MenuElement DuplicateSelf (bool fromEditor)
		{
			MenuProfilesList newElement = CreateInstance <MenuProfilesList>();
			newElement.Declare ();
			newElement.CopyProfilesList (this);
			return newElement;
		}
		
		
		private void CopyProfilesList (MenuProfilesList _element)
		{
			uiSlots = _element.uiSlots;
			
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			anchor = _element.anchor;
			maxSlots = _element.maxSlots;
			actionListOnClick = _element.actionListOnClick;
			showActive = _element.showActive;
			uiHideStyle = _element.uiHideStyle;

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
						ProcessClickUI (_menu, j, KickStarter.playerInput.GetMouseState ());
					});
				}
				i++;
			}
		}
		

		/**
		 * <summary>Gets the first linked Unity UI GameObject associated with this element.</summary>
		 * <param name = "The first Unity UI GameObject associated with the element</param>
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
		 * Hides all linked Unity UI GameObjects associated with the element.
		 */
		public override void HideAllUISlots ()
		{
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
		}
		

		/**
		 * <summary>Gets the boundary of a slot</summary>
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

			showActive = EditorGUILayout.Toggle ("Include active?", showActive);
			maxSlots = EditorGUILayout.IntField ("Max no. of slots:", maxSlots);
			if (source == MenuSource.AdventureCreator)
			{
				numSlots = EditorGUILayout.IntSlider ("Test slots:", numSlots, 1, maxSlots);
				slotSpacing = EditorGUILayout.Slider ("Slot spacing:", slotSpacing, 0f, 20f);
				orientation = (ElementOrientation) EditorGUILayout.EnumPopup ("Slot orientation:", orientation);
				if (orientation == ElementOrientation.Grid)
				{
					gridWidth = EditorGUILayout.IntSlider ("Grid size:", gridWidth, 1, 10);
				}
			}
			
			if (source == MenuSource.AdventureCreator)
			{
				anchor = (TextAnchor) EditorGUILayout.EnumPopup ("Text alignment:", anchor);
				textEffects = (TextEffects) EditorGUILayout.EnumPopup ("Text effect:", textEffects);
				if (textEffects != TextEffects.None)
				{
					outlineSize = EditorGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f);
				}
			}
			
			actionListOnClick = ActionListAssetMenu.AssetGUI ("ActionList after selecting:", actionListOnClick, "", menu.title + "_" + title + "_After_Selecting");

			if (source != MenuSource.AdventureCreator)
			{
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
				uiHideStyle = (UIHideStyle) EditorGUILayout.EnumPopup ("When invisible:", uiHideStyle);
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				uiSlots = ResizeUISlots (uiSlots, maxSlots);
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}
			}
			
			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}
		
		#endif


		/**
		 * <summary>Shifts which slots are on display, if the number of slots the element has exceeds the number of slots it can show at once.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <param name = "amount">The amount to shift slots by</param>
		 */
		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (isVisible && numSlots >= maxSlots)
			{
				Shift (shiftType, maxSlots, KickStarter.options.GetNumProfiles (), amount);
			}
		}


		/**
		 * <summary>Checks if the element's slots can be shifted in a particular direction.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <returns>True if the element's slots can be shifted in the particular direction</returns>
		 */
		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (numSlots == 0)
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
				if (offset >= GetMaxOffset ())
				{
					return false;
				}
			}
			return true;
		}


		private int GetMaxOffset ()
		{
			if (!showActive)
			{
				return Mathf.Max (0, KickStarter.options.GetNumProfiles () - 1 - maxSlots);
			}
			return Mathf.Max (0, KickStarter.options.GetNumProfiles () - maxSlots);
		}
		

		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "slot">The index number of the slot</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			if (Application.isPlaying)
			{
				return KickStarter.options.GetProfileName (slot + offset, showActive);
			}

			return ("Profile " + slot.ToString ());
		}
		

		/**
		 * <summary>Performs all calculations necessary to display the element.</summary>
		 * <param name = "_slot">The index number of the slot to display</param>
		 * <param name = "languageNumber">The index number of the language to display text in</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			string fullText = GetLabel (_slot, languageNumber);

			if (!Application.isPlaying)
			{
				if (labels == null || labels.Length != numSlots)
				{
					labels = new string [numSlots];
				}
			}

			labels [_slot] = fullText;

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);
					uiSlots[_slot].SetText (labels [_slot]);
				}
			}
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
			
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}
			
			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style);
			}
		}
		

		/**
		 * <summary>Performs what should happen when the element is clicked on.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_slot">The index number of ths slot that was clicked</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return;
			}
			
			base.ProcessClick (_menu, _slot, _mouseState);

			bool isSuccess = KickStarter.options.SwitchProfileIfExists (_slot + offset, showActive);
			if (isSuccess)
			{
				AdvGame.RunActionListAsset (actionListOnClick);
			}
		}


		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void RecalculateSize (MenuSource source)
		{
			if (Application.isPlaying)
			{
				numSlots = KickStarter.options.GetNumProfiles ();

				if (!showActive)
				{
					numSlots --;
				}

				if (numSlots > maxSlots)
				{
					numSlots = maxSlots;
				}

				offset = Mathf.Min (offset, GetMaxOffset ());
			}
			
			labels = new string [numSlots];
			
			if (!isVisible)
			{
				LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
			}
			
			base.RecalculateSize (source);
		}
		
		
		protected override void AutoSize ()
		{
			AutoSize (new GUIContent (GetLabel (0, 0)));
		}
		
	}
	
}