/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"MenuSlider.cs"
 * 
 *	This MenuElement creates a slider for eg. volume control.
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
	 * A MenuElement that provides a slider, whose value can represent either a Float global variable or the volume of an Options sound type.
	 */
	public class MenuSlider : MenuElement
	{

		/** The Unity UI Slider this is linked to (Unity UI Menus only) */
		public Slider uiSlider;
		/** The slider's default value */
		public float amount;
		/** The slider's minimum value */
		public float minValue = 0f;
		/** The slider's maximum value */
		public float maxValue = 1f;
		/** The text that's displayed on-screen */
		public string label;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The text alignement */
		public TextAnchor anchor;
		/** The fill-bar texture, or moveable block texture (OnGUI Menus only) */
		public Texture2D sliderTexture;
		/** The display type of the slider (FillBar, MoveableBlock) (OnGUI Menus Only) */
		public SliderDisplayType sliderDisplayType = SliderDisplayType.FillBar;
		/** What the slider's value represents (Speech, Music, SFX, CustomScript, FloatVariable) */
		public AC_SliderType sliderType;
		/** The dimensions of the block, if sliderDisplayType = SliderDisplayType.MoveableBlock */
		public Vector2 blockSize = new Vector2 (0.05f, 1f);
		/** If True, then the slider will be drawn across the whole width of the element.  Otherwise, it will be drawn across only half. */
		public bool useFullWidth = false;
		/** The ID number of the Float global variable to link the slider's value to, if sliderType = AC_SliderType.FloatVariable) */
		public int varID;
		/** If >0, The number of descrete values the slider can have (OnGUI Menus only) */
		public int numberOfSteps = 0;
		/** An ActionListAsset that will run when the value of the slider is changed */
		public ActionListAsset actionListOnChange = null;
		/** The method which this element are hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

		private float visualAmount;
		private Rect sliderRect;
		private string fullText;


		/**
		 * Initialises the MenuElement when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiSlider = null;

			label = "Slider";
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			amount = 1f;
			minValue = 0f;
			maxValue = 1f;
			anchor = TextAnchor.MiddleLeft;
			sliderType = AC_SliderType.CustomScript;
			sliderDisplayType = SliderDisplayType.FillBar;
			blockSize = new Vector2 (0.05f, 1f);
			useFullWidth = false;
			varID = 0;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			numberOfSteps = 0;
			actionListOnChange = null;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

			base.Declare ();
		}


		/**
		 * <summary>Creates and returns a new MenuSlider that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <returns>A new MenuSlider with the same values as itself</returns>
		 */
		public override MenuElement DuplicateSelf (bool fromEditor)
		{
			MenuSlider newElement = CreateInstance <MenuSlider>();
			newElement.Declare ();
			newElement.CopySlider (this);
			return newElement;
		}
		
		
		private void CopySlider (MenuSlider _element)
		{
			uiSlider = _element.uiSlider;
			label = _element.label;
			isClickable = _element.isClickable;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			amount = _element.amount;
			minValue = _element.minValue;
			maxValue = _element.maxValue;
			anchor = _element.anchor;
			sliderTexture = _element.sliderTexture;
			sliderType = _element.sliderType;
			sliderDisplayType = _element.sliderDisplayType;
			blockSize = _element.blockSize;
			useFullWidth = _element.useFullWidth;
			varID = _element.varID;
			numberOfSteps = _element.numberOfSteps;
			actionListOnChange = _element.actionListOnChange;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;

			base.Copy (_element);
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu)
		{
			uiSlider = LinkUIElement <Slider>();
			if (uiSlider)
			{
				uiSlider.interactable = isClickable;
				if (isClickable)
				{
					uiSlider.onValueChanged.AddListener ((amount) => {
						ProcessClickUI (_menu, 0, KickStarter.playerInput.GetMouseState ());
					});
				}
			}
		}


		/**
		 * <summary>Gets the linked Unity UI GameObject associated with this element.</summary>
		 * <returns>The Unity UI GameObject associated with the element</returns>
		 */
		public override GameObject GetObjectToSelect ()
		{
			if (uiSlider)
			{
				return uiSlider.gameObject;
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
			if (uiSlider)
			{
				return uiSlider.GetComponent <RectTransform>();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			if (uiSlider)
			{
				uiSlider.interactable = state;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");

			if (source == MenuSource.AdventureCreator)
			{
				label = EditorGUILayout.TextField ("Label text:", label);
				anchor = (TextAnchor) EditorGUILayout.EnumPopup ("Text alignment:", anchor);
				textEffects = (TextEffects) EditorGUILayout.EnumPopup ("Text effect:", textEffects);
				if (textEffects != TextEffects.None)
				{
					outlineSize = EditorGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f);
				}
				useFullWidth = EditorGUILayout.Toggle ("Use full width?", useFullWidth);
				sliderDisplayType = (SliderDisplayType) EditorGUILayout.EnumPopup ("Display type:", sliderDisplayType);
				
				EditorGUILayout.BeginHorizontal ();
				if (sliderDisplayType == SliderDisplayType.FillBar)
				{
					EditorGUILayout.LabelField ("Fill-bar texture:", GUILayout.Width (145f));
				}
				else if (sliderDisplayType == SliderDisplayType.MoveableBlock)
				{
					EditorGUILayout.LabelField ("Movable block texture:", GUILayout.Width (145f));
				}
				sliderTexture = (Texture2D) EditorGUILayout.ObjectField (sliderTexture, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
				
				if (sliderDisplayType == SliderDisplayType.MoveableBlock)
				{
					blockSize = EditorGUILayout.Vector2Field ("Block size:", blockSize);
				}
			}
			else
			{
				uiSlider = LinkedUiGUI <Slider> (uiSlider, "Linked Slider:", source);
				uiSelectableHideStyle = (UISelectableHideStyle) EditorGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle);
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
			}
			
			sliderType = (AC_SliderType) EditorGUILayout.EnumPopup ("Slider affects:", sliderType);
			if (sliderType == AC_SliderType.CustomScript)
			{
				ShowClipHelp ();
				amount = EditorGUILayout.Slider ("Default value:", amount, minValue, maxValue);
			}
			else if (sliderType == AC_SliderType.FloatVariable)
			{
				varID = AdvGame.GlobalVariableGUI ("Global float var:", varID);
				if (varID >= 0 && AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
				{
					GVar _var = AdvGame.GetReferences ().variablesManager.GetVariable (varID);
					if (_var != null && _var.type != VariableType.Float)
					{
						EditorGUILayout.HelpBox ("The chosen Variable must be a Float.", MessageType.Warning);
					}
				}
			}
			if (sliderType == AC_SliderType.CustomScript || sliderType == AC_SliderType.FloatVariable)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Min. value:", GUILayout.Width (80f));
				minValue = EditorGUILayout.FloatField (minValue);
				EditorGUILayout.LabelField ("Max. value:", GUILayout.Width (80f));
				maxValue = EditorGUILayout.FloatField (maxValue);
				EditorGUILayout.EndHorizontal ();
				maxValue = Mathf.Max (minValue, maxValue);
				actionListOnChange = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList on change:", actionListOnChange, typeof (ActionListAsset), false);
			}
			else
			{
				minValue = 0f;
				maxValue = 1f;
			}

			if (source == MenuSource.AdventureCreator)
			{
				numberOfSteps = EditorGUILayout.IntField ("Number of steps:", numberOfSteps);
			}
			isClickable = EditorGUILayout.Toggle ("User can change value?", isClickable);

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

			if (uiSlider)
			{
				uiSlider.value = visualAmount;
				UpdateUISelectable (uiSlider, uiSelectableHideStyle);
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
			GUI.Label (ZoomRect (relativeRect, zoom), "", _style);
			
			if (sliderTexture)
			{
				DrawSlider (zoom);
			}
			
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}
			
			_style.normal.background = null;
			
			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), fullText, _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (relativeRect, zoom), fullText, _style);
			}
		}
		
		
		private void DrawSlider (float zoom)
		{
			sliderRect = relativeRect;
			if (sliderDisplayType == SliderDisplayType.FillBar)
			{
				if (useFullWidth)
				{
					sliderRect.x = relativeRect.x;
					sliderRect.width = slotSize.x * visualAmount;
				}
				else
				{
					sliderRect.x = relativeRect.x + (relativeRect.width / 2);
					sliderRect.width = slotSize.x * visualAmount * 0.5f;
				}
				
				if (sizeType != AC_SizeType.AbsolutePixels)
				{
					sliderRect.width *= AdvGame.GetMainGameViewSize ().x / 100f;
				}
			}
			else if (sliderDisplayType == SliderDisplayType.MoveableBlock)
			{
				sliderRect.width *= blockSize.x;
				sliderRect.height *= blockSize.y;
				sliderRect.y += (relativeRect.height - sliderRect.height) / 2f;
				
				if (useFullWidth)
				{
					sliderRect.x = slotSize.x * visualAmount + relativeRect.x - (sliderRect.width / 2f);
				}
				else
				{
					sliderRect.x = slotSize.x * visualAmount * 0.5f + relativeRect.x + (relativeRect.width / 2) - (sliderRect.width / 2f);
				}
				
				if (sizeType != AC_SizeType.AbsolutePixels)
				{
					sliderRect.x *= AdvGame.GetMainGameViewSize ().x / 100f;
				}
			}
			
			GUI.DrawTexture (ZoomRect (sliderRect, zoom), sliderTexture, ScaleMode.StretchToFill, true, 0f);
		}
		

		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			return TranslateLabel (label, languageNumber);
		}
		
		
		private void Change ()
		{
			visualAmount += 0.02f; 
			
			if (visualAmount > 1f)
			{
				visualAmount = 0f;
			}
			
			UpdateValue ();
		}
		
		
		private void Change (float mouseX)
		{
			if (useFullWidth)
			{
				mouseX = mouseX - relativeRect.x;
				visualAmount = mouseX / relativeRect.width;
			}
			else
			{
				mouseX = mouseX - relativeRect.x - (relativeRect.width / 2f);
				visualAmount = mouseX / (relativeRect.width / 2f);
			}
			
			UpdateValue ();
		}
		
		
		private void UpdateValue ()
		{
			if (uiSlider == null)
			{
				visualAmount = Mathf.Clamp (visualAmount, 0f, 1f);

				// Limit by steps
				if (numberOfSteps > 0)
				{
					visualAmount = Mathf.Round (visualAmount * numberOfSteps) / numberOfSteps;
				}

				amount = (visualAmount * (maxValue - minValue)) + minValue;
			}
			else
			{
				amount = visualAmount;
			}
				
			if (sliderType == AC_SliderType.Speech || sliderType == AC_SliderType.SFX || sliderType == AC_SliderType.Music)
			{
				if (Options.optionsData != null)
				{
					if (sliderType == AC_SliderType.Speech)
					{
						KickStarter.options.SetVolume (SoundType.Speech, amount);
					}
					else if (sliderType == AC_SliderType.Music)
					{
						KickStarter.options.SetVolume (SoundType.Music, amount);
					}
					else if (sliderType == AC_SliderType.SFX)
					{
						KickStarter.options.SetVolume (SoundType.SFX, amount);
					}

					#if UNITY_5
					if (sliderType == AC_SliderType.Speech)
					{
						AdvGame.SetMixerVolume (KickStarter.settingsManager.speechMixerGroup, KickStarter.settingsManager.speechAttentuationParameter, amount);
					}
					else if (sliderType == AC_SliderType.Music)
					{
						AdvGame.SetMixerVolume (KickStarter.settingsManager.musicMixerGroup, KickStarter.settingsManager.musicAttentuationParameter, amount);
					}
					else if (sliderType == AC_SliderType.SFX)
					{
						AdvGame.SetMixerVolume (KickStarter.settingsManager.sfxMixerGroup, KickStarter.settingsManager.sfxAttentuationParameter, amount);
					}
					#endif
				}
				else
				{
					ACDebug.LogWarning ("Could not find Options data!");
				}
			}
			else if (sliderType == AC_SliderType.FloatVariable)
			{
				if (varID >= 0)
				{
					GVar var = GlobalVariables.GetVariable (varID);
					if (var.type == VariableType.Float)
					{
						var.SetFloatValue (amount);
					}
				}
			}

			if (!KickStarter.actionListAssetManager.IsListRunning (actionListOnChange))
			{
				AdvGame.RunActionListAsset (actionListOnChange);
			}
		}


		private void CalculateValue ()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (sliderType == AC_SliderType.Speech || sliderType == AC_SliderType.SFX || sliderType == AC_SliderType.Music)
			{
				if (Options.optionsData != null)
				{
					if (sliderType == AC_SliderType.Speech)
					{
						amount = Options.optionsData.speechVolume;
					}
					else if (sliderType == AC_SliderType.Music)
					{
						amount = Options.optionsData.musicVolume;
					}
					else if (sliderType == AC_SliderType.SFX)
					{
						amount = Options.optionsData.sfxVolume;
					}
				}
			}
			else if (sliderType == AC_SliderType.FloatVariable)
			{
				if (varID >= 0)
				{
					GVar _variable = GlobalVariables.GetVariable (varID);
					if (_variable != null)
					{
						if (_variable.type != VariableType.Float)
						{
							ACDebug.LogWarning ("Cannot link MenuSlider " + title + " to Variable " + varID + " as it is not a Float.");
						}
						else
						{
							amount = Mathf.Clamp (_variable.floatVal, minValue, maxValue);
							_variable.SetFloatValue (amount);
						}
					}
					else
					{
						ACDebug.LogWarning ("Slider " + this.label + " is referencing Gloval Variable " + varID + ", which does not exist.");
					}
				}
			}

			visualAmount = (amount - minValue) / (maxValue - minValue);
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

			if (uiSlider != null)
			{
				visualAmount = uiSlider.value;
				UpdateValue ();
			}
			else
			{
				if (KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController)
				{
					Change ();
				}
				else
				{
					Change (KickStarter.playerInput.GetMousePosition ().x - _menu.GetRect ().x);
				}
			}

			if (sliderType == AC_SliderType.CustomScript)
			{
				MenuSystem.OnElementClick (_menu, this, _slot, (int) _mouseState);
			}

			KickStarter.eventManager.Call_OnMenuElementClick (_menu, this, _slot, (int) _mouseState);
		}
		

		/**
		 * <summary>Performs what should happen when the element is clicked on continuously.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 */
		public override void ProcessContinuousClick (AC.Menu _menu, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return;
			}

			if (KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController)
			{
				Change ();
			}
			else
			{
				Change (KickStarter.playerInput.GetMousePosition ().x - _menu.GetRect ().x);
			}
			if (sliderType == AC_SliderType.CustomScript)
			{
				MenuSystem.OnElementClick (_menu, this, 0, (int) _mouseState);
			}
		}

		
		protected override void AutoSize ()
		{
			AutoSize (new GUIContent (TranslateLabel (label, Options.GetLanguage ())));
		}
		
	}
	
}