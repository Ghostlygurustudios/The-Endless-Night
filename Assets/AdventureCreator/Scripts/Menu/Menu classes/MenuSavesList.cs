/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"MenuSavesList.cs"
 * 
 *	This MenuElement handles the display of any saved games recorded.
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
	 * This MenuElement lists any save game files found on by SaveSystem.
	 * Clicking on slots can load or save the relevant file, and importing variables from another game is also possible.
	 */
	public class MenuSavesList : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The text alignment */
		public TextAnchor anchor;
		/** How this list behaves (Load, Save, Import) */
		public AC_SaveListType saveListType;
		/** The maximum number of slots that can be displayed at once */
		public int maxSlots = 5;
		/** An ActionListAsset that can run once a game is succesfully loaded/saved/imported */
		public ActionListAsset actionListOnSave;
		/** How save files are displayed (LabelOnly, ScreenshotOnly, LabelAndScreenshot) */
		public SaveDisplayType displayType = SaveDisplayType.LabelOnly;
		/** The default graphic to use if slots display save screenshots */
		public Texture2D blankSlotTexture;

		/** The name of the project to import files from, if saveListType = AC_SaveListType.Import */
		public string importProductName;
		/** The filename syntax of import files, if saveListType = AC_SaveListType.Import */
		public string importSaveFilename;
		/** If True, and saveListType = AC_SaveListType.Import, then a specific Boolean global variable must = True for an import file to be listed */
		public bool checkImportBool;
		/** If checkImportBool = True, the ID number of the Boolean global variable that must = True, for an import file to be listed */
		public int checkImportVar;

		/** If True, then only one save slot will be shown */
		public bool fixedOption;
		/** The index number of the save slot to show, if fixedOption = true */
		public int optionToShow;
		/** If >=0, The ID number of the integer ActionParameter in actionListOnSave to set to the index number of the slot clicked */
		public int parameterID = -1;

		/** The display text when a slot represents a "new save" space */
		public string newSaveText = "New save";
		/** If True, a slot that represents a "new save" space can be displayed if appropriate */
		public bool showNewSaveOption = true;
		/** If True, then the save file will be loaded/saved once its slot is clicked on */
		public bool autoHandle = true;
		/** The method which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;

		private string[] labels = null;
		private bool newSaveSlot = false;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiSlots = null;

			newSaveText = "New save";
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			maxSlots = 5;

			SetSize (new Vector2 (20f, 5f));
			anchor = TextAnchor.MiddleCenter;
			saveListType = AC_SaveListType.Save;

			actionListOnSave = null;
			newSaveSlot = false;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			displayType = SaveDisplayType.LabelOnly;
			blankSlotTexture = null;

			fixedOption = false;
			optionToShow = 1;

			importProductName = "";
			importSaveFilename = "";
			checkImportBool = false;
			checkImportVar = 0;

			showNewSaveOption = true;
			autoHandle = true;

			parameterID = -1;
			uiHideStyle = UIHideStyle.DisableObject;

			base.Declare ();
		}


		/**
		 * <summary>Creates and returns a new MenuSavesList that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <returns>A new MenuSavesList with the same values as itself</returns>
		 */
		public override MenuElement DuplicateSelf (bool fromEditor)
		{
			MenuSavesList newElement = CreateInstance <MenuSavesList>();
			newElement.Declare ();
			newElement.CopySavesList (this);
			return newElement;
		}
		
		
		private void CopySavesList (MenuSavesList _element)
		{
			uiSlots = _element.uiSlots;

			newSaveText = _element.newSaveText;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			anchor = _element.anchor;
			saveListType = _element.saveListType;
			maxSlots = _element.maxSlots;
			actionListOnSave = _element.actionListOnSave;
			displayType = _element.displayType;
			blankSlotTexture = _element.blankSlotTexture;
			fixedOption = _element.fixedOption;
			optionToShow = _element.optionToShow;
			importProductName = _element.importProductName;
			importSaveFilename = _element.importSaveFilename;
			checkImportBool = _element.checkImportBool;
			checkImportVar = _element.checkImportVar;
			parameterID = _element.parameterID;
			showNewSaveOption = _element.showNewSaveOption;
			autoHandle = _element.autoHandle;
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
		 * <param name = "The first Unity UI GameObject associated with the element</returns>
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

			fixedOption = EditorGUILayout.Toggle ("Fixed option number?", fixedOption);
			if (fixedOption)
			{
				numSlots = 1;
				slotSpacing = 0f;
				optionToShow = EditorGUILayout.IntField ("Option to display:", optionToShow);
			}
			else
			{
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

			displayType = (SaveDisplayType) EditorGUILayout.EnumPopup ("Display:", displayType);
			if (displayType != SaveDisplayType.LabelOnly)
			{
				EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Empty slot texture:", GUILayout.Width (145f));
					blankSlotTexture = (Texture2D) EditorGUILayout.ObjectField (blankSlotTexture, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
			}
			saveListType = (AC_SaveListType) EditorGUILayout.EnumPopup ("List type:", saveListType);
			if (saveListType == AC_SaveListType.Save)
			{
				showNewSaveOption = EditorGUILayout.Toggle ("Show 'New save' option?", showNewSaveOption);
				if (showNewSaveOption)
				{
					newSaveText = EditorGUILayout.TextField ("'New save' text:", newSaveText);
				}
				autoHandle = EditorGUILayout.Toggle ("Save when click on?", autoHandle);
				if (autoHandle)
				{
					ActionListGUI ("ActionList after saving:", menu.title, "After_Saving");
				}
				else
				{
					ActionListGUI ("ActionList when click:", menu.title, "When_Click");
				}
			}
			else if (saveListType == AC_SaveListType.Load)
			{
				autoHandle = EditorGUILayout.Toggle ("Load when click on?", autoHandle);
				if (autoHandle)
				{
					ActionListGUI ("ActionList after loading:", menu.title, "After_Loading");
				}
				else
				{
					ActionListGUI ("ActionList when click:", menu.title, "When_Click");
				}
			}
			else if (saveListType == AC_SaveListType.Import)
			{
				autoHandle = true;
				#if UNITY_STANDALONE
				importProductName = EditorGUILayout.TextField ("Import project name:", importProductName);
				importSaveFilename = EditorGUILayout.TextField ("Import save filename:", importSaveFilename);
				ActionListGUI ("ActionList after import:", menu.title, "After_Import");
				checkImportBool = EditorGUILayout.Toggle ("Require Bool to be true?", checkImportBool);
				if (checkImportBool)
				{
					checkImportVar = EditorGUILayout.IntField ("Global Variable ID:", checkImportVar);
				}
				#else
				EditorGUILayout.HelpBox ("This feature is only available for standalone platforms (PC, Mac, Linux)", MessageType.Warning);
				#endif
			}

			if (source != MenuSource.AdventureCreator)
			{
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
				uiHideStyle = (UIHideStyle) EditorGUILayout.EnumPopup ("When invisible:", uiHideStyle);
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				if (fixedOption)
				{
					uiSlots = ResizeUISlots (uiSlots, 1);
				}
				else
				{
					uiSlots = ResizeUISlots (uiSlots, maxSlots);
				}
				
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}
			}
				
			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		private void ActionListGUI (string label, string menuTitle, string suffix)
		{
			actionListOnSave = ActionListAssetMenu.AssetGUI (label, actionListOnSave, "", menuTitle + "_" + title + "_" + suffix);
			
			if (actionListOnSave != null && actionListOnSave.useParameters && actionListOnSave.parameters.Count > 0)
			{
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.BeginHorizontal ();
				parameterID = Action.ChooseParameterGUI ("", actionListOnSave.parameters, parameterID, ParameterType.Integer);
				if (parameterID >= 0)
				{
					EditorGUILayout.LabelField ("(= Slot index)");
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.EndVertical ();
			}
		}
		
		#endif


		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "slot">The index number of the slot</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			if (newSaveSlot && saveListType == AC_SaveListType.Save)
			{
				if (!fixedOption && (slot + offset) == (numSlots-1))
				{
					return TranslateLabel (newSaveText, languageNumber);
				}
			}
			return SaveSystem.GetSaveSlotLabel (slot + offset, optionToShow, fixedOption);
		}


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
			if (displayType != SaveDisplayType.ScreenshotOnly)
			{
				string fullText = "";

				if (newSaveSlot && saveListType == AC_SaveListType.Save)
				{
					if (!fixedOption && (_slot + offset) == (KickStarter.saveSystem.GetNumSaves ()))
					{
						fullText = TranslateLabel (newSaveText, languageNumber);
					}
					else if (fixedOption)
					{
						fullText = TranslateLabel (newSaveText, languageNumber);
					}
					else
					{
						fullText = SaveSystem.GetSaveSlotLabel (_slot + offset, optionToShow, fixedOption);
					}
				}
				else
				{
					if (saveListType == AC_SaveListType.Import)
					{
						fullText = SaveSystem.GetImportSlotLabel (_slot + offset, optionToShow, fixedOption);
					}
					else
					{
						fullText = SaveSystem.GetSaveSlotLabel (_slot + offset, optionToShow, fixedOption);
					}
				}

				if (!Application.isPlaying)
				{
					if (labels == null || labels.Length != numSlots)
					{
						labels = new string [numSlots];
					}
				}

				labels [_slot] = fullText;
			}

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);
					
					if (displayType != SaveDisplayType.LabelOnly)
					{
						Texture2D tex = null;
						if (saveListType == AC_SaveListType.Import)
						{
							tex = SaveSystem.GetImportSlotScreenshot (_slot + offset, optionToShow, fixedOption);
						}
						else
						{
							tex = SaveSystem.GetSaveSlotScreenshot (_slot + offset, optionToShow, fixedOption);
						}
						if (tex == null)
						{
							tex = blankSlotTexture;
						}
						uiSlots[_slot].SetImage (tex);
					}
					if (displayType != SaveDisplayType.ScreenshotOnly)
					{
						uiSlots[_slot].SetText (labels [_slot]);
					}
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

			if (displayType != SaveDisplayType.LabelOnly)
			{
				Texture2D tex = null;
				if (saveListType == AC_SaveListType.Import)
				{
					tex = SaveSystem.GetImportSlotScreenshot (_slot + offset, optionToShow, fixedOption);
				}
				else
				{
					tex = SaveSystem.GetSaveSlotScreenshot (_slot + offset, optionToShow, fixedOption);
				}
				if (tex == null && blankSlotTexture != null)
				{
					tex = blankSlotTexture;
				}

				if (tex != null)
				{
					GUI.DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), tex, ScaleMode.StretchToFill, true, 0f);
				}
			}

			if (displayType != SaveDisplayType.ScreenshotOnly)
			{
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

			bool isSuccess = true;
			if (saveListType == AC_SaveListType.Save && autoHandle)
			{
				if (newSaveSlot && _slot == (numSlots - 1))
				{
					isSuccess = SaveSystem.SaveNewGame ();

					if (KickStarter.settingsManager.orderSavesByUpdateTime)
					{
						offset = 0;
					}
					else
					{
						Shift (AC_ShiftInventory.ShiftRight, 1);
					}
				}
				else
				{
					isSuccess = SaveSystem.SaveGame (_slot + offset, optionToShow, fixedOption);
				}
			}
			else if (saveListType == AC_SaveListType.Load && autoHandle)
			{
				isSuccess = SaveSystem.LoadGame (_slot + offset, optionToShow, fixedOption);
			}
			else if (saveListType == AC_SaveListType.Import)
			{
				isSuccess = SaveSystem.ImportGame (_slot + offset, optionToShow, fixedOption);
			}

			if (isSuccess)
			{
				if (saveListType == AC_SaveListType.Save)
				{
					_menu.TurnOff (true);
				}
				else if (saveListType == AC_SaveListType.Load)
				{
					_menu.TurnOff (false);
				}

				AdvGame.RunActionListAsset (actionListOnSave, parameterID, _slot);
			}
			else if (!autoHandle && saveListType != AC_SaveListType.Import)
			{
				AdvGame.RunActionListAsset (actionListOnSave, parameterID, _slot);
			}
		}


		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void RecalculateSize (MenuSource source)
		{
			newSaveSlot = false;

			if (Application.isPlaying)
			{
				if (saveListType == AC_SaveListType.Import)
				{
					if (checkImportBool)
					{
						KickStarter.saveSystem.GatherImportFiles (importProductName, importSaveFilename, checkImportVar);
					}
					else
					{
						KickStarter.saveSystem.GatherImportFiles (importProductName, importSaveFilename, -1);
					}
				}

				if (fixedOption)
				{
					numSlots = 1;
				}
				else
				{
					if (saveListType == AC_SaveListType.Import)
					{
						numSlots = SaveSystem.GetNumImportSlots ();
					}
					else
					{
						numSlots = SaveSystem.GetNumSlots ();

						if (saveListType == AC_SaveListType.Save && numSlots < KickStarter.settingsManager.maxSaves && showNewSaveOption)
						{
							newSaveSlot = true;
							numSlots ++;
						}
					}

					if (numSlots > maxSlots)
					{
						numSlots = maxSlots;
					}

					offset = Mathf.Min (offset, GetMaxOffset ());
				}
			}

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
		
		
		protected override void AutoSize ()
		{
			if (displayType == SaveDisplayType.ScreenshotOnly)
			{
				if (blankSlotTexture != null)
				{
					AutoSize (new GUIContent (blankSlotTexture));
				}
				else
				{
					AutoSize (GUIContent.none);
				}
			}
			else if (displayType == SaveDisplayType.LabelAndScreenshot)
			{
				if (blankSlotTexture != null)
				{
					AutoSize (new GUIContent (blankSlotTexture));
				}
				else
				{
					AutoSize (new GUIContent (SaveSystem.GetSaveSlotLabel (0, optionToShow, fixedOption)));
				}
			}
			else
			{
				AutoSize (new GUIContent (SaveSystem.GetSaveSlotLabel (0, optionToShow, fixedOption)));
			}
		}


		/**
		 * <summary>Checks if the element's slots can be shifted in a particular direction.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <returns>True if the element's slots can be shifted in the particular direction</returns>
		 */
		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (numSlots == 0 || fixedOption)
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
			if (numSlots == 0 || fixedOption)
			{
				return 0;
			}

			return Mathf.Max (0, GetNumFilledSlots () - maxSlots);
		}


		/**
		 * <summary>Shifts which slots are on display, if the number of slots the element has exceeds the number of slots it can show at once.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <param name = "amount">The amount to shift slots by</param>
		 */
		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (isVisible && numSlots >= maxSlots)
			{
				Shift (shiftType, maxSlots, GetNumFilledSlots (), amount);
			}
		}


		private int GetNumFilledSlots ()
		{
			if (saveListType == AC_SaveListType.Save && !fixedOption && newSaveSlot && showNewSaveOption)
			{
				return KickStarter.saveSystem.GetNumSaves () + 1;
			}
			return KickStarter.saveSystem.GetNumSaves ();
		}

	}

}