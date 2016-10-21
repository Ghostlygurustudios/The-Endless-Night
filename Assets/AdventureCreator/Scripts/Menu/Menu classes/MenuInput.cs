/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"MenuInput.cs"
 * 
 *	This MenuElement acts like a label, whose text can be changed with keyboard input.
 * 
 */

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that provides an input box that the player can enter text into.
	 */
	public class MenuInput : MenuElement
	{

		/** The Unity UI InputField this is linked to (Unity UI Menus only) */
		public InputField uiInput;
		/** The text that's displayed on-screen */
		public string label = "Element";
		/** The text alignment */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** What kind of characters can be entered in by the player (AlphaNumeric, NumericOnly) */
		public AC_InputType inputType;
		/** The character limit on text that can be entered */
		public int characterLimit = 10;
		/** The name of the MenuButton element that is synced with the 'Return' key when this element is active */
		public string linkedButton = "";
		/** If True, then spaces are recognised */
		public bool allowSpaces = false;
		/** The method which this element are hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

		private bool isSelected = false;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiInput = null;
			label = "Input";
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (10f, 5f));
			inputType = AC_InputType.AlphaNumeric;
			characterLimit = 10;
			linkedButton = "";
			textEffects = TextEffects.None;
			outlineSize = 2f;
			allowSpaces = false;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

			base.Declare ();
		}


		/**
		 * <summary>Creates and returns a new MenuInput that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <returns>A new MenuInput with the same values as itself</returns>
		 */
		public override MenuElement DuplicateSelf (bool fromEditor)
		{
			MenuInput newElement = CreateInstance <MenuInput>();
			newElement.Declare ();
			newElement.CopyInput (this);
			return newElement;
		}
		
		
		private void CopyInput (MenuInput _element)
		{
			uiInput = _element.uiInput;
			label = _element.label;
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			inputType = _element.inputType;
			characterLimit = _element.characterLimit;
			linkedButton = _element.linkedButton;
			allowSpaces = _element.allowSpaces;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;

			base.Copy (_element);
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu)
		{
			uiInput = LinkUIElement <InputField>();
		}
		

		/**
		 * <summary>Gets the boundary of the element</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <returns>The boundary Rect of the element</returns>
		 */
		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiInput)
			{
				return uiInput.GetComponent <RectTransform>();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			if (uiInput)
			{
				uiInput.interactable = state;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");
			if (source == MenuSource.AdventureCreator)
			{
				label = EditorGUILayout.TextField ("Default text:", label);
				inputType = (AC_InputType) EditorGUILayout.EnumPopup ("Input type:", inputType);
				if (inputType == AC_InputType.AlphaNumeric)
				{
					allowSpaces = EditorGUILayout.Toggle ("Allow spaces?", allowSpaces);
				}
				characterLimit = EditorGUILayout.IntField ("Character limit:", characterLimit);
				anchor = (TextAnchor) EditorGUILayout.EnumPopup ("Text alignment:", anchor);
				textEffects = (TextEffects) EditorGUILayout.EnumPopup ("Text effect:", textEffects);
				if (textEffects != TextEffects.None)
				{
					outlineSize = EditorGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f);
				}
				linkedButton = EditorGUILayout.TextField ("'Enter' key's linked Button:", linkedButton);
			}
			else
			{
				uiInput = LinkedUiGUI <InputField> (uiInput, "Linked InputField:", source);
				uiSelectableHideStyle = (UISelectableHideStyle) EditorGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle);
			}
			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}
		
		#endif
		

		/**
		 * <summary>Gets the contents of the text box.</summary>
		 * <returns>The contents of the text box.</returns>
		 */
		public string GetContents ()
		{
			if (uiInput != null)
			{
				if (uiInput.textComponent != null)
				{
					return uiInput.textComponent.text;
				}
				else
				{
					ACDebug.LogWarning (uiInput.gameObject.name + " has no Text component");
				}
			}

			return label;
		}


		/**
		 * <summary>Performs all calculations necessary to display the element.</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language to display text in</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (uiInput != null)
			{
				UpdateUISelectable (uiInput, uiSelectableHideStyle);
			}
		}


		/**
		 * <summary>Draws the element using OnGUI.</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			string fullText = label;
			if (Application.isPlaying && (isSelected || isActive))
			{
				if (Options.GetLanguageName () == "Arabic" || Options.GetLanguageName () == "Hebrew")
				{
					fullText = "|" + fullText;
				}
				else
				{
					fullText += "|";
				}
			}

			_style.wordWrap = true;
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}

			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), fullText, _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (relativeRect, zoom), fullText, _style);
			}
		}


		/**
		 * <summary>Gets the display text of the element.</summary>
		 * <param name = "slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			return TranslateLabel (label, languageNumber);
		}


		/**
		 * Processes input entered by the player, and applies it to the text box (OnGUI-based Menus only).
		 */
		public void CheckForInput (string input, bool shift, string menuName)
		{
			if (uiInput != null)
			{
				return;
			}

			bool rightToLeft = false;
			if (Options.GetLanguageName () == "Arabic" || Options.GetLanguageName () == "Hebrew")
			{
				rightToLeft = true;
			}

			isSelected = true;
			if (input == "Backspace")
			{
				if (label.Length > 1)
				{
					if (rightToLeft)
					{
						label = label.Substring (1, label.Length - 1);
					}
					else
					{
						label = label.Substring (0, label.Length - 1);
					}
				}
				else if (label.Length == 1)
				{
					label = "";
				}
			}
			else if (input == "KeypadEnter" || input == "Return" || input == "Enter")
			{
				if (linkedButton != "" && menuName != "")
				{
					PlayerMenus.SimulateClick (menuName, PlayerMenus.GetElementWithName (menuName, linkedButton), 1);
				}
			}
			else if ((inputType == AC_InputType.AlphaNumeric && (input.Length == 1 || input.Contains ("Alpha"))) ||
			         (inputType == AC_InputType.NumbericOnly && input.Contains ("Alpha")) ||
			         (inputType == AC_InputType.AlphaNumeric && allowSpaces && input == "Space"))
			{
				input = input.Replace ("Alpha", "");
				input = input.Replace ("Space", " ");
				if (shift)
				{
					input = input.ToUpper ();
				}
				else
				{
					input = input.ToLower ();
				}

				if (characterLimit == 1)
				{
					label = input;
				}
				else if (label.Length < characterLimit)
				{
					if (rightToLeft)
					{
						label = input + label;
					}
					else
					{
						label += input;
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
			if (source == MenuSource.AdventureCreator)
			{
				Deselect ();
			}

			base.RecalculateSize (source);
		}


		/**
		 * De-selects the text box (OnGUI-based Menus only).
		 */
		public void Deselect ()
		{
			isSelected = false;
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (!_menu.IsClickable ())
			{
				return;
			}

			base.ProcessClick (_menu, _slot, _mouseState);
			KickStarter.playerMenus.SelectInputBox (this);
		}

		
		protected override void AutoSize ()
		{
			GUIContent content = new GUIContent (TranslateLabel (label, Options.GetLanguage ()));
			AutoSize (content);
		}
		
	}

}