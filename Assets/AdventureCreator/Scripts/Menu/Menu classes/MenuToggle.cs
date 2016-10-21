/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"MenuToggle.cs"
 * 
 *	This MenuElement toggles between On and Off when clicked on.
 *	It can be used for changing boolean options.
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
	 * A MenuElement that provides an "on/off" toggle button.
	 * It can be used to change the value of a Boolean global variable, or the display of subtitles in Options.
	 */
	public class MenuToggle : MenuElement
	{

		/** The Unity UI Toggle this is linked to (Unity UI Menus only) */
		public Toggle uiToggle;
		/** What the value of the toggle represents (Subtitles, Variable, CustomScript) */
		public AC_ToggleType toggleType;
		/** An ActionListAsset that will run when the element is clicked on */
		public ActionListAsset actionListOnClick = null;
		/** The text that's displayed on-screen */
		public string label;
		/** If True, then the toggle will be in its "on" state by default */
		public bool isOn;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The text alignment */
		public TextAnchor anchor;
		/** The ID number of the Boolean global variable to link to, if toggleType = AC_ToggleType.Variable */
		public int varID;
		/** If True, then the state ("On"/"Off") will be added to the display label */
		public bool appendState = true;
		/** The background texture when in the "on" state (OnGUI Menus only) */
		public Texture2D onTexture = null;
		/** The background texture when in the "off" state (OnGUI Menus only) */
		public Texture2D offTexture = null;
		/** The method which this element are hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

		/** The text suffix when the toggle is 'on' */
		public string onText = "On";
		/** The translation ID of the 'off' text, as set within SpeechManager */
		public int onTextLineID = -1;
		/** The text suffix when the toggle is 'off' */
		public string offText = "Off";
		/** The translation ID of the 'off' text, as set within SpeechManager */
		public int offTextLineID = -1;

		private Text uiText;
		private string fullText;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiToggle = null;
			uiText = null;
			label = "Toggle";
			isOn = false;
			isVisible = true;
			isClickable = true;
			toggleType = AC_ToggleType.CustomScript;
			numSlots = 1;
			varID = 0;
			SetSize (new Vector2 (15f, 5f));
			anchor = TextAnchor.MiddleLeft;
			appendState = true;
			onTexture = null;
			offTexture = null;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			actionListOnClick = null;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
			onText = "On";
			offText = "Off";
			onTextLineID = -1;
			offTextLineID = -1;

			base.Declare ();
		}


		/**
		 * <summary>Creates and returns a new MenuToggle that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <returns>A new MenuToggle with the same values as itself</returns>
		 */
		public override MenuElement DuplicateSelf (bool fromEditor)
		{
			MenuToggle newElement = CreateInstance <MenuToggle>();
			newElement.Declare ();
			newElement.CopyToggle (this);
			return newElement;
		}
		
		
		private void CopyToggle (MenuToggle _element)
		{
			uiToggle = _element.uiToggle;
			uiText = null;
			label = _element.label;
			isOn = _element.isOn;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			anchor = _element.anchor;
			toggleType = _element.toggleType;
			varID = _element.varID;
			appendState = _element.appendState;
			onTexture = _element.onTexture;
			offTexture = _element.offTexture;
			actionListOnClick = _element.actionListOnClick;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;
			onText = _element.onText;
			offText = _element.offText;
			onTextLineID = _element.onTextLineID;
			offTextLineID = _element.offTextLineID;

			base.Copy (_element);
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu)
		{
			uiToggle = LinkUIElement <Toggle>();

			if (uiToggle)
			{
				if (uiToggle.GetComponentInChildren <Text>())
				{
					uiText = uiToggle.GetComponentInChildren <Text>();
				}
				uiToggle.onValueChanged.AddListener ((isOn) => {
					ProcessClickUI (_menu, 0, KickStarter.playerInput.GetMouseState ());
				});
			}
		}


		/**
		 * <summary>Gets the linked Unity UI GameObject associated with this element.</summary>
		 * <returns>The Unity UI GameObject associated with the element</returns>
		 */
		public override GameObject GetObjectToSelect ()
		{
			if (uiToggle)
			{
				return uiToggle.gameObject;
			}
			return null;
		}
		

		/**
		 * <summary>Gets the boundary of the element.</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <returns>The boundary Rect of the element</returns>
		 */
		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiToggle)
			{
				return uiToggle.GetComponent <RectTransform>();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			if (uiToggle)
			{
				uiToggle.interactable = state;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");

			if (source != MenuSource.AdventureCreator)
			{
				uiToggle = LinkedUiGUI <Toggle> (uiToggle, "Linked Toggle:", source);
				uiSelectableHideStyle = (UISelectableHideStyle) EditorGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle);
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
			}

			label = EditorGUILayout.TextField ("Label text:", label);
			appendState = EditorGUILayout.Toggle ("Append state to label?", appendState);
			if (appendState)
			{
				onText = EditorGUILayout.TextField ("'On' state text:", onText);
				offText = EditorGUILayout.TextField ("'Off' state text:", offText);
			}

			if (source == MenuSource.AdventureCreator)
			{
				anchor = (TextAnchor) EditorGUILayout.EnumPopup ("Text alignment:", anchor);
				textEffects = (TextEffects) EditorGUILayout.EnumPopup ("Text effect:", textEffects);
				if (textEffects != TextEffects.None)
				{
					outlineSize = EditorGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f);
				}
			
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("'On' texture:", GUILayout.Width (145f));
				onTexture = (Texture2D) EditorGUILayout.ObjectField (onTexture, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("'Off' texture:", GUILayout.Width (145f));
				offTexture = (Texture2D) EditorGUILayout.ObjectField (offTexture, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
			}

			toggleType = (AC_ToggleType) EditorGUILayout.EnumPopup ("Toggle type:", toggleType);
			if (toggleType == AC_ToggleType.CustomScript)
			{
				isOn = EditorGUILayout.Toggle ("On by default?", isOn);
				ShowClipHelp ();
			}
			else if (toggleType == AC_ToggleType.Variable)
			{
				varID = AdvGame.GlobalVariableGUI ("Global boolean var:", varID);
				if (varID >= 0 && AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
				{
					GVar _var = AdvGame.GetReferences ().variablesManager.GetVariable (varID);
					if (_var != null && _var.type != VariableType.Boolean)
					{
						EditorGUILayout.HelpBox ("The chosen Variable must be a Boolean.", MessageType.Warning);
					}
				}
			}
			if (toggleType != AC_ToggleType.Subtitles)
			{
				actionListOnClick = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList on click:", actionListOnClick, typeof (ActionListAsset), false);
			}
			alternativeInputButton = EditorGUILayout.TextField ("Alternative input button:", alternativeInputButton);
			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}
		
		#endif
		

		/**
		 * <summary>Performs all calculations necessary to display the element.</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language to display text in</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			CalculateValue ();

			fullText = TranslateLabel (label, languageNumber);
			if (appendState)
			{
				if (languageNumber == 0)
				{
					if (isOn)
					{
						fullText += " : " + onText;
					}
					else
					{
						fullText += " : " + offText;
					}
				}
				else
				{
					if (isOn)
					{
						fullText += " : " + KickStarter.runtimeLanguages.GetTranslation (onText, onTextLineID, languageNumber);
					}
					else
					{
						fullText += " : " + KickStarter.runtimeLanguages.GetTranslation (offText, offTextLineID, languageNumber);
					}
				}
			}

			if (uiToggle)
			{
				if (uiText)
				{
					uiText.text = fullText;
				}
				uiToggle.isOn = isOn;
				UpdateUISelectable (uiToggle, uiSelectableHideStyle);
			}
		}
		

		/**
		 * <summary>Draws the element using OnGUI</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);
			
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}
			
			Rect rect = ZoomRect (relativeRect, zoom);
			if (isOn && onTexture != null)
			{
				GUI.DrawTexture (rect, onTexture, ScaleMode.StretchToFill, true, 0f);
			}
			else if (!isOn && offTexture != null)
			{
				GUI.DrawTexture (rect, offTexture, ScaleMode.StretchToFill, true, 0f);
			}
			
			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (rect, fullText, _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (rect, fullText, _style);
			}
		}
		

		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			if (appendState)
			{
				if (isOn)
				{
					return TranslateLabel (label, languageNumber) + " : " + KickStarter.runtimeLanguages.GetTranslation (onText, onTextLineID, languageNumber);
				}
				
				return TranslateLabel (label, languageNumber) + " : " + KickStarter.runtimeLanguages.GetTranslation (offText, offTextLineID, languageNumber);
			}
			return TranslateLabel (label, languageNumber);
		}
		

		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (!_menu.IsClickable ())
			{
				return;
			}

			base.ProcessClick (_menu, _slot, _mouseState);
			if (uiToggle != null)
			{
				isOn = uiToggle.isOn;
			}
			else
			{
				if (isOn)
				{
					isOn = false;
				}
				else
				{
					isOn = true;
				}
			}

			if (toggleType == AC_ToggleType.Subtitles)
			{
				Options.SetSubtitles (isOn);
			}
			else if (toggleType == AC_ToggleType.Variable)
			{
				if (varID >= 0)
				{
					GVar var = GlobalVariables.GetVariable (varID);
					if (var.type == VariableType.Boolean)
					{
						if (isOn)
						{
							var.val = 1;
						}
						else
						{
							var.val = 0;
						}
						var.Upload ();
					}
				}
			}
			
			if (toggleType == AC_ToggleType.CustomScript)
			{
				MenuSystem.OnElementClick (_menu, this, _slot, (int) _mouseState);
			}

			if (actionListOnClick)
			{
				AdvGame.RunActionListAsset (actionListOnClick);
			}
		}


		private void CalculateValue ()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (toggleType == AC_ToggleType.Subtitles)
			{	
				if (Options.optionsData != null)
				{
					isOn = Options.optionsData.showSubtitles;
				}
			}
			else if (toggleType == AC_ToggleType.Variable)
			{
				if (varID >= 0)
				{
					GVar var = GlobalVariables.GetVariable (varID);
					if (var != null && var.type == VariableType.Boolean)
					{
						if (var.val == 1)
						{
							isOn = true;
						}
						else
						{
							isOn = false;
						}
					}
					else
					{
						ACDebug.LogWarning ("Cannot link MenuToggle " + title + " to Variable " + varID + " as it is not a Boolean.");
					}
				}
			}
		}

		
		protected override void AutoSize ()
		{
			int languageNumber = Options.GetLanguage ();
			if (appendState)
			{
				AutoSize (new GUIContent (TranslateLabel (label, languageNumber) + " : Off"));
			}
			else
			{
				AutoSize (new GUIContent (TranslateLabel (label, languageNumber)));
			}
		}
		
	}
	
}