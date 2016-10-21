/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"Menu.cs"
 * 
 *	This script is a container of MenuElement subclasses, which together make up a menu.
 *	When menu elements are added, this script updates the size, positioning etc automatically.
 *	The handling of menu visibility, element clicking, etc is all handled in MenuSystem,
 *	rather than the Menu class itself.
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
	 * A Menu is an in-game GUI.
	 * It is made by grouping together MenuElement subclasses, and displaying them in a particular way.
	 * Menus can either be created using OnGUI (aka "Adventure Creator") calls, or by referencing Canvas objects and Unity UI components.
	 */
	[System.Serializable]
	public class Menu : ScriptableObject
	{

		/** The source of the Menu's display information (AdventureCreator, UnityUiPrefab, UnityUiInScene) */ 
		public MenuSource menuSource = MenuSource.AdventureCreator;
		/** If a Menu links to Unity UI, the linked Canvas gameobject */
		public Canvas canvas;
		/** The ConstantID number of the canvas */
		public int canvasID = 0;
		/** A RectTransform that describes the Menu's screen space */
		public RectTransform rectTransform;
		/** The ConstantID number of the rectTransform */
		public int rectTransformID = 0;
		/** The transition method for Unity UI-based menus (None, CanvasGroupFade, CustomAnimation) */
		public UITransition uiTransitionType = UITransition.None;
		/** The position method for Unity UI-based menus (AbovePlayer, AboveSpeakingCharacter, AppearAtCursorThenFreeze, FollowCursor, Manual, OnHotspot) */
		public UIPositionType uiPositionType = UIPositionType.Manual;

		/** If True, the Menu's propertied can be edited in MenuManager */
		public bool isEditing = false;
		/** If True, the Menu is locked off, and won't ever be displayed */
		public bool isLocked = false;
		/** A unique identifier */
		public int id;
		/** A name for the Menu, used in PlauerMenus to identify it */
		public string title;
		/** An OnGUI Menu's total size, if sizeType = AC_SizeType.Manual */
		public Vector2 manualSize = Vector2.zero;
		/** How an OnGUI Menu is positioned (Centred, Aligned, Manual, FollowCursor, AppearAtCursorAndFreeze, OnHotspot, AboveSpeakingCharacter, AbovePlayer) */
		public AC_PositionType positionType = AC_PositionType.Centred;
		/** An OnGUI Menu's centre point, if positionType = AC_PositionType.Manual */
		public Vector2 manualPosition = Vector2.zero;
		/** An OnGUI Menu's alignment type, if positionType = AC_PositionType.Aligned */
		public TextAnchor alignment = TextAnchor.MiddleCenter;
		/** The Input axis that toggle the Menu on and off, it appearType = AppearType.OnInputKey */
		public string toggleKey = "";
		/** If True, then mouse clicks will be ineffective */
		public bool ignoreMouseClicks = false;
		/** If True, then the game will be paused whenever the Menu is enabled */
		public bool pauseWhenEnabled = false;
		/** If True, then the Menu will be clickable during gameplay-blocking cutscenes */
		public bool canClickInCutscene = false;
		/** If True, and appearType = AppearType.Manual, then the Menu will be enabled when the game begins */
		public bool enabledOnStart = false;
		/** The ActionListAsset to run whenever the Menu is enabled */
		public ActionListAsset actionListOnTurnOn = null;
		/** The ActionListAsset to run whenever the Menu is disabled */
		public ActionListAsset actionListOnTurnOff = null;

		public bool updateWhenFadeOut = true;


		/** If True, the Menu will be positioned such that it is always completely within the screen boundary */
		public bool fitWithinScreen = true;
		/** The texture to display in the background */
		public Texture2D backgroundTexture;

		/** A List of MenuElement subclasses that are currently visible */
		public List<MenuElement> visibleElements = new List<MenuElement>();
		/** The progress made along an in/out transition (0 = off, 1 = on) */
		public float transitionProgress = 0f;
		/** The 'rule' that dictates when a Menu is displayed (Manual, MouseOver, DuringConversation, OnInputKey, OnInteraction, OnHotspot, WhenSpeechPlays, DuringGameplay, OnContainer) */
		public AppearType appearType;
		/** What kind of speaker has to be speaking for this Menu to enable, if appearType = AppearType.WhenSpeechPlays (All, CharactersOnly, NarrationOnly, SpecificCharactersOnly) */
		public SpeechMenuType speechMenuType = SpeechMenuType.All;
		/** What kind of speech has to play for this Menu to enable, if appearType = AppearType.WhenSpeechPlays (All, BlockingOnly, BackgroundOnly) */
		public SpeechMenuLimit speechMenuLimit = SpeechMenuLimit.All;
		/** A list of character names that this Menu will show for, if appearType = AppearType.WhenSpeechPlays and speechMenuType = SpeechMenuType.SpecificCharactersOnly */
		public string limitToCharacters = "";

		/** Which OnGUI MenuElement is currently active, when it is keyboard-controlled */
		public MenuElement selected_element;
		/** Which slot within an OnGUI MenuElement is currently active, when it is keyboard-controlled */
		public int selected_slot = 0;
		/** The name of the Unity UI MenuElement to automatically select */
		public string firstSelectedElement;

		/** A List of MenuElement subclasses that are drawn within the Menu */
		public List<MenuElement> elements = new List<MenuElement>();

		/** The spacing between OnGUI MenuElement subclasses, when sizeType = AC_SizeType.Automatic */
		public float spacing;
		/** How the size of the OnGUI Menu is determined (AbsolutePixels, Automatic, Manual) */
		public AC_SizeType sizeType;
		/** If True, and sizeType = AC_SizeType.Automatic, then the dimensions of the Menu will be recalculated every frame */
		public bool autoSizeEveryFrame = false;

		/** How OnGUI MenuElements are arranged together (Horizontal, Vertical) */
		public MenuOrientation orientation;
		/** How an OnGUI Menu transitions in and out (Fade, FadeAndPan, None, Pan, Zoom) */
		public MenuTransition transitionType = MenuTransition.None;
		/** The pan direction of an OnGUI Menu, if the Menu pans when transitioning */
		public PanDirection panDirection = PanDirection.Up;
		/** The pan animation style of an OnGUI Menu, if the Menu pans when transitioning */
		public PanMovement panMovement = PanMovement.Linear;
		/** An AnimationCurve that describes the transition progress over time */
		public AnimationCurve timeCurve = new AnimationCurve (new Keyframe(0, 0), new Keyframe(1, 1));
		/** The pan distance of an OnGUI Menu, if the Menu pans when transitioning */
		public float panDistance = 0.5f;
		/** The transition duration, in seconds */
		public float fadeSpeed = 0f;
		/** The zoom alignment, if transitionType = MenuTransitio.Zoom */
		public TextAnchor zoomAnchor = TextAnchor.MiddleCenter;
		/** If True, then MenuElement subclasses will also re-size during zoom transitions */
		public bool zoomElements = false;
		/** If True, then a new instance of the Menu will be created for each speech line, if appearType = AppearType.WhenSpeechPlays */
		public bool oneMenuPerSpeech = false;
		/** The Speech instance tied to the Menu, if a duplicate was made specifically for it */
		public Speech speech;

		// Interaction menus
		private InvItem forItem;
		private Hotspot forHotspot;

		private float fadeStartTime = 0f;
		private bool isFading = false;
		private FadeType fadeType = FadeType.fadeIn;
		private Vector2 panOffset = Vector2.zero;
		private Vector2 dragOffset = Vector2.zero;
		private float zoomAmount = 1f;
		private Rect aspectCorrectedRect = new Rect ();

		private GameState gameStateWhenTurnedOn;
		private bool isEnabled;
		private bool isDisabledForScreenshot = false;
		[SerializeField] private Vector2 biggestElementSize;
		[SerializeField] private Rect rect = new Rect ();


		/**
		 * <summary>Initialises a Menu when it is created within MenuManager.</summary>
		 * <param name = "idArray">An array of previously-used ID numbers</param>
		 */
		public void Declare (int[] idArray)
		{
			menuSource = MenuSource.AdventureCreator;
			canvas = null;
			canvasID = 0;
			uiPositionType = UIPositionType.Manual;
			uiTransitionType = UITransition.None;

			spacing = 0.5f;
			orientation = MenuOrientation.Vertical;
			appearType = AppearType.Manual;
			oneMenuPerSpeech = false;

			fitWithinScreen = true;
			elements = new List<MenuElement>();
			visibleElements = new List<MenuElement>();
			enabledOnStart = false;
			isEnabled = false;
			sizeType = AC_SizeType.Automatic;
			autoSizeEveryFrame = false;
			speechMenuType = SpeechMenuType.All;
			speechMenuLimit = SpeechMenuLimit.All;
			limitToCharacters = "";
			actionListOnTurnOn = null;
			actionListOnTurnOff = null;
			firstSelectedElement = "";
			
			fadeSpeed = 0f;
			transitionType = MenuTransition.None;
			panDirection = PanDirection.Up;
			panMovement = PanMovement.Linear;
			timeCurve = new AnimationCurve (new Keyframe(0, 0), new Keyframe(1, 1));
			panDistance = 0.5f;
			zoomAnchor = TextAnchor.MiddleCenter;
			zoomElements = false;
			ignoreMouseClicks = false;
			
			pauseWhenEnabled = false;
			canClickInCutscene = false;
			id = 0;
			isLocked = false;
			updateWhenFadeOut = true;

			// Update id based on array
			foreach (int _id in idArray)
			{
				if (id == _id)
				{
					id ++;
				}
			}
			
			title = "Menu " + (id + 1).ToString ();
		}


		/**
		 * <summary>Copies the variables of another Menu onto itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <param name = "_menu">The Menu to copy from</param>
		 * <param name = "forceUIFields">If True, the variables related to Unity UI-sourced Menus will also be copied, regardless of the Menu's menuSource value</param>
		 */
		public void Copy (AC.Menu _menu, bool fromEditor, bool forceUIFields = false)
		{
			menuSource = _menu.menuSource;
			if (forceUIFields || menuSource == MenuSource.UnityUiPrefab || menuSource == MenuSource.UnityUiInScene)
			{
				canvas = _menu.canvas;
				canvasID = _menu.canvasID;
				rectTransform = _menu.rectTransform;
				rectTransformID = _menu.rectTransformID;
			}
			uiTransitionType = _menu.uiTransitionType;
			uiPositionType = _menu.uiPositionType;

			isEditing = false;
			id = _menu.id;
			isLocked = _menu.isLocked;
			title = _menu.title;
			manualSize = _menu.manualSize;
			autoSizeEveryFrame = _menu.autoSizeEveryFrame;
			positionType = _menu.positionType;
			manualPosition = _menu.manualPosition;
			fitWithinScreen = _menu.fitWithinScreen;
			alignment = _menu.alignment;
			toggleKey = _menu.toggleKey;

			backgroundTexture = _menu.backgroundTexture;
			visibleElements = new List<MenuElement>();
			transitionProgress = 0f;
			appearType = _menu.appearType;
			oneMenuPerSpeech = _menu.oneMenuPerSpeech;
			selected_element = null;
			selected_slot = 0;
			firstSelectedElement = _menu.firstSelectedElement;

			spacing = _menu.spacing;
			sizeType = _menu.sizeType;
			orientation = _menu.orientation;
			fadeSpeed = _menu.fadeSpeed;
			transitionType = _menu.transitionType;
			panDirection = _menu.panDirection;
			panMovement = _menu.panMovement;
			timeCurve = _menu.timeCurve;
			panDistance = _menu.panDistance;
			zoomAnchor = _menu.zoomAnchor;
			zoomElements = _menu.zoomElements;
			pauseWhenEnabled = _menu.pauseWhenEnabled;
			canClickInCutscene = _menu.canClickInCutscene;

			speechMenuType = _menu.speechMenuType;
			speechMenuLimit = _menu.speechMenuLimit;
			enabledOnStart = _menu.enabledOnStart;
			actionListOnTurnOn = _menu.actionListOnTurnOn;
			actionListOnTurnOff = _menu.actionListOnTurnOff;
			ignoreMouseClicks = _menu.ignoreMouseClicks;
			limitToCharacters = _menu.limitToCharacters;
			updateWhenFadeOut = _menu.updateWhenFadeOut;

			elements = new List<MenuElement>();
			foreach (MenuElement _element in _menu.elements)
			{
				MenuElement newElement = _element.DuplicateSelf (fromEditor);
				elements.Add (newElement);
			}
		}


		/**
		 * Instantiates and initialises a linked Unity UI Canvas, if Unity UI is used for display.
		 */
		public void LoadUnityUI ()
		{
			Canvas localCanvas = null;

			if (menuSource == MenuSource.UnityUiPrefab)
			{
				if (canvas != null)
				{
					localCanvas = (Canvas) Instantiate (canvas);
					localCanvas.gameObject.name = canvas.name;
					DontDestroyOnLoad (localCanvas.gameObject);
				}
			}
			else if (menuSource == MenuSource.UnityUiInScene)
			{
				localCanvas = Serializer.returnComponent <Canvas> (canvasID, KickStarter.sceneSettings.gameObject);
			}

			canvas = localCanvas;
			EnableUI ();

			if (localCanvas != null)
			{
				rectTransform = Serializer.returnComponent <RectTransform> (rectTransformID);
				if (localCanvas.renderMode != RenderMode.ScreenSpaceOverlay && localCanvas.worldCamera == null)
				{
					localCanvas.worldCamera = Camera.main;
				}

				if (localCanvas.renderMode != RenderMode.WorldSpace)
				{
					SetParent ();
				}
			}

			if (IsUnityUI ())
			{
				foreach (MenuElement _element in elements)
				{
					_element.LoadUnityUI (this);
				}
			}

			DisableUI ();
		}


		private void SetAnimState ()
		{
			if (IsUnityUI () && uiTransitionType == UITransition.CustomAnimation && fadeSpeed > 0f && canvas != null && canvas.GetComponent <Animator>())
			{
				Animator animator = canvas.GetComponent <Animator>();
				
				if (isFading)
				{
					if (fadeType == FadeType.fadeIn)
					{
						animator.Play ("On", -1, transitionProgress);
					}
					else
					{
						animator.Play ("Off", -1, 1f - transitionProgress);
					}
				}
				else
				{
					if (isEnabled)
					{
						animator.Play ("OnInstant", -1, 0f);
					}
					else
					{
						animator.Play ("OffInstant", -1, 0f);
					}
				}
			}
		}


		/**
		 * Places the linked Canvas in the "_UI" hierarchy folder, if Unity UI is used for display.
		 */
		public void SetParent ()
		{
			if (GetsDuplicated ()) return;

			GameObject uiOb = GameObject.Find ("_UI");
			if (uiOb != null && canvas != null)
			{
				uiOb.transform.position = Vector3.zero;
				canvas.transform.SetParent (uiOb.transform);
			}
		}


		/**
		 * <summary>Checks if the Menu gets duplicated for either each subtitle line or Hotspot.</summary>
		 * <returns>True if the Menu gets duplicated for either each subtitle line or Hotspot.</returns>
		 */
		public bool GetsDuplicated ()
		{
			if (oneMenuPerSpeech && appearType == AppearType.WhenSpeechPlays)
			{
				return true;
			}
			if (oneMenuPerSpeech && appearType == AppearType.OnHotspot && KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction)
			{
			//	return true;
			}
			return false;
		}


		/**
		 * Removes the linked Canvas from the "_UI" hierarchy folder, if Unity UI is used for display.
		 * This is necessary for prefabs that must survive scene changes.
		 */
		public void ClearParent ()
		{
			if (GetsDuplicated ()) return;

			GameObject uiOb = GameObject.Find ("_UI");
			if (uiOb != null && canvas != null)
			{
				if (canvas.transform.parent == uiOb.transform)
				{
					canvas.transform.SetParent (null);
				}
		    }
		}


		/**
		 * Initialises the Menu when the game begins.
		 */
		public void Initalise ()
		{
			if (appearType == AppearType.Manual && enabledOnStart && !isLocked)
			{
				transitionProgress = 1f;
				EnableUI ();
				TurnOn (false);
			}
			else
			{
				transitionProgress = 0f;
				DisableUI ();
				TurnOff (false);
			}
			if (transitionType == MenuTransition.Zoom)
			{
				zoomAmount = 0f;
			}

			foreach (MenuElement _element in elements)
			{
				_element.Initialise (this);
			}

			SetAnimState ();
			UpdateTransition ();
		}


		/**
		 * Enables the associated Unity UI canvas, if source != MenuSource.AdventureCreator
		 */
		public void EnableUI ()
		{
			if (canvas != null && menuSource != MenuSource.AdventureCreator)
			{
				canvas.gameObject.SetActive (true);
				canvas.enabled = true;
				KickStarter.playerMenus.FindFirstSelectedElement ();
			}
		}


		/**
		 * Disables the associated Unity UI canvas, if source != MenuSource.AdventureCreator
		 */
		public void DisableUI ()
		{
			if (canvas != null && menuSource != MenuSource.AdventureCreator)
			{
				isEnabled = false;
				isFading = false;
				SetAnimState ();

				canvas.gameObject.SetActive (false);
				KickStarter.playerMenus.FindFirstSelectedElement ();
			}
		}


		/**
		 * Makes all linked UI elements interactive, if the Menu is drawn with Unity UI.
		 */
		public void MakeUIInteractive ()
		{
			SetUIInteractableState (true);
		}


		/**
		 * Makes all linked UI elements non-interactive, if the Menu is drawn with Unity UI.
		 */
		public void MakeUINonInteractive ()
		{
			if (!IsClickable ())
			{
				SetUIInteractableState (false);
			}
		}


		private void SetUIInteractableState (bool state)
		{
			if (menuSource != MenuSource.AdventureCreator)
			{
				foreach (MenuElement element in visibleElements)
				{
					element.SetUIInteractableState (state);
				}
			}
		}


		#if UNITY_EDITOR
		
		public void ShowGUI ()
		{
			string apiPrefix = "AC.PlayerMenus.GetMenuWithName (\"" + title + "\")";

			title = CustomGUILayout.TextField ("Menu name:", title, apiPrefix + ".title");
			menuSource = (MenuSource) CustomGUILayout.EnumPopup ("Source:", menuSource, apiPrefix + ".menuSource");

			isLocked = CustomGUILayout.Toggle ("Start game locked off?", isLocked, apiPrefix + ".isLocked");
			ignoreMouseClicks = CustomGUILayout.Toggle ("Ignore Cursor clicks?", ignoreMouseClicks, apiPrefix + ".ignoreMouseClicks");
			actionListOnTurnOn = ActionListAssetMenu.AssetGUI ("ActionList when turn on:", actionListOnTurnOn, apiPrefix + ".actionListOnTurnOn", title + "_When_Turn_On");
			actionListOnTurnOff = ActionListAssetMenu.AssetGUI ("ActionList when turn off:", actionListOnTurnOff, apiPrefix + ".actionListOnTurnOff", title + "_When_Turn_Off");
			
			appearType = (AppearType) CustomGUILayout.EnumPopup ("Appear type:", appearType, apiPrefix + ".appearType");

			if (appearType == AppearType.OnInputKey)
			{
				toggleKey = CustomGUILayout.TextField ("Toggle key:", toggleKey, apiPrefix + ".toggleKey");
			}
			else if (appearType == AppearType.Manual)
			{
				enabledOnStart = CustomGUILayout.Toggle ("Enabled on start?", enabledOnStart, apiPrefix + ".enabledOnStart");
			}
			else if (appearType == AppearType.WhenSpeechPlays)
			{
				speechMenuType = (SpeechMenuType) EditorGUILayout.EnumPopup ("For speakers of type:", speechMenuType);
				speechMenuLimit = (SpeechMenuLimit) EditorGUILayout.EnumPopup ("For speech of type:", speechMenuLimit);
				oneMenuPerSpeech = CustomGUILayout.Toggle ("Duplicate for each line?", oneMenuPerSpeech, apiPrefix + ".oneMenuPerSpeech");

				if (speechMenuType == SpeechMenuType.SpecificCharactersOnly)
				{
					limitToCharacters = CustomGUILayout.TextField ("Character(s) to limit to:", limitToCharacters, apiPrefix + ".limitToCharacters");
					EditorGUILayout.HelpBox ("Multiple character names should be separated by a colon ';'", MessageType.Info);
				}
				else if (speechMenuType == SpeechMenuType.AllExceptSpecificCharacters)
				{
					limitToCharacters = CustomGUILayout.TextField ("Character(s) to exclude:", limitToCharacters, apiPrefix + ".limitToCharacters");
					EditorGUILayout.HelpBox ("Multiple character names should be separated by a colon ';'", MessageType.Info);
				}
			}
			else if (appearType == AppearType.OnHotspot)
			{
				if (AdvGame.GetReferences ().settingsManager != null && AdvGame.GetReferences ().settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
				//	oneMenuPerSpeech = CustomGUILayout.Toggle ("Duplicate for each Hotspot?", oneMenuPerSpeech, apiPrefix + ".oneMenuPerSpeech");
				}
			}

			if (CanPause ())
			{
				pauseWhenEnabled = CustomGUILayout.Toggle ("Pause game when enabled?", pauseWhenEnabled, apiPrefix + ".pauseWhenEnabled");
			}

			if (ShowClickInCutscenesOption () && !ignoreMouseClicks)
			{
				canClickInCutscene = CustomGUILayout.Toggle ("Clickable in cutscenes?", canClickInCutscene, apiPrefix + ".canClickInCutscene");
				if (canClickInCutscene)
				{
					EditorGUILayout.HelpBox ("Only Button, Toggle, and Cycle will be clickable during cutscenes.", MessageType.Info);
				}
			}

			if (menuSource == MenuSource.AdventureCreator)
			{
				spacing = CustomGUILayout.Slider ("Spacing (%):", spacing, 0f, 10f);
				orientation = (MenuOrientation) CustomGUILayout.EnumPopup ("Element orientation:", orientation, apiPrefix + ".orientation");
				
				positionType = (AC_PositionType) CustomGUILayout.EnumPopup ("Position:", positionType, apiPrefix + ".positionType");
				if (positionType == AC_PositionType.Aligned)
				{
					alignment = (TextAnchor) CustomGUILayout.EnumPopup ("Alignment:", alignment, apiPrefix + ".alignment");
				}
				else if (positionType == AC_PositionType.Manual || positionType == AC_PositionType.FollowCursor || positionType == AC_PositionType.AppearAtCursorAndFreeze || positionType == AC_PositionType.OnHotspot || positionType == AC_PositionType.AboveSpeakingCharacter || positionType == AC_PositionType.AbovePlayer)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("X:", GUILayout.Width (20f));
					manualPosition.x = EditorGUILayout.Slider (manualPosition.x, 0f, 100f);
					EditorGUILayout.LabelField ("Y:", GUILayout.Width (20f));
					manualPosition.y = EditorGUILayout.Slider (manualPosition.y, 0f, 100f);
					EditorGUILayout.EndHorizontal ();

					fitWithinScreen = EditorGUILayout.Toggle ("Always fit within screen?", fitWithinScreen);
				}
				
				sizeType = (AC_SizeType) EditorGUILayout.EnumPopup ("Size:", sizeType);
				if (sizeType == AC_SizeType.Manual)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("W:", GUILayout.Width (15f));
					manualSize.x = EditorGUILayout.Slider (manualSize.x, 0f, 100f);
					EditorGUILayout.LabelField ("H:", GUILayout.Width (15f));
					manualSize.y = EditorGUILayout.Slider (manualSize.y, 0f, 100f);
					EditorGUILayout.EndHorizontal ();
				}
				else if (sizeType == AC_SizeType.AbsolutePixels)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Width:", GUILayout.Width (50f));
					manualSize.x = EditorGUILayout.FloatField (manualSize.x);
					EditorGUILayout.LabelField ("Height:", GUILayout.Width (50f));
					manualSize.y = EditorGUILayout.FloatField (manualSize.y);
					EditorGUILayout.EndHorizontal ();
				}
				else if (sizeType == AC_SizeType.Automatic)
				{
					autoSizeEveryFrame = CustomGUILayout.Toggle ("Resize every frame?", autoSizeEveryFrame, apiPrefix + ".autoSizeEveryFrame");
					if (autoSizeEveryFrame)
					{
						EditorGUILayout.HelpBox ("This process is fairly CPU-intensive, so only use it if your are having display issues without it.", MessageType.Info);
					}
				}
				
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Background texture:", GUILayout.Width (145f));
				backgroundTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (backgroundTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".backgroundTexture");
				EditorGUILayout.EndHorizontal ();
				
				transitionType = (MenuTransition) CustomGUILayout.EnumPopup ("Transition type:", transitionType, apiPrefix + ".transitionType");
				if (transitionType == MenuTransition.Pan || transitionType == MenuTransition.FadeAndPan)
				{
					panDirection = (PanDirection) CustomGUILayout.EnumPopup ("Pan from:", panDirection, apiPrefix + ".panDirection");
					panDistance = CustomGUILayout.Slider ("Pan distance:", panDistance, 0f, 1f, apiPrefix + ".panDistance");
				}
				else if (transitionType == MenuTransition.Zoom)
				{
					zoomAnchor = (TextAnchor) CustomGUILayout.EnumPopup ("Zoom from:", zoomAnchor, apiPrefix + ".zoomAnchor");
					zoomElements = CustomGUILayout.Toggle ("Adjust elements?", zoomElements, apiPrefix + ".zoomElements");
				}
				else if (transitionType == MenuTransition.Fade)
				{
				}
				if (transitionType != MenuTransition.None)
				{
					fadeSpeed = CustomGUILayout.Slider ("Transition time (s):", fadeSpeed, 0f, 2f, apiPrefix + ".fadeSpeed");
					TransitionAnimGUI (apiPrefix);

					if (fadeSpeed > 0f)
					{
						updateWhenFadeOut = EditorGUILayout.Toggle ("Update while fading out?", updateWhenFadeOut);
					}
				}
			}
			else
			{
				uiPositionType = (UIPositionType) CustomGUILayout.EnumPopup ("Position type:", uiPositionType, apiPrefix + ".uiPositionType");
				fitWithinScreen = CustomGUILayout.Toggle ("Always fit within screen?", fitWithinScreen, apiPrefix + ".fitWithinScreen");
				uiTransitionType = (UITransition) CustomGUILayout.EnumPopup ("Transition type:", uiTransitionType, apiPrefix + ".uiTransitionType");
				if (uiTransitionType != UITransition.None)
				{
					fadeSpeed = CustomGUILayout.Slider ("Transition time (s):", fadeSpeed, 0f, 2f, apiPrefix + ".fadeSpeed");
					if (uiTransitionType == UITransition.CanvasGroupFade)
					{
						TransitionAnimGUI (apiPrefix);
						if (canvas == null || canvas.GetComponent <CanvasGroup>() == null)
						{
							EditorGUILayout.HelpBox ("A Canvas Group component must be attached to the Canvas object.", MessageType.Info);
						}
					}
					else if (uiTransitionType == UITransition.CustomAnimation)
					{
						EditorGUILayout.HelpBox ("The Canvas must have an Animator with 4 States: On, Off, OnInstant and OffInstant.", MessageType.Info);
					}

					if (uiTransitionType != UITransition.None && fadeSpeed > 0f)
					{
						updateWhenFadeOut = EditorGUILayout.Toggle ("Update while fading out?", updateWhenFadeOut);
					}
				}

				bool isInScene = false;
				if (menuSource == MenuSource.UnityUiInScene)
				{
					isInScene = true;
				}

				canvas = (Canvas) EditorGUILayout.ObjectField ("Linked Canvas:", canvas, typeof (Canvas), isInScene);
				if (isInScene)
				{
					canvasID = Menu.FieldToID <Canvas> (canvas, canvasID);
					canvas = Menu.IDToField <Canvas> (canvas, canvasID, menuSource);
				}

				rectTransform = (RectTransform) CustomGUILayout.ObjectField <RectTransform> ("RectTransform boundary:", rectTransform, true, apiPrefix + ".rectTransform");
				rectTransformID = Menu.FieldToID <RectTransform> (rectTransform, rectTransformID);
				rectTransform = Menu.IDToField <RectTransform> (rectTransform, rectTransformID, menuSource);

				firstSelectedElement = CustomGUILayout.TextField ("First selected Element:", firstSelectedElement, apiPrefix + ".firstSelectedElement");
				EditorGUILayout.HelpBox ("For UIs to be keyboard-controlled, the name of the first selected element must be entered above.", MessageType.Info);
			}
		}


		private void TransitionAnimGUI (string apiPrefix)
		{
			panMovement = (PanMovement) CustomGUILayout.EnumPopup ("Transition animation:", panMovement, apiPrefix + ".panMovement");
			if (panMovement == PanMovement.CustomCurve && fadeSpeed > 0f)
			{
				timeCurve = CustomGUILayout.CurveField ("Time curve:", timeCurve, apiPrefix + ".timeCurve");
			}
		}


		public static int FieldToID <T> (T field, int _constantID) where T : Component
		{
			if (field == null)
			{
				return _constantID;
			}
			
			if (field.GetComponent <ConstantID>())
			{
				if (!field.gameObject.activeInHierarchy && field.GetComponent <ConstantID>().constantID == 0)
				{
					field.GetComponent <ConstantID>().AssignInitialValue (true);
				}
				_constantID = field.GetComponent <ConstantID>().constantID;
			}
			else
			{
				field.gameObject.AddComponent <ConstantID>();
				_constantID = field.GetComponent <ConstantID>().AssignInitialValue (true);
				AssetDatabase.SaveAssets ();
			}
			
			return _constantID;
		}
		
		
		public static T IDToField <T> (T field, int _constantID, MenuSource source) where T : Component
		{
			if (Application.isPlaying || source == MenuSource.AdventureCreator)
			{
				return field;
			}
			
			T newField = field;
			if (_constantID != 0)
			{
				newField = Serializer.returnComponent <T> (_constantID);
				if (newField != null && source == MenuSource.UnityUiInScene)
				{
					field = newField;
				}
				
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
				if (field == null && source == MenuSource.UnityUiInScene)
				{
					if (GUILayout.Button ("Search scenes", EditorStyles.miniButton))
					{
						AdvGame.FindObjectWithConstantID (_constantID);
					}
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.EndVertical ();
			}
			return field;
		}
		
		#endif


		/**
		 * <summary>Checks if Unity UI is used for the Menu's display, rather than OnGUI.</summary>
		 * <returns>True if Unity UI is used for the Menu's display</returns>
		 */
		public bool IsUnityUI ()
		{
			if (menuSource == MenuSource.UnityUiPrefab || menuSource == MenuSource.UnityUiInScene)
			{
				return true;
			}
			return false;
		}


		/**
		 * Draws an outline around the Menu and the MenuElement subclasses it houses.
		 */
		public void DrawOutline (MenuElement _selectedElement)
		{
			DrawStraightLine.DrawBox (rect, Color.yellow, 1f, false, 1);
			
			foreach (MenuElement element in visibleElements)
			{
				if (element == _selectedElement)
				{
					element.DrawOutline (true, this);
				}
				{
					element.DrawOutline (false, this);
				}
			}
		}
		

		/**
		 * Begins the display of an OnGUI-based Menu.
		 */
		public void StartDisplay ()
		{
			if (isFading)
			{
				GUI.BeginGroup (new Rect (dragOffset.x + panOffset.x + GetRect ().x, dragOffset.y + panOffset.y + GetRect ().y, GetRect ().width * zoomAmount, GetRect ().height * zoomAmount));
			}
			else
			{
				GUI.BeginGroup (new Rect (dragOffset.x + GetRect ().x, dragOffset.y + GetRect ().y, GetRect ().width * zoomAmount, GetRect ().height * zoomAmount));
			}

			if (backgroundTexture)
			{
				Rect texRect = new Rect (0f, 0f, rect.width, rect.height);
				GUI.DrawTexture (texRect, backgroundTexture, ScaleMode.StretchToFill, true, 0f);
			}
		}
		

		/**
		 * Ends the display of an OnGUI-based Menu.
		 */
		public void EndDisplay ()
		{
			GUI.EndGroup ();
		}
	

		/**
		 * <summary>Sets the centre-point of a 3D Menu.</summary>
		 * <param name = "_position">The position in 3D space to place the Menu's centre.</param>
		 */
		public void SetCentre (Vector3 _position)
		{
			if (IsUnityUI ())
			{
				if (canvas != null && rectTransform != null && canvas.renderMode == RenderMode.WorldSpace)
				{
					rectTransform.transform.position = _position;
				}
				
				return;
			}
			
			Vector2 centre = new Vector2 (_position.x * AdvGame.GetMainGameViewSize ().x, _position.y * AdvGame.GetMainGameViewSize ().y);
			
			rect.x = centre.x - (rect.width / 2);
			rect.y = centre.y - (rect.height / 2);
			
			FitMenuInsideScreen ();
			UpdateAspectRect ();
		}
		

		/**
		 * <summary>Sets the centre-point of a 2D Menu.</summary>
		 * <param name = "_position">The position in Screen Space to place the Menu's centre.</param>
		 */
		public void SetCentre (Vector2 _position)
		{
			if (IsUnityUI ())
			{
				if (canvas != null && rectTransform != null)
				{
					if (canvas.renderMode != RenderMode.WorldSpace)
					{
						float minLeft = rectTransform.sizeDelta.x * (1f - rectTransform.pivot.x) * canvas.scaleFactor * rectTransform.localScale.x;
						float minTop = rectTransform.sizeDelta.y * (1f - rectTransform.pivot.y) * canvas.scaleFactor * rectTransform.localScale.y;

						float maxLeft = rectTransform.sizeDelta.x * rectTransform.pivot.x * canvas.scaleFactor * rectTransform.localScale.x;
						float maxTop = rectTransform.sizeDelta.y * rectTransform.pivot.y * canvas.scaleFactor * rectTransform.localScale.y;

						if (fitWithinScreen)
						{
							_position.x = Mathf.Clamp (_position.x, maxLeft, Screen.width - minLeft);
							_position.y = Mathf.Clamp (_position.y, maxTop, Screen.height - minTop);
						}
					}

					rectTransform.transform.position = new Vector3 (_position.x, _position.y, rectTransform.transform.position.z);
				}

				return;
			}

			Vector2 centre = new Vector2 (_position.x * AdvGame.GetMainGameViewSize ().x, _position.y * AdvGame.GetMainGameViewSize ().y);
			
			rect.x = centre.x - (rect.width / 2);
			rect.y = centre.y - (rect.height / 2);
			
			FitMenuInsideScreen ();
			UpdateAspectRect ();
		}
		
		
		private Vector2 GetCentre ()
		{
			Vector2 centre = Vector2.zero;
			
			centre.x = (rect.x + (rect.width / 2)) / AdvGame.GetMainGameViewSize ().x * 100f;
			centre.y = (rect.y + (rect.height / 2)) / AdvGame.GetMainGameViewSize ().y * 100f;
			
			return centre;
		}
		
		
		private void FitMenuInsideScreen ()
		{
			if (!fitWithinScreen)
			{
				return;
			}

			if (rect.x < 0f)
			{
				rect.x = 0f;
			}
			
			if (rect.y < 0f)
			{
				rect.y = 0f;
			}
			
			if ((rect.x + rect.width) > AdvGame.GetMainGameViewSize ().x)
			{
				rect.x = AdvGame.GetMainGameViewSize ().x - rect.width;
			}
			
			if ((rect.y + rect.height) > AdvGame.GetMainGameViewSize ().y)
			{
				rect.y = AdvGame.GetMainGameViewSize ().y - rect.height;
			}
		}
		

		/**
		 * <summary>Aligns an OnGUI Menu to an area of the screen.</summary>
		 * <param name = "_anchor">The alignement to make</param>
		 */
		public void Align (TextAnchor _anchor)
		{
			// X
			if (_anchor == TextAnchor.LowerLeft || _anchor == TextAnchor.MiddleLeft || _anchor == TextAnchor.UpperLeft)
			{
				rect.x = 0;
			}
			else if (_anchor == TextAnchor.LowerCenter || _anchor == TextAnchor.MiddleCenter || _anchor == TextAnchor.UpperCenter)
			{
				rect.x = (AdvGame.GetMainGameViewSize ().x - rect.width) / 2;
			}
			else
			{
				rect.x = AdvGame.GetMainGameViewSize ().x - rect.width;
			}
			
			// Y
			if (_anchor == TextAnchor.LowerLeft || _anchor == TextAnchor.LowerCenter || _anchor == TextAnchor.LowerRight)
			{
				rect.y = AdvGame.GetMainGameViewSize ().y - rect.height;
			}
			else if (_anchor == TextAnchor.MiddleLeft || _anchor == TextAnchor.MiddleCenter || _anchor == TextAnchor.MiddleRight)
			{
				rect.y = (AdvGame.GetMainGameViewSize ().y - rect.height) / 2;
			}
			else
			{
				rect.y = 0;
			}
		}
		
		
		private void SetManualSize (Vector2 _size)
		{
			rect.width = _size.x * AdvGame.GetMainGameViewSize ().x;
			rect.height = _size.y * AdvGame.GetMainGameViewSize ().y;
		}


		/**
		 * <summary>Checks if a point in Screen Space lies within the Menu's boundary.</summary>
		 * <param name = "_point">The point to check for</param>
		 * <returns>True if the point is within the Menu's boundary.</returns>
		 */
		public bool IsPointInside (Vector2 _point)
		{
			if (menuSource == MenuSource.AdventureCreator)
			{
				return GetRect ().Contains (_point);
			}
			else if (rectTransform != null && canvas != null)
			{
				bool turnOffAgain = false;
				bool answer = false;
				if (!canvas.gameObject.activeSelf)
				{
					canvas.gameObject.SetActive (true);
					turnOffAgain = true;
				}

				if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					answer = RectTransformUtility.RectangleContainsScreenPoint (rectTransform, new Vector2 (_point.x, Screen.height - _point.y), null);
				}
				else
				{
					answer = RectTransformUtility.RectangleContainsScreenPoint (rectTransform, new Vector2 (_point.x, Screen.height - _point.y), canvas.worldCamera);
				}

				if (turnOffAgain)
				{
					canvas.gameObject.SetActive (false);
				}

				return answer;
			}
			return false;
		}
		

		/**
		 * <summary>Gets a Rect that describes an OnGUI Menu's boundary.</summary>
		 * <returns>A Rect that describes an OnGUI Menu's boundary.</returns>
		 */
		public Rect GetRect ()
		{
			if (!Application.isPlaying)
			{
				if (KickStarter.mainCamera)
				{
					return KickStarter.mainCamera.LimitMenuToAspect (rect);
				}
				return rect;
			}
			
			if (aspectCorrectedRect == new Rect ())
			{
				UpdateAspectRect ();
			}
			
			return aspectCorrectedRect;
		}
		

		/**
		 * Updates an OnGUI Menu's boundary so that it fits within the fixed aspect ratio chosen in SettingsManager.
		 */
		public void UpdateAspectRect ()
		{
			if (IsUnityUI ())
			{
				return;
			}

			// This used to be called every GetRect (), but is now only done when the menu changes position
			if (KickStarter.mainCamera)
			{
				aspectCorrectedRect = KickStarter.mainCamera.LimitMenuToAspect (rect);
			}
		}
		

		/**
		 * <summary>Checks if a point in Screen Space within a specific slot of a specific MenuElement.</summary>
		 * <param name = "_element">The MenuElement to check for</param>
		 * <param name = "slot">The slot to check for</param>
		 * <param name = "_point">The point to check is within the MenuElement slot.</param>
		 * <returns>True if the point is within the boundary of the MenuElement slot</returns>
		 */
		public bool IsPointerOverSlot (MenuElement _element, int slot, Vector2 _point) 
		{
			if (menuSource == MenuSource.AdventureCreator)
			{
				Rect rectRelative = _element.GetSlotRectRelative (slot);
				Rect rectAbsolute = GetRectAbsolute (rectRelative);
				return (rectAbsolute.Contains (_point));
			}
			else if (canvas != null)
			{
				RectTransform slotRectTransform = _element.GetRectTransform (slot);
				if (slotRectTransform != null)
				{
					if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
					{
						return RectTransformUtility.RectangleContainsScreenPoint (slotRectTransform, new Vector2 (_point.x, Screen.height - _point.y), null);
					}
					else
					{
						return RectTransformUtility.RectangleContainsScreenPoint (slotRectTransform, new Vector2 (_point.x, Screen.height - _point.y), canvas.worldCamera);
					}
				}
			}
			return false;
		}
		

		/**
		 * <summary>Converts a Rect that's relative to an OnGUI Menu's boundary to Screen Space.</summary>
		 * <param name = "_rectRelative">The relative Rect to convert</param>
		 * <returns>The Rect converted to Screen Space co-ordinates</returns>
		 */
		public Rect GetRectAbsolute (Rect _rectRelative)
		{
			return (new Rect (_rectRelative.x + dragOffset.x + GetRect ().x, _rectRelative.y + dragOffset.y + GetRect ().y, _rectRelative.width, _rectRelative.height));
		}
		

		/**
		 * Re-populates the visibleElements List with MenuElement subclasses that are visible
		 */
		public void ResetVisibleElements ()
		{
			visibleElements.Clear ();
			foreach (MenuElement element in elements)
			{
				element.RecalculateSize (menuSource);
				if (element.isVisible)
				{
					visibleElements.Add (element);
				}
			}
		}


		/**
		 * Refreshes any active MenuDialogList elements, after changing the state of dialogue options.
		 */
		public void RefreshDialogueOptions ()
		{
			if (appearType == AppearType.DuringConversation && !IsOff ())
			{
				foreach (MenuElement element in visibleElements)
				{
					if (element is MenuDialogList)
					{
						element.RecalculateSize (menuSource);
					}
				}
			}
		}
		

		/**
		 * Recalculates all position, size and display variables - accounting for hidden and re-sized elements.
		 * This should be called whenever a Menu's shape is changed.
		 */
		public void Recalculate ()
		{
			if (IsUnityUI ())
			{
				AutoResize ();
				return;
			}

			ResetVisibleElements ();
			PositionElements ();
			
			if (sizeType == AC_SizeType.Automatic)
			{
				AutoResize ();
			}
			else if (sizeType == AC_SizeType.Manual)
			{
				SetManualSize (new Vector2 (manualSize.x / 100f, manualSize.y / 100f));
			}
			else if (sizeType == AC_SizeType.AbsolutePixels)
			{
				rect.width = manualSize.x;
				rect.height = manualSize.y;
			}
			
			if (positionType == AC_PositionType.Centred)
			{
				Centre ();
				manualPosition = GetCentre ();
			}
			else if (positionType == AC_PositionType.Aligned)
			{
				Align (alignment);
				manualPosition = GetCentre ();
			}
			else if (positionType == AC_PositionType.Manual || !Application.isPlaying)
			{
				SetCentre (new Vector2 (manualPosition.x / 100f, manualPosition.y / 100f));
			}

			if (sizeType == AC_SizeType.Automatic)
			{
				UpdateAspectRect ();
			}
		}
		

		/**
		 * Resizes a Menu that's size is dependent on the elements within it.
		 */
		public void AutoResize ()
		{
			visibleElements.Clear ();
			biggestElementSize = new Vector2 ();
			
			foreach (MenuElement element in elements)
			{
				if (element != null)
				{
					element.RecalculateSize (menuSource);

					if (element.isVisible)
					{
						visibleElements.Add (element);

						if (menuSource == MenuSource.AdventureCreator)
						{
							if (element.GetSizeFromCorner ().x > biggestElementSize.x)
							{
								biggestElementSize.x = element.GetSizeFromCorner ().x;
							}
							
							if (element.GetSizeFromCorner ().y > biggestElementSize.y)
							{
								biggestElementSize.y = element.GetSizeFromCorner ().y;
							}
						}
					}
				}
			}

			if (menuSource == MenuSource.AdventureCreator)
			{
				rect.width = (spacing / 100 * AdvGame.GetMainGameViewSize ().x) + biggestElementSize.x;
				rect.height = (spacing / 100 * AdvGame.GetMainGameViewSize ().x) + biggestElementSize.y;
				manualSize = new Vector2 (rect.width * 100f / AdvGame.GetMainGameViewSize ().x, rect.height * 100f / AdvGame.GetMainGameViewSize ().y);
			}
		}
		
		
		private void PositionElements ()
		{
			float totalLength = 0f;
			
			foreach (MenuElement element in visibleElements)
			{
				if (menuSource != MenuSource.AdventureCreator)
				{
					element.RecalculateSize (menuSource);
					return;
				}

				if (element == null)
				{
					ACDebug.Log ("Null element found");
					break;
				}
				
				if (element.positionType == AC_PositionType2.RelativeToMenuSize && sizeType == AC_SizeType.Automatic)
				{
					ACDebug.LogError ("Menu " + title + " cannot display because its size is Automatic, while its Element " + element.title + "'s Position is set to Relative");
					return;
				}
				
				element.RecalculateSize (menuSource);
				
				if (element.positionType == AC_PositionType2.RelativeToMenuSize)
				{
					element.SetRelativePosition (new Vector2 (rect.width / 100f, rect.height / 100f));
				}
				else if (orientation == MenuOrientation.Horizontal)
				{
					if (element.positionType == AC_PositionType2.Aligned)
					{
						element.SetPosition (new Vector2 ((spacing / 100 * AdvGame.GetMainGameViewSize ().x) + totalLength, (spacing / 100 * AdvGame.GetMainGameViewSize ().x)));
					}
					
					totalLength += element.GetSize().x + (spacing / 100 * AdvGame.GetMainGameViewSize ().x);
				}
				else
				{
					if (element.positionType == AC_PositionType2.Aligned)
					{
						element.SetPosition (new Vector2 ((spacing / 100 * AdvGame.GetMainGameViewSize ().x), (spacing / 100 * AdvGame.GetMainGameViewSize ().x) + totalLength));
					}
					
					totalLength += element.GetSize().y + (spacing / 100 * AdvGame.GetMainGameViewSize ().x);
				}
			}
		}
		

		/**
		 * Positions an OnGUI Menu in the centre of the screen.
		 */
		public void Centre ()
		{
			SetCentre (new Vector2 (0.5f, 0.5f));
		}
		

		/**
		 * <summary>Checks if the Menu is currently enabled.</summary>
		 * <returns>True if the Menu is currently enabled.</return>
		 */
		public bool IsEnabled ()
		{
			if (isLocked)
			{
				if (isFading && fadeType == FadeType.fadeOut)
				{
					return isEnabled;
				}
				
				return false;
			}
			
			return (isEnabled);
		}
		

		/**
		 * <summary>Checks if the Menu is fully visible or not.</summary>
		 * <returns>True if the Menu is fully visible; False will be returned while midway through a transition.</returns>
		 */
		public bool IsVisible ()
		{
			if (transitionProgress == 1f && isEnabled)
			{
				return true;
			}
			
			return false;
		}
		

		private void EndTransitionOn ()
		{
			transitionProgress = 1f;
			isEnabled = true;
			isFading = false;
		}
		
		
		private void EndTransitionOff ()
		{
			transitionProgress = 0f;
			isFading = false;
			isEnabled = false;
			SetAnimState ();
			ReturnGameState ();
			DisableUI ();
			ClearSpeechText ();

			KickStarter.playerMenus.CheckCrossfade (this);
		}
		

		/**
		 * <summary>Checks if the Menu is fully on or not.</summary>
		 * <returns>True if the Menu is fully on.</returns>
		 */
		public bool IsOn ()
		{
			if (!isLocked && isEnabled && !isFading)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if the Menu is fully off or not.</summary>
		 * <returns>True if the Menu is fully off.</returns>
		 */
		public bool IsOff ()
		{
			if (isLocked)
			{
				return true;
			}
			if (!isEnabled)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the Menu transitions over time when being enabled or disabled.</summary>
		 * <returns>True if the Menu transitions over time</returns>
		 */
		public bool HasTransition ()
		{
			if (fadeSpeed == 0f)
			{
				return false;
			}
			if (IsUnityUI ())
			{
				if (uiTransitionType != UITransition.None)
				{
					return true;
				}
			}
			else if (transitionType != MenuTransition.None)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets the value of StateHandler's gameState variable at the point that the Menu was last turned on.</summary>
		 * <returns>The value of StateHandler's gameState variable at the point that the Menu was last turned on</returns>
		 */
		public GameState GetGameStateWhenTurnedOn ()
		{
			return gameStateWhenTurnedOn;
		}
		

		/**
		 * <summary>Turns the Menu on.</summary>
		 * <param name = "doFade">If True, the Menu will play its transition animation; otherwise, it will turn on instantly.</param>
		 * <returns>True if the Menu was turned on. False if the Menu was already on.</returns>
		 */
		public bool TurnOn (bool doFade = true)
		{
			if (IsOn ())
			{
				return false;
			}

			gameStateWhenTurnedOn = KickStarter.stateHandler.gameState;
			if (menuSource == MenuSource.AdventureCreator)
			{
				KickStarter.playerMenus.UpdateMenuPosition (this, Vector2.zero);
			}

			if (!HasTransition ())
			{
				doFade = false;
			}

			// Setting selected_slot to -2 will cause PlayerInput's selected_option to reset
			if (isLocked)
			{
				#if UNITY_EDITOR
				ACDebug.Log ("Cannot turn on menu " + title + " as it is locked.");
				#endif
			}
			else if (!isEnabled || (isFading && fadeType == FadeType.fadeOut))
			{
				if (KickStarter.playerInput)
				{
					if (menuSource == MenuSource.AdventureCreator && positionType == AC_PositionType.AppearAtCursorAndFreeze)
					{
						SetCentre (new Vector2 ((KickStarter.playerInput.GetInvertedMouse ().x / Screen.width) + ((manualPosition.x - 50f) / 100f),
						                        (KickStarter.playerInput.GetInvertedMouse ().y / Screen.height) + ((manualPosition.y - 50f) / 100f)));
					}
					else if (menuSource != MenuSource.AdventureCreator && uiPositionType == UIPositionType.AppearAtCursorAndFreeze)
					{
						EnableUI (); // Necessary because scaling issues occur otherwise
						SetCentre (new Vector2 (KickStarter.playerInput.GetInvertedMouse ().x, Screen.height + 1f - KickStarter.playerInput.GetInvertedMouse ().y));
					}
				}

				selected_slot = -2;
				
				MenuSystem.OnMenuEnable (this);
				ChangeGameState ();
				Recalculate ();
				
				dragOffset = Vector2.zero;
				isEnabled = true;
				isFading = doFade;
				EnableUI ();
				
				if (actionListOnTurnOn != null)
				{
					AdvGame.RunActionListAsset (actionListOnTurnOn);
				}
				
				if (doFade && fadeSpeed > 0f)
				{
					fadeType = FadeType.fadeIn;
					fadeStartTime = Time.realtimeSinceStartup - (transitionProgress * fadeSpeed);
				}
				else
				{
					transitionProgress = 1f;
					isEnabled = true;
					isFading = false;

					if (IsUnityUI ())
					{
						UpdateTransition ();
					}
				}
				SetAnimState ();
			}
			return true;
		}


		/**
		 * <summary>Turns the Menu off.</summary>
		 * <param name = "doFade">If True, the Menu will play its transition animation; otherwise, it will turn off instantly.</param>
		 * <returns>True if the Menu was turned off. False if the Menu was already off.</returns>
		 */
		public bool TurnOff (bool doFade = true)
		{
			if (IsOff ())
			{
				return false;
			}

			if (actionListOnTurnOff != null && !isFading)
			{
				AdvGame.RunActionListAsset (actionListOnTurnOff);
			}
			
			if (appearType == AppearType.OnContainer)
			{
				KickStarter.playerInput.activeContainer = null;
			}
			
			if (!HasTransition ())
			{
				doFade = false;
			}
			
			if (isEnabled && (!isFading || (isFading && fadeType == FadeType.fadeIn)))// && appearType == AppearType.OnHotspot)))
			{
				isFading = doFade;
				
				if (doFade && fadeSpeed > 0f)
				{
					fadeType = FadeType.fadeOut;
					fadeStartTime = Time.realtimeSinceStartup - ((1f - transitionProgress) * fadeSpeed);
					SetAnimState ();
				}
				else
				{
					transitionProgress = 0f;
					UpdateTransition ();
					isFading = false;
					isEnabled = false;
					ReturnGameState ();
					DisableUI ();
					ClearSpeechText ();
				}
			}
			
			return true;
		}
		

		/**
		 * Forces the Menu off instantly.
		 */
		public void ForceOff ()
		{
			if (isEnabled || isFading)
			{
				transitionProgress = 0f;
				UpdateTransition ();
				isFading = false;
				isEnabled = false;
				DisableUI ();
				ClearSpeechText ();
				ReturnGameState ();
			}
		}


		/**
		 * <summary>Checks if the Menu is transitioning in.</summary>
		 * <returns>True if the Menu is transitioning in</returns>
		 */
		public bool IsFadingIn ()
		{
			if (isFading && fadeType == FadeType.fadeIn)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the Menu is transitioning out.</summary>
		 * <returns>True if the Menu is transitioning out</returns>
		 */
		public bool IsFadingOut ()
		{
			if (isFading && fadeType == FadeType.fadeOut)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if the Menu is transitioning in or out.</summary>
		 * <returns>True if the Menu is transitioning in or out</returns>
		 */
		public bool IsFading ()
		{
			return isFading;
		}


		/**
		 * <summary>Gets the progression through the Menu's transition animation (0 = fully on, 1 = fully off)</summary>
		 * <returns>The progression through the Menu's transition animation</returns>
		 */
		public float GetFadeProgress ()
		{
			if (panMovement == PanMovement.Linear)
			{
				return (1f - transitionProgress);
			}
			else if (panMovement == PanMovement.Smooth)
			{
				return ((transitionProgress * transitionProgress) - (2 * transitionProgress) + 1);
			}
			else if (panMovement == PanMovement.CustomCurve)
			{
				float startTime = timeCurve [0].time;
				float endTime = timeCurve [timeCurve.length - 1].time;
				
				return 1f - timeCurve.Evaluate ((endTime - startTime) * transitionProgress);
			}
			return 0f;
		}


		/**
		 * Updates the transition animation.
		 * This is called every frame by PlayerMenus.
		 */
		public void HandleTransition ()
		{
			if (isFading && isEnabled)
			{
				if (fadeType == FadeType.fadeIn)
				{
					transitionProgress = ((Time.realtimeSinceStartup - fadeStartTime) / fadeSpeed);
					
					if (transitionProgress > 1f)
					{
						transitionProgress = 1f;
						UpdateTransition ();
						EndTransitionOn ();
						return;
					}
					else
					{
						UpdateTransition ();
					}
				}
				else
				{
					transitionProgress = 1f - ((Time.realtimeSinceStartup - fadeStartTime) / fadeSpeed);

					if (transitionProgress < 0f)
					{
						transitionProgress = 0f;
						UpdateTransition ();
						EndTransitionOff ();
						return;
					}
					else
					{
						UpdateTransition ();
					}
				}
			}
		}
		

		private void UpdateTransition ()
		{
			if (IsUnityUI ())
			{
				if (uiTransitionType == UITransition.CanvasGroupFade && canvas != null && fadeSpeed > 0f)
				{
					CanvasGroup canvasGroup = canvas.GetComponent <CanvasGroup>();
					canvasGroup.alpha = 1f - GetFadeProgress ();
				}
				return;
			}

			if (transitionType == MenuTransition.Fade)
			{
				return;
			}
			
			if (transitionType == MenuTransition.FadeAndPan || transitionType == MenuTransition.Pan)
			{
				float amount = GetFadeProgress () * panDistance;

				if (panDirection == PanDirection.Down)
				{
					panOffset = new Vector2 (0f, amount);
				}
				else if (panDirection == PanDirection.Left)
				{
					panOffset = new Vector2 (-amount, 0f);
				}
				else if (panDirection == PanDirection.Up)
				{
					panOffset = new Vector2 (0f, -amount);
				}
				else if (panDirection == PanDirection.Right)
				{
					panOffset = new Vector2 (amount, 0f);
				}
				
				panOffset = new Vector2 (panOffset.x * AdvGame.GetMainGameViewSize ().x, panOffset.y * AdvGame.GetMainGameViewSize ().y);
			}
			
			else if (transitionType == MenuTransition.Zoom)
			{
				//zoomAmount = transitionProgress;
				zoomAmount = 1f - GetFadeProgress ();
				
				if (zoomAnchor == TextAnchor.UpperLeft)
				{
					panOffset = Vector2.zero;
				}
				else if (zoomAnchor == TextAnchor.UpperCenter)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width / 2f, 0f);
				}
				else if (zoomAnchor == TextAnchor.UpperRight)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width, 0f);
				}
				else if (zoomAnchor == TextAnchor.MiddleLeft)
				{
					panOffset = new Vector2 (0f, (1f - zoomAmount) * rect.height / 2f);
				}
				else if (zoomAnchor == TextAnchor.MiddleCenter)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width / 2f, (1f - zoomAmount) * rect.height / 2f);
				}
				else if (zoomAnchor == TextAnchor.MiddleRight)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width, (1f - zoomAmount) * rect.height / 2f);
				}
				else if (zoomAnchor == TextAnchor.LowerLeft)
				{
					panOffset = new Vector2 (0, (1f - zoomAmount) * rect.height);
				}
				else if (zoomAnchor == TextAnchor.LowerCenter)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width / 2f, (1f - zoomAmount) * rect.height);
				}
				else if (zoomAnchor == TextAnchor.LowerRight)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width, (1f - zoomAmount) * rect.height);
				}
			}
		}


		/**
		 * Pauses the game if appropriate after a scene-change.
		 */
		public void AfterSceneChange ()
		{
			if (IsOn ())
			{
				ChangeGameState ();
			}
		}
		
		
		private void ChangeGameState ()
		{
			if (IsBlocking () && Application.isPlaying)
			{
				if (appearType != AppearType.OnInteraction)
				{
					KickStarter.playerInteraction.DeselectHotspot (true);
				}
				KickStarter.mainCamera.FadeIn (0);
				KickStarter.mainCamera.PauseGame (true);
			}
		}
		
		
		private void ReturnGameState ()
		{
			if (IsBlocking () && !KickStarter.playerMenus.ArePauseMenusOn (this) && Application.isPlaying)
			{
				KickStarter.stateHandler.RestoreLastNonPausedState ();
			}
		}


		/**
		 * <summary>Checks if the Menu's appearType is such that the pauseWhenEnabled option is valid.</summary>
		 * <returns>True if the Menu's appearType is such that the pauseWhenEnabled option is valid.</returns>
		 */
		public bool CanPause ()
		{
			if (appearType == AppearType.Manual || appearType == AppearType.OnInputKey || appearType == AC.AppearType.OnInteraction || appearType == AppearType.OnContainer)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>If True, the Menu is currently clickable.</summary>
		 * <returns>True if the Menu is currently clickable</returns>
		 */
		public bool IsClickable ()
		{
			if (ignoreMouseClicks)
			{
				return false;
			}

			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				if (canClickInCutscene && ShowClickInCutscenesOption ())
				{
					return true;
				}
				return false;
			}

			return true;
		}


		/**
		 * <summary>If True, the Menu is clickable during Cutscenes.</summary>
		 * <returns>True if the Menu is clickable during Cutscenes.</returns>
		 */
		public bool CanClickInCutscenes ()
		{
			if (ShowClickInCutscenesOption () && !ignoreMouseClicks && canClickInCutscene)
			{
				return true;
			}
			return false;
		}


		private bool ShowClickInCutscenesOption ()
		{
			if (appearType == AppearType.WhenSpeechPlays || appearType == AppearType.DuringConversation || 
			    appearType == AppearType.Manual || appearType == AppearType.WhenSpeechPlays ||
			    appearType == AppearType.DuringCutscene)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if the Menu will pause gameplay when enabled.</summary>
		 * <returns>True if the Menu will pause gameplay when enabled.</returns>
		 */
		public bool IsBlocking ()
		{
			if (pauseWhenEnabled && CanPause ())
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the Menu's enabled state is controlled by either the player or by Actions.</summary>
		 * <returns>True if the Menu's enabled state is controlled by either the player or by Actions.</returns>
		 */
		public bool IsManualControlled ()
		{
			if (appearType == AppearType.Manual || appearType == AppearType.OnInputKey || appearType == AppearType.OnContainer)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Recalculates a Menu's display for a particular set of Hotspot Buttons.</summary>
		 * <param name = "buttons">A List of Button classes to recalculate the Menus's display for</param>
		 * <param name = "includeInventory">If True, then InventoryBox elements will also be displayed when appropriate</param>
		 */
		public void MatchInteractions (List<Button> buttons, bool includeInventory)
		{
			forHotspot = KickStarter.playerInteraction.GetActiveHotspot ();
			forItem = null;

			foreach (MenuElement element in elements)
			{
				if (element is MenuInteraction)
				{
					MenuInteraction interaction = (MenuInteraction) element;
					interaction.MatchInteractions (buttons);
				}
				else if (element is MenuInventoryBox)
				{
					if (includeInventory)
					{
						element.RecalculateSize (menuSource);
						Recalculate ();
						element.AutoSetVisibility ();
					}
					else
					{
						element.isVisible = false;
					}
				}
			}
			
			Recalculate ();
			Recalculate ();
		}
		

		/**
		 * <summary>Recalculates a Menu's display for a particular inventory item.</summary>
		 * <param name = "buttons">The InvItem to recalculate the Menus's display for</param>
		 * <param name = "includeInventory">If True, then InventoryBox elements will also be displayed when appropriate</param>
		 */
		public void MatchInteractions (InvItem item, bool includeInventory)
		{
			forHotspot = null;
			forItem = item;

			foreach (MenuElement element in elements)
			{
				if (element is MenuInteraction)
				{
					MenuInteraction interaction = (MenuInteraction) element;
					interaction.MatchInteractions (item);
				}
				else if (element is MenuInventoryBox)
				{
					if (includeInventory)
					{
						element.RecalculateSize (menuSource);
						Recalculate ();
						element.AutoSetVisibility ();
					}
					else
					{
						element.isVisible = false;
					}
				}
			}
			
			Recalculate ();
			Recalculate ();
		}
		

		/**
		 * <summary>Recalculates a Menu's display for an "Examine" Hotspot Button.</summary>
		 * <param name = "button">A Button class to recalculate the Menus's display for</param>
		 */
		public void MatchLookInteraction (Button button)
		{
			foreach (MenuElement element in elements)
			{
				if (element is MenuInteraction)
				{
					MenuInteraction interaction = (MenuInteraction) element;
					interaction.MatchLookInteraction (KickStarter.cursorManager.lookCursor_ID);
				}
			}
		}
		

		/**
		 * <summary>Recalculates a Menu's display for an "Use" Hotspot Button.</summary>
		 * <param name = "button">A Button class to recalculate the Menus's display for</param>
		 */
		public void MatchUseInteraction (Button button)
		{
			foreach (MenuElement element in elements)
			{
				if (element is MenuInteraction)
				{
					MenuInteraction interaction = (MenuInteraction) element;
					interaction.MatchUseInteraction (button);
				}
			}
		}


		/**
		 * Hides all MenuInteraction elements within the Menu.
		 */
		public void HideInteractions ()
		{
			foreach (MenuElement element in elements)
			{
				if (element is MenuInteraction)
				{
					element.isVisible = false;
					element.isClickable = false; // This function is only called for Context-Sensitive anyway
				}
			}
		}
		

		/**
		 * <summary>Offsets an OnGUI Menu's position when dragged by a MenuDrag element.</summary>
		 * <param name = "pos">The amoung to offset the position by</param>
		 * <param name = "dragRect">The boundary limit to keep the Menu within</param>
		 */
		public void SetDragOffset (Vector2 pos, Rect dragRect)
		{
			if (pos.x < dragRect.x)
			{
				pos.x = dragRect.x;
			}
			else if (pos.x > (dragRect.x + dragRect.width - GetRect ().width))
			{
				pos.x = dragRect.x + dragRect.width - GetRect ().width;
			}
			
			if (pos.y < dragRect.y)
			{
				pos.y = dragRect.y;
			}
			else if (pos.y > (dragRect.y + dragRect.height - GetRect ().height))
			{
				pos.y = dragRect.y + dragRect.height - GetRect ().height;
			}
			
			dragOffset = pos;
			
			UpdateAspectRect ();
		}
		

		/**
		 * <summary>Gets the drag offset.</summary>
		 * <returns>The drag offset</returns>
		 */
		public Vector2 GetDragStart ()
		{
			return dragOffset;
		}
		

		/**
		 * Gets the zoom factor of MenuElements when a Menu is zooming
		 */
		public float GetZoom ()
		{
			if (!IsUnityUI () && transitionType == MenuTransition.Zoom && zoomElements)
			{
				return zoomAmount;
			}
			return 1f;
		}
		

		/**
		 * <summary>Sets the "active" slot/element within a keyboard-controlled Menu.</summary>
		 * <param name = "selected_option">The intended slot/element to select</param>
		 * <returns>The newly-selected slot/element</returns>
		 */
		public int ControlSelected (int selected_option)
		{
			if (selected_slot == -2)
			{
				selected_option = 0;
			}
			
			if (selected_option < 0)
			{
				selected_option = 0;
				selected_element = visibleElements[0];
				selected_slot = 0;
			}
			else
			{
				int sel = 0;
				selected_slot = -1;
				int element = 0;
				int slot = 0;
				
				for (element=0; element<visibleElements.Count; element++)
				{
					if (visibleElements[element].isClickable)
					{
						for (slot=0; slot<visibleElements[element].GetNumSlots (); slot++)
						{
							if (selected_option == sel)
							{
								selected_slot = slot;
								selected_element = visibleElements[element];
								break;
							}
							sel++;
						}
					}
					
					if (selected_slot != -1)
					{
						break;
					}
				}
				
				if (selected_slot == -1)
				{
					// Couldn't find match, must've maxed out
					selected_slot = slot - 1;
					selected_option = sel - 1;
					if (visibleElements.Count < (element-1))
					{
						selected_element = visibleElements[element-1];
					}
				}
			}
			
			return selected_option;
		}
		

		/**
		 * <summary>Gets a MenuElement subclass within the Menu's list of elements.</summary>
		 * <param name = "menuElementName">The title of the MenuElement to get</param>
		 * <returns>The MenuElement subclass</returns>
		 */
		public MenuElement GetElementWithName (string menuElementName)
		{
			foreach (MenuElement menuElement in elements)
			{
				if (menuElement.title == menuElementName)
				{
					return menuElement;
				}
			}
			
			return null;
		}
		

		/**
		 * <summary>Gets the centre-point of a MenuElement slot, in Screen Space.</summary>
		 * <param name = "_element">The MenuElement that the slot is in</param>
		 * <param name = "slot">The slot to reference, by index number</param>
		 * <returns>The centre-point of the MenuElement slot</returns>
		 */
		public Vector2 GetSlotCentre (MenuElement _element, int slot)
		{
			foreach (MenuElement menuElement in elements)
			{
				if (menuElement == _element)
				{
					if (IsUnityUI ())
					{
						Vector3 _position = menuElement.GetRectTransform (slot).position;
						if (canvas.renderMode != RenderMode.WorldSpace)
						{
							return new Vector2 (_position.x, Screen.height - _position.y);
						}
						return Camera.main.WorldToScreenPoint (_position);
					}

					Rect slotRect = _element.GetSlotRectRelative (slot);
					return new Vector2 (GetRect ().x + slotRect.x + (slotRect.width / 2f), GetRect ().y + slotRect.y + (slotRect.height / 2f));
				}
			}
			
			return Vector2.zero;
		}


		private void ClearSpeechText ()
		{
			foreach (MenuElement element in elements)
			{
				element.ClearSpeech ();
			}
		}


		/**
		 * <summary>Assigns the Menu, and all MenuElement classes within it, to a Hotspot.</summary>
		 * <param name = "_speech">The Speech line to assign to</param>
		 */
		public void SetHotspot (Hotspot _hotspot, InvItem _invItem)
		{
			forHotspot = _hotspot;
			forItem = _invItem;
			foreach (MenuElement element in elements)
			{
				element.SetHotspot (_hotspot, _invItem);
			}
		}


		/**
		 * <summary>Assigns the Menu, and all MenuElement classes within it, to a Speech line.</summary>
		 * <param name = "_speech">The Speech line to assign to</param>
		 */
		public void SetSpeech (Speech _speech)
		{
			speech = _speech;
			foreach (MenuElement element in elements)
			{
				element.SetSpeech (_speech);
			}
		}


		/**
		 * <summary>Gets the GameObject of the first-selected MenuElement, for a Unity UI-based Menu.</summary>
		 * <returns>The GameObject of the first-selected MenuElement</returns>
		 */
		public GameObject GetObjectToSelect ()
		{
			if (firstSelectedElement == "")
			{
				return null;
			}

			foreach (MenuElement element in visibleElements)
			{
				if (element.title == firstSelectedElement)
				{
					return element.GetObjectToSelect ();
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the inventory item that an interaction Menu was recalculated for.</summary>
		 * <returns>The InvItem that an interaction Menu was recalculated for</returns>
		 */
		public InvItem GetTargetInvItem ()
		{
			return forItem;
		}


		/**
		 * <summary>Gets the Hotspot that an interaction Menu was recalculated for.</summary>
		 * <returns>The Hotspot that an interaction Menu was recalculated for</returns>
		 */
		public Hotspot GetTargetHotspot ()
		{
			return forHotspot;
		}


		/**
		 * <summary>Prepares the Menu for a screenshot by disabling the canvas if it has one.</summary>
		 */
		public void PreScreenshotBackup ()
		{
			if (menuSource != MenuSource.AdventureCreator && canvas != null)
			{
				isDisabledForScreenshot = canvas.gameObject.activeSelf;
				if (isDisabledForScreenshot)
				{
					canvas.gameObject.SetActive (false);
				}
			}
		}


		/**
		 * <summary>Re-enables the Menu's canvas if it was disabled to take a screenshot.</summary>
		 */
		public void PostScreenshotBackup ()
		{
			if (menuSource != MenuSource.AdventureCreator && canvas != null)
			{
				if (isDisabledForScreenshot)
				{
					canvas.gameObject.SetActive (true);
				}
			}
		}

	}
	
}
