/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"PlayerInput.cs"
 * 
 *	This script records all input and processes it for other scripts.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * This script recieves and processes all input, for use by other scripts.
	 * It should be placed on the GameEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_input.html")]
	#endif
	public class PlayerInput : MonoBehaviour
	{

		private AnimationCurve timeCurve;
		private float changeTimeStart;

		private MouseState mouseState = MouseState.Normal;
		private DragState dragState = DragState.None;

		private Vector2 moveKeys = new Vector2 (0f, 0f);
		private bool playerIsControlledRunning = false;
		/** The game's current Time.timeScale value */
		[HideInInspector] public float timeScale = 1f;
		
		private bool isUpLocked = false;
		private bool isDownLocked = false;
		private bool isLeftLocked = false;
		private bool isRightLocked = false;
		private bool freeAimLock = false;

		/** If True, Menus can be controlled via the keyboard or controller during gameplay (if SettingsManager.inputMethod = InputMethod.KeyboardOrController */
		[HideInInspector] public bool canKeyboardControlMenusDuringGameplay = false;
		/** If True, then the Player prefab cannot run */
		[HideInInspector] public PlayerMoveLock runLock = PlayerMoveLock.Free;
		/** The name of the Input button that skips movies played with ActionMove */
		[HideInInspector] public string skipMovieKey = "";
		/** The minimum duration, in seconds, that can elapse between mouse clicks */
		public float clickDelay = 0.3f;
		/**<The maximum duration, in seconds, between two successive mouse clicks to register a "double-click" */
		public float doubleClickDelay = 1f;

		private int selected_option;
		private float clickTime = 0f;
		private float doubleClickTime = 0;
		private MenuDrag activeDragElement;
		private bool hasUnclickedSinceClick = false;
		private bool lastClickWasDouble = false;
		private float lastclickTime = 0f;
		
		// Menu input override
		private string menuButtonInput;
		private float menuButtonValue;
		private SimulateInputType menuInput;
		
		// Controller movement
		/** The movement speed of a keyboard or controller-controlled cursor */
		public float cursorMoveSpeed = 4f;
		/** If True, and Direct movement is used to control the Player, then the Player will not change direction. This is to avoid the Player moving in unwanted directions when the camera cuts. */
		[HideInInspector] public bool cameraLockSnap = false;
		private Vector2 xboxCursor;
		private Vector2 mousePosition;
		private bool scrollingLocked = false;
		
		// Touch-Screen movement
		private Vector2 dragStartPosition = Vector2.zero;
		private float dragSpeed = 0f;
		private Vector2 dragVector;
		private float touchTime = 0f;
		private float touchThreshold = 0.2f;
		
		// 1st person movement
		private Vector2 freeAim;
		/** If True, the mouse cursor will be locked in the centre of the screen, allowing for free-aiming if in First-Person */
		[HideInInspector] public bool cursorIsLocked = false;
		private bool toggleRun = false;
		
		// Draggable
		private bool canDragMoveable = false;
		private float cameraInfluence = 100000f;
		private DragBase dragObject = null;
		private Vector2 lastMousePosition;
		private Vector3 lastCameraPosition;
		private Vector3 dragForce;
		private Vector2 deltaDragMouse;

		/** The active Conversation */
		[HideInInspector] public Conversation activeConversation = null;
		/** The active ArrowPrompt */
		[HideInInspector] public ArrowPrompt activeArrows = null;
		/** The active Container */
		[HideInInspector] public Container activeContainer = null;
		private bool mouseIsOnScreen = true;

		// Delegates
		/** A delegate template for overriding input button detection */
		public delegate bool InputButtonDelegate (string buttonName);
		/** A delegate template for overriding input axis detection */
		public delegate float InputAxisDelegate (string axisName);
		/** A delegate template for overriding mouse position detection */
		public delegate Vector2 InputMouseDelegate (bool cusorIsLocked = false);
		/** A delegate template for overriding mouse button detection */
		public delegate bool InputMouseButtonDelegate (int button);

		/** A delegate for the InputGetButtonDown function, used to detect when a button is first pressed */
		public InputButtonDelegate InputGetButtonDownDelegate = null;
		/** A delegate for the InputGetButtonUp function, used to detect when a button is released */
		public InputButtonDelegate InputGetButtonUpDelegate = null;
		/** A delegate for the InputGetButton function, used to detect when a button is held down */
		public InputButtonDelegate InputGetButtonDelegate = null;
		/** A delegate for the InputGetAxis function, used to detect the value of an input axis */
		public InputAxisDelegate InputGetAxisDelegate = null;
		/** A delagate for the InputGetMouseButton function, used to detect mouse clicks */
		public InputMouseButtonDelegate InputGetMouseButtonDelegate;
		/** A delagate for the InputGetMouseDownButton function, used to detect when a mouse button is first clicked */
		public InputMouseButtonDelegate InputGetMouseButtonDownDelegate;
		/** A delagate for the InputMousePosition function, used to detect the mouse position */
		public InputMouseDelegate InputMousePositionDelegate;
		/** A delegate for the InputGetFreeAim function, used to get the free-aiming vector */
		public InputMouseDelegate InputGetFreeAimDelegate;


		public void OnAwake ()
		{
			if (KickStarter.settingsManager)
			{
				cursorIsLocked = KickStarter.settingsManager.lockCursorOnStart;
				if (cursorIsLocked && !KickStarter.settingsManager.IsInFirstPerson ())
				{
					ACDebug.Log ("Starting a non-First Person game with a locked cursor - is this correct?"); 
				}
			}
		
			ResetClick ();
			
			xboxCursor.x = Screen.width / 2;
			xboxCursor.y = Screen.height / 2;

			if (KickStarter.settingsManager.CanDragCursor ())
			{
				mousePosition = xboxCursor;
			}
		}


		/**
		 * Updates the input handler.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateInput ()
		{
			if (timeCurve != null && timeCurve.length > 0)
			{
				float timeIndex = Time.time - changeTimeStart;
				if (timeCurve [timeCurve.length -1].time < timeIndex)
				{
					SetTimeScale (timeCurve [timeCurve.length -1].time);
					timeCurve = null;
				}
				else
				{
					SetTimeScale (timeCurve.Evaluate (timeIndex));
				}
			}

			if (clickTime > 0f)
			{
				clickTime -= 4f * GetDeltaTime ();
			}
			if (clickTime < 0f)
			{
				clickTime = 0f;
			}
			
			if (doubleClickTime > 0f)
			{
				doubleClickTime -= 4f * GetDeltaTime ();
			}
			if (doubleClickTime < 0f)
			{
				doubleClickTime = 0f;
			}

			if (skipMovieKey != "" && InputGetButtonDown (skipMovieKey))
			{
				skipMovieKey = "";
			}
			
			if (KickStarter.stateHandler && KickStarter.settingsManager)
			{
				if (InputGetButtonDown ("ToggleCursor") && KickStarter.stateHandler.gameState == GameState.Normal)
				{
					ToggleCursor ();
				}

				if (KickStarter.stateHandler.gameState == GameState.Cutscene && InputGetButtonDown ("EndCutscene"))
				{
					KickStarter.actionListManager.EndCutscene ();
				}

				#if UNITY_EDITOR
				if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard || KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
				#else
				if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard)
				#endif
				{
					// Cursor position
					bool shouldLockCursor = UnityVersionHandler.CursorLock;

					if (!cursorIsLocked || KickStarter.stateHandler.gameState == AC.GameState.Paused || KickStarter.stateHandler.gameState == AC.GameState.DialogOptions || (freeAimLock && KickStarter.settingsManager.IsInFirstPerson ()))
					{
						shouldLockCursor = false;
						mousePosition = InputMousePosition (false);
						freeAim = InputGetFreeAim (false);
					}
					else if (dragObject != null && KickStarter.settingsManager.IsInFirstPerson () && KickStarter.settingsManager.disableFreeAimWhenDragging)
					{
						shouldLockCursor = false;
						mousePosition = InputMousePosition (false);
						freeAim = InputGetFreeAim (false);
					}
					else if (cursorIsLocked && KickStarter.stateHandler.gameState == GameState.Normal)
					{
						if (!shouldLockCursor && dragObject == null && KickStarter.settingsManager.IsInFirstPerson ())
						{
							shouldLockCursor = true;
						}
						mousePosition = InputMousePosition (true);
						freeAim = InputGetFreeAim (true);
					}

					UnityVersionHandler.CursorLock = shouldLockCursor;

					// Cursor state
					if (mouseState == MouseState.Normal)
					{
						dragState = DragState.None;
					}

					if (InputGetMouseButtonDown (0) || InputGetButtonDown ("InteractionA"))
					{
						if (KickStarter.settingsManager.touchUpWhenPaused && KickStarter.stateHandler.gameState == GameState.Paused)
						{
							ResetMouseClick ();
						}
						else if (mouseState == MouseState.Normal)
						{
							if (CanDoubleClick ())
							{
								mouseState = MouseState.DoubleClick;
								ResetClick ();
							}
							else if (CanClick ())
							{
								dragStartPosition = GetInvertedMouse ();
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
						}
					}
					else if (InputGetMouseButtonDown (1) || InputGetButtonDown ("InteractionB"))
					{
						mouseState = MouseState.RightClick;
					}
					else if (InputGetMouseButton (0) || InputGetButton ("InteractionA"))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else
					{
						if (mouseState == MouseState.HeldDown && dragState == DragState.None && CanClick ())
						{
							if (KickStarter.settingsManager.touchUpWhenPaused && KickStarter.stateHandler.gameState == GameState.Paused)
							{
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
							else
							{
								mouseState = MouseState.LetGo;
							}
						}
						else
						{
							ResetMouseClick ();
						}
					}

					SetDoubleClickState ();
					
					if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
					{
						if (dragState == DragState.Player)
						{
							if (KickStarter.settingsManager.IsFirstPersonDragMovement ())
							{
								freeAim = new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, 0f);
							}
							else
							{
								freeAim = new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, -dragVector.y * KickStarter.settingsManager.freeAimTouchSpeed);
							}
						}
						else
						{
							freeAim = Vector2.zero;
						}
					}
				}
				else if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
				{
					int touchCount = Input.touchCount;
					
					// Cursor position
					if (cursorIsLocked)
					{
						mousePosition = new Vector2 (Screen.width / 2f, Screen.height / 2f);
					}
					else if (touchCount > 0)
					{
						if (KickStarter.settingsManager.CanDragCursor ())
						{
							if (touchTime > touchThreshold)
							{
								Touch t = Input.GetTouch (0);
								if (t.phase == TouchPhase.Moved && touchCount == 1)
								{
									if (KickStarter.stateHandler.gameState == GameState.Paused)
									{
										mousePosition += t.deltaPosition * 1.7f;
									}
									else
									{
										mousePosition += t.deltaPosition * Time.deltaTime / t.deltaTime;
									}
									
									if (mousePosition.x < 0f)
									{
										mousePosition.x = 0f;
									}
									else if (mousePosition.x > Screen.width)
									{
										mousePosition.x = Screen.width;
									}
									if (mousePosition.y < 0f)
									{
										mousePosition.y = 0f;
									}
									else if (mousePosition.y > Screen.height)
									{
										mousePosition.y = Screen.height;
									}
								}
							}
						}
						else
						{
							mousePosition = Input.GetTouch (0).position;
						}
					}
					
					// Cursor state
					if (mouseState == MouseState.Normal)
					{
						dragState = DragState.None;
					}
					
					if (touchTime > 0f && touchTime < touchThreshold)
						dragStartPosition = GetInvertedMouse ();

					if ((touchCount == 1 && KickStarter.stateHandler.gameState == GameState.Cutscene && Input.GetTouch (0).phase == TouchPhase.Began)
					    || (touchCount == 1 && !KickStarter.settingsManager.CanDragCursor () && Input.GetTouch (0).phase == TouchPhase.Began)
					    || touchTime == -1f)
					{
						if (KickStarter.settingsManager.touchUpWhenPaused && KickStarter.stateHandler.gameState == GameState.Paused)
						{
							ResetMouseClick ();
						}
						else if (mouseState == MouseState.Normal)
						{
							dragStartPosition = GetInvertedMouse (); //

							if (CanDoubleClick ())
							{
								mouseState = MouseState.DoubleClick;
								ResetClick ();
							}
							else if (CanClick ())
							{
								dragStartPosition = GetInvertedMouse ();
								
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
						}
					}
					else if (touchCount == 2 && Input.GetTouch (1).phase == TouchPhase.Began)
					{
						mouseState = MouseState.RightClick;

						if (KickStarter.settingsManager.IsFirstPersonDragComplex ())
						{
							dragStartPosition = GetInvertedMouse ();
						}
					}
					else if (touchCount == 1 && (Input.GetTouch (0).phase == TouchPhase.Stationary || Input.GetTouch (0).phase == TouchPhase.Moved))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else if (touchCount == 2 && (Input.GetTouch (0).phase == TouchPhase.Stationary || Input.GetTouch (0).phase == TouchPhase.Moved) && KickStarter.settingsManager.IsFirstPersonDragComplex ())
					{
						mouseState = MouseState.HeldDown;
						SetDragStateTouchScreen ();
					}
					else
					{
						if (mouseState == MouseState.HeldDown && dragState == DragState.None && CanClick ())
						{
							if (KickStarter.settingsManager.touchUpWhenPaused && KickStarter.stateHandler.gameState == GameState.Paused)
							{
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
							else
							{
								mouseState = MouseState.LetGo;
							}
						}
						else
						{
							ResetMouseClick ();
						}
					}

					SetDoubleClickState ();
					
					if (KickStarter.settingsManager.CanDragCursor ())
					{
						if (touchCount > 0)
						{
							touchTime += GetDeltaTime ();
						}
						else
						{
							if (touchTime > 0f && touchTime < touchThreshold)
							{
								touchTime = -1f;
							}
							else
							{
								touchTime = 0f;
							}
						}
					}
					
					if (dragState == DragState.Player)
					{
						if (KickStarter.settingsManager.IsFirstPersonDragMovement ())
						{
							freeAim = new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, 0f);
						}
						else
						{
							freeAim = new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, -dragVector.y * KickStarter.settingsManager.freeAimTouchSpeed);
						}
					}
					else
					{
						freeAim = Vector2.zero; //
					}
				}
				else if (KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController)
				{

					// Cursor position
					if (cursorIsLocked && KickStarter.stateHandler.gameState == GameState.Normal)
					{
						mousePosition = new Vector2 (Screen.width / 2f, Screen.height / 2f);
						freeAim = new Vector2 (InputGetAxis ("CursorHorizontal") * 50f, InputGetAxis ("CursorVertical") * 50f);
					}
					else
					{
						xboxCursor.x += InputGetAxis ("CursorHorizontal") * cursorMoveSpeed / Screen.width * 5000f;
						xboxCursor.y += InputGetAxis ("CursorVertical") * cursorMoveSpeed / Screen.height * 5000f;

						xboxCursor.x = Mathf.Clamp (xboxCursor.x, 0f, Screen.width);
						xboxCursor.y = Mathf.Clamp (xboxCursor.y, 0f, Screen.height);
						
						mousePosition = xboxCursor;
						freeAim = Vector2.zero;
					}
					
					// Cursor state
					if (mouseState == MouseState.Normal)
					{
						dragState = DragState.None;
					}
					
					if (InputGetButtonDown ("InteractionA"))
					{
						if (mouseState == MouseState.Normal)
						{
							if (CanDoubleClick ())
							{
								mouseState = MouseState.DoubleClick;
								ResetClick ();
							}
							else if (CanClick ())
							{
								dragStartPosition = GetInvertedMouse ();
								
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
						}
					}
					else if (InputGetButtonDown ("InteractionB"))
					{
						mouseState = MouseState.RightClick;
					}
					else if (InputGetButton ("InteractionA"))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else
					{
						ResetMouseClick ();
					}

					SetDoubleClickState ();

					// Menu option changing
					if (!KickStarter.playerMenus.IsCyclingInteractionMenu ())
					{
						if (KickStarter.stateHandler.gameState == GameState.DialogOptions ||
							KickStarter.stateHandler.gameState == GameState.Paused ||
						   (KickStarter.stateHandler.gameState == GameState.Normal && canKeyboardControlMenusDuringGameplay))
						{
							if (!scrollingLocked)
							{
								if (InputGetAxisRaw ("Vertical") > 0.1 || InputGetAxisRaw ("Horizontal") < -0.1)
								{
									// Up / Left
									scrollingLocked = true;
									selected_option --;
								}
								else if (InputGetAxisRaw ("Vertical") < -0.1 || InputGetAxisRaw ("Horizontal") > 0.1)
								{
									// Down / Right
									scrollingLocked = true;
									selected_option ++;
								}
							}
							else if (InputGetAxisRaw ("Vertical") < 0.05 && InputGetAxisRaw ("Vertical") > -0.05 && InputGetAxisRaw ("Horizontal") < 0.05 && InputGetAxisRaw ("Horizontal") > -0.05)
							{
								scrollingLocked = false;
							}
						}
					}
				}
				
				if (KickStarter.playerInteraction.GetHotspotMovingTo () != null)
				{
					freeAim = Vector2.zero;
				}

				if (KickStarter.stateHandler.gameState == GameState.Normal)
				{
					DetectCursorInputs ();
				}

				if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && KickStarter.playerMenus.IsInteractionMenuOn ())
				{
					if (InputGetButtonDown ("CycleInteractionsRight"))
					{
						KickStarter.playerInteraction.SetNextInteraction ();
					}
					else if (InputGetButtonDown ("CycleInteractionsLeft"))
					{
						KickStarter.playerInteraction.SetPreviousInteraction ();
					}
					else if (InputGetAxis ("CycleInteractions") > 0.1f)
					{
						KickStarter.playerInteraction.SetNextInteraction ();
					}
					else if (InputGetAxis ("CycleInteractions") < -0.1f)
					{
						KickStarter.playerInteraction.SetPreviousInteraction ();
					}
				}
				
				mousePosition = KickStarter.mainCamera.LimitToAspect (mousePosition);

				if (mouseState == MouseState.Normal && !hasUnclickedSinceClick)
				{
					hasUnclickedSinceClick = true;
				}
				
				if (mouseState == MouseState.Normal)
				{
					canDragMoveable = true;
				}
				
				UpdateDrag ();
				
				if (dragState != DragState.None)
				{
					dragVector = GetInvertedMouse () - dragStartPosition;
					dragSpeed = dragVector.magnitude;
				}
				else
				{
					dragVector = Vector2.zero;
					dragSpeed = 0f;
				}

				UpdateActiveInputs ();

				if (mousePosition.x < 0f || mousePosition.x > Screen.width || mousePosition.y < 0f || mousePosition.y > Screen.height)
				{
					mouseIsOnScreen = false;
				}
				else
				{
					mouseIsOnScreen = true;
				}
			}
		}


		private void SetDoubleClickState ()
		{
			if (mouseState == MouseState.DoubleClick)
			{
				lastClickWasDouble = true;
			}
			else if (mouseState == MouseState.SingleClick || mouseState == MouseState.RightClick || mouseState == MouseState.LetGo)
			{
				lastClickWasDouble = false;
			}

			if (mouseState == MouseState.DoubleClick || mouseState == MouseState.RightClick || mouseState == MouseState.SingleClick)
			{
				lastclickTime = clickDelay;
			}
			else if (lastclickTime > 0f)
			{
				lastclickTime -= Time.deltaTime;
			}
		}


		/**
		 * <summary>Checks if the player clicked within the last few frames. This is useful when checking for input in Actions, because Actions do not run every frame.</summary>
		 * <param name = "checkForDouble">If True, then the check will be made for a double-click, rather than a single-click.</param>
		 * <returns>True if the player recently clicked.</returns>
		 */
		public bool ClickedRecently (bool checkForDouble = false)
		{
			if (lastclickTime > 0f)
			{
				if (checkForDouble == lastClickWasDouble)
				{
					return true;
				}
			}
			return false;
		}


		private void UpdateActiveInputs ()
		{
			if (KickStarter.settingsManager.activeInputs != null)
			{
				foreach (ActiveInput activeInput in KickStarter.settingsManager.activeInputs)
				{
					if (InputGetButtonDown (activeInput.inputName))
					{
						if (KickStarter.stateHandler.gameState == activeInput.gameState && activeInput.actionListAsset != null && !KickStarter.actionListAssetManager.IsListRunning (activeInput.actionListAsset))
						{
							AdvGame.RunActionListAsset (activeInput.actionListAsset);
						}
					}
				}
			}
		}


		private void DetectCursorInputs ()
		{
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
			{
				if (KickStarter.cursorManager.allowWalkCursor)
				{
					if (InputGetButtonDown ("Icon_Walk"))
					{
						KickStarter.runtimeInventory.SetNull ();
						KickStarter.playerCursor.ResetSelectedCursor ();
						return;
					}
				}

				foreach (CursorIcon icon in KickStarter.cursorManager.cursorIcons)
				{
					if (InputGetButtonDown (icon.GetButtonName ()))
					{
						KickStarter.runtimeInventory.SetNull ();
						KickStarter.playerCursor.SetCursor (icon);
						return;
					}
				}
			}
		}


		/**
		 * <summary>Gets the cursor's position in screen space.</summary>
		 * <returns>The cursor's position in screen space</returns>
		 */
		public Vector2 GetMousePosition ()
		{
			return mousePosition;
		}


		/**
		 * <summary>Gets the y-inverted cursor position. This is useful because Menu Rects are drawn upwards, while screen space is measured downwards.</summary>
		 * <returns>Gets the y-inverted cursor position. This is useful because Menu Rects are drawn upwards, while screen space is measured downwards.</returns>
		 */
		public Vector2 GetInvertedMouse ()
		{
			return new Vector2 (GetMousePosition ().x, Screen.height - GetMousePosition ().y);
		}


		/**
		 * <summary>Initialises the cursor lock based on a given movement method.</summary>
		 * <param name = "movementMethod">The new movement method</param>
		 */
		public void InitialiseCursorLock (MovementMethod movementMethod)
		{
			if (KickStarter.settingsManager.IsInFirstPerson () && movementMethod != MovementMethod.FirstPerson)
			{
				cursorIsLocked = false;
			}
			else if (!KickStarter.settingsManager.IsInFirstPerson () && movementMethod == MovementMethod.FirstPerson)
			{
				cursorIsLocked = KickStarter.settingsManager.lockCursorOnStart;
			}
		}


		/**
		 * <summary>Checks if the cursor's position can be read. This is only ever False if the cursor cannot be dragged on a touch-screen.</summary>
		 * <returns>True if the cursor's position can be read</returns>
		 */
		public bool IsCursorReadable ()
		{
			if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				if (mouseState == MouseState.Normal)
				{
					if (KickStarter.runtimeInventory.selectedItem != null && KickStarter.settingsManager.inventoryDragDrop)
					{
						return true;
					}
					
					return KickStarter.settingsManager.CanDragCursor ();
				}
			}
			return true;
		}


		/**
		 * Detects the pressing of the numeric keys if they can be used to trigger a Conversation's dialogue options.
		 */
		public void DetectConversationNumerics ()
		{		
			if (activeConversation != null && KickStarter.settingsManager.runConversationsWithKeys)
			{
				Event e = Event.current;
				if (e.isKey && e.type == EventType.KeyDown)
				{
					if (e.keyCode == KeyCode.Alpha1 || e.keyCode == KeyCode.Keypad1)
					{
						activeConversation.RunOption (0);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha2 || e.keyCode == KeyCode.Keypad2)
					{
						activeConversation.RunOption (1);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha3 || e.keyCode == KeyCode.Keypad3)
					{
						activeConversation.RunOption (2);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha4 || e.keyCode == KeyCode.Keypad4)
					{
						activeConversation.RunOption (3);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha5 || e.keyCode == KeyCode.Keypad5)
					{
						activeConversation.RunOption (4);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha6 || e.keyCode == KeyCode.Keypad6)
					{
						activeConversation.RunOption (5);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha7 || e.keyCode == KeyCode.Keypad7)
					{
						activeConversation.RunOption (6);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha8 || e.keyCode == KeyCode.Keypad8)
					{
						activeConversation.RunOption (7);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha9 || e.keyCode == KeyCode.Keypad9)
					{
						activeConversation.RunOption (8);
						return;
					}
				}
			}
		}


		/**
		 * Detects the pressing of the defined input buttons if they can be used to trigger a Conversation's dialogue options.
		 */
		public void DetectConversationInputs ()
		{		
			if (activeConversation != null && KickStarter.settingsManager.runConversationsWithKeys)
			{

				if (InputGetButtonDown ("DialogueOption1"))
				{
					activeConversation.RunOption (0);
				}
				else if (InputGetButtonDown ("DialogueOption2"))
				{
					activeConversation.RunOption (1);
				}
				else if (InputGetButtonDown ("DialogueOption3"))
				{
					activeConversation.RunOption (2);
				}
				else if (InputGetButtonDown ("DialogueOption4"))
				{
					activeConversation.RunOption (3);
				}
				else if (InputGetButtonDown ("DialogueOption5"))
				{
					activeConversation.RunOption (4);
				}
				else if (InputGetButtonDown ("DialogueOption6"))
				{
					activeConversation.RunOption (5);
				}
				else if (InputGetButtonDown ("DialogueOption7"))
				{
					activeConversation.RunOption (6);
				}
				else if (InputGetButtonDown ("DialogueOption8"))
				{
					activeConversation.RunOption (7);
				}
				else if (InputGetButtonDown ("DialogueOption9"))
				{
					activeConversation.RunOption (8);
				}
			}
			
		}
		
		
		/**
		 * Draws a drag-line on screen if the chosen movement method allows for one.
		 */
		public void DrawDragLine ()
		{
			if (dragState == DragState.Player && KickStarter.settingsManager.movementMethod != MovementMethod.StraightToCursor && KickStarter.settingsManager.drawDragLine)
			{
				Vector2 pointA = dragStartPosition;
				Vector2 pointB = GetInvertedMouse ();
				
				if (pointB.x >= 0f)
				{
					DrawStraightLine.Draw (pointA, pointB, KickStarter.settingsManager.dragLineColor, KickStarter.settingsManager.dragLineWidth, true);
				}
			}
			
			if (activeDragElement != null)
			{
				if (mouseState == MouseState.HeldDown)
				{
					if (!activeDragElement.DoDrag (GetDragVector ()))
					{
						activeDragElement = null;
					}
				}
				else if (mouseState == MouseState.Normal)
				{
					if (activeDragElement.CheckStop (GetInvertedMouse ()))
					{
						activeDragElement = null;
					}
				}
			}
		}
		

		/**
		 * Updates the input variables needed for Direct movement.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateDirectInput ()
		{
			if (KickStarter.settingsManager != null)
			{
				if (activeArrows != null)
				{
					if (activeArrows.arrowPromptType == ArrowPromptType.KeyOnly || activeArrows.arrowPromptType == ArrowPromptType.KeyAndClick)
					{
						Vector2 normalizedVector = new Vector2 (InputGetAxis ("Horizontal"), InputGetAxis ("Vertical"));

						if (normalizedVector.sqrMagnitude > 0f)
						{
							if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && dragState == DragState.ScreenArrows)
							{
								normalizedVector = GetDragVector () / KickStarter.settingsManager.dragRunThreshold / KickStarter.settingsManager.dragWalkThreshold;
							}

							float threshold = 0.95f;
							if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard)
							{
								threshold = 0.05f;
							}

							if (normalizedVector.x > threshold)
							{
								activeArrows.DoRight ();
							}
							else if (normalizedVector.x < -threshold)
							{
								activeArrows.DoLeft ();
							}
							else if (normalizedVector.y < -threshold)
							{
								activeArrows.DoDown();
							}
							else if (normalizedVector.y > threshold)
							{
								activeArrows.DoUp ();
							}
						}
					}
					
					if (activeArrows != null && (activeArrows.arrowPromptType == ArrowPromptType.ClickOnly || activeArrows.arrowPromptType == ArrowPromptType.KeyAndClick))
					{
						// Arrow Prompt is displayed: respond to mouse clicks
						Vector2 invertedMouse = GetInvertedMouse ();
						if (mouseState == MouseState.SingleClick)
						{
							if (activeArrows.upArrow.rect.Contains (invertedMouse))
							{
								activeArrows.DoUp ();
							}
							
							else if (activeArrows.downArrow.rect.Contains (invertedMouse))
							{
								activeArrows.DoDown ();
							}
							
							else if (activeArrows.leftArrow.rect.Contains (invertedMouse))
							{
								activeArrows.DoLeft ();
							}
							
							else if (activeArrows.rightArrow.rect.Contains (invertedMouse))
							{
								activeArrows.DoRight ();
							}
						}
					}
				}
				
				if (activeArrows == null && KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick)
				{
					float h = 0f;
					float v = 0f;
					bool run;
					
					if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen || KickStarter.settingsManager.movementMethod == MovementMethod.Drag)
					{
						if (dragState != DragState.None)
						{
							h = dragVector.x;
							v = -dragVector.y;
						}
					}
					else
					{
						h = InputGetAxis ("Horizontal");
						v = InputGetAxis ("Vertical");
					}

					if (InputGetButtonDown ("Jump") && KickStarter.stateHandler.gameState == GameState.Normal)
					{
						KickStarter.player.Jump ();
					}
					
					if ((isUpLocked && v > 0f) || (isDownLocked && v < 0f))
					{
						v = 0f;
					}
					
					if ((isLeftLocked && h > 0f) || (isRightLocked && h < 0f))
					{
						h = 0f;
					}
					
					if (runLock == PlayerMoveLock.Free)
					{
						if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen || KickStarter.settingsManager.movementMethod == MovementMethod.Drag)
						{
							if (dragStartPosition != Vector2.zero && dragSpeed > KickStarter.settingsManager.dragRunThreshold * 10f)
							{
								run = true;
							}
							else
							{
								run = false;
							}
						}
						else
						{
							if (InputGetAxis ("Run") > 0.1f)
							{
								run = true;
							}
							else
							{
								run = InputGetButton ("Run");
							}

							if (InputGetButtonDown ("ToggleRun"))
							{
								toggleRun = !toggleRun;
							}
						}
					}
					else if (runLock == PlayerMoveLock.AlwaysWalk)
					{
						run = false;
					}
					else
					{
						run = true;
					}
					
					if (KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen && (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson || KickStarter.settingsManager.movementMethod == MovementMethod.Direct) && runLock == PlayerMoveLock.Free && toggleRun)
					{
						playerIsControlledRunning = !run;
					}
					else
					{
						playerIsControlledRunning = run;
					}

					moveKeys = CreateMoveKeys (h, v);
				}
				
				if (InputGetButtonDown ("FlashHotspots"))
				{
					FlashHotspots ();
				}
			}
		}


		private Vector2 CreateMoveKeys (float h, float v)
		{
			if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct && KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen && KickStarter.settingsManager.directMovementType == DirectMovementType.RelativeToCamera)
			{
				if (KickStarter.settingsManager.limitDirectMovement == LimitDirectMovement.FourDirections)
				{
					if (Mathf.Abs (h) > Mathf.Abs (v))
					{
						v = 0f;
					}
					else
					{
						h = 0f;
					}
				}
				else if (KickStarter.settingsManager.limitDirectMovement == LimitDirectMovement.EightDirections)
				{
					if (Mathf.Abs (h) > Mathf.Abs (v))
					{
						v = 0f;
					}
					else if (Mathf.Abs (h) < Mathf.Abs (v))
					{
						h = 0f;
					}
					else if (Mathf.Abs (h) > 0.4f && Mathf.Abs (v) > 0.4f)
					{
						if (h*v > 0)
						{
							h = v;
						}
						else
						{
							h = -v;
						}
					}
					else
					{
						h = v = 0f;
					}
				}
			}

			if (cameraLockSnap)
			{
				Vector2 newMoveKeys = new Vector2 (h, v);
				if (newMoveKeys.sqrMagnitude < 0.01f || Vector2.Angle (newMoveKeys, moveKeys) > 5f)
				{
					cameraLockSnap = false;
					return newMoveKeys;
				}
				return moveKeys;
			}

			return new Vector2 (h, v);
		}


		private void FlashHotspots ()
		{
			Hotspot[] hotspots = FindObjectsOfType (typeof (Hotspot)) as Hotspot[];
			foreach (Hotspot hotspot in hotspots)
			{
				if (hotspot.IsOn () && hotspot.highlight && hotspot != KickStarter.playerInteraction.GetActiveHotspot ())
				{
					hotspot.highlight.Flash ();
				}
			}
		}
		

		/**
		 * Disables the active ArrowPrompt.
		 */
		public void RemoveActiveArrows ()
		{
			if (activeArrows)
			{
				activeArrows.TurnOff ();
			}
		}
		

		/**
		 * Records the current click time, so that another click will not register for the duration of clickDelay.
		 */
		public void ResetClick ()
		{
			clickTime = clickDelay;
			hasUnclickedSinceClick = false;
		}
		
		
		private void ResetDoubleClick ()
		{
			doubleClickTime = doubleClickDelay;
		}
		

		/**
		 * <summary>Checks if a mouse click will be registered.</summary>
		 * <returns>True if a mouse click will be registered</returns>
		 */
		public bool CanClick ()
		{
			if (clickTime == 0f)
			{
				return true;
			}
			
			return false;
		}
		

		/**
		 * <summary>Checks if a mouse double-click will be registered.</summary>
		 * <returns>True if a mouse double-click will be registered</returns>
		 */
		public bool CanDoubleClick ()
		{
			if (doubleClickTime > 0f && clickTime == 0f)
			{
				return true;
			}
			
			return false;
		}


		/**
		 * <summary>Simulates the pressing of an Input button.</summary>
		 * <param name = "button">The name of the Input button</param>
		 */
		public void SimulateInputButton (string button)
		{
			SimulateInput (SimulateInputType.Button, button, 1f);
		}
		

		/**
		 * <summary>Simulates the pressing of an Input axis.</summary>
		 * <param name = "axis">The name of the Input axis</param>
		 * <param name = "value">The value to assign the Input axis</param>
		 */
		public void SimulateInputAxis (string axis, float value)
		{
			SimulateInput (SimulateInputType.Axis, axis, value);
		}
		

		/**
		 * <summary>Simulates the pressing of an Input button or axis.</summary>
		 * <param name = "input">The type of Input this is simulating (Button, Axis)</param>
		 * <param name = "axis">The name of the Input button or axis</param>
		 * <param name = "value">The value to assign the Input axis, if input = SimulateInputType.Axis</param>
		 */
		public void SimulateInput (SimulateInputType input, string axis, float value)
		{
			if (axis != "")
			{
				menuInput = input;
				menuButtonInput = axis;
				
				if (input == SimulateInputType.Button)
				{
					menuButtonValue = 1f;
				}
				else
				{
					menuButtonValue = value;
				}

				CancelInvoke ();
				Invoke ("StopSimulatingInput", 0.1f);
			}
		}


		/**
		 * <summary>Checks if the cursor is locked.</summary>
		 * <returns>True if the cursor is locked</returns>
		 */
		public bool IsCursorLocked ()
		{
			return UnityVersionHandler.CursorLock;
		}
		
		
		private void StopSimulatingInput ()
		{
			menuButtonInput = "";
		}


		/**
		 * <summary>Checks if any input button is currently being pressed, simulated or otherwise.</summary>
		 * <returns>True if any input button is currently being pressed, simulated or otherwise.</returns>
		 */
		public bool InputAnyKey ()
		{
			if (menuButtonInput != null && menuButtonInput != "")
			{
				return true;
			}
			return Input.anyKey;
		}


		private float InputGetAxisRaw (string axis)
		{
			if (axis == "")
			{
				return 0f;
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetAxisRaw (axis) != 0f)
				{
					return Input.GetAxisRaw (axis);
				}
			}
			else
			{
				if (InputGetAxisDelegate != null)
				{
					return InputGetAxisDelegate (axis);
				}

				try
				{
					if (Input.GetAxisRaw (axis) != 0f)
					{
						return Input.GetAxisRaw (axis);
					}
				}
				catch {}
			}
			
			if (menuButtonInput != "" && menuButtonInput == axis && menuInput == SimulateInputType.Axis)
			{
				return menuButtonValue;
			}
			
			return 0f;
		}
		

		/**
		 * <summary>Replaces "Input.GetAxis", allowing for custom overrides.</summary>
		 * <param name = "axis">The Input axis to detect</param>
		 * <returns>The Input axis' value</returns>
		 */
		public float InputGetAxis (string axis)
		{
			if (axis == "")
			{
				return 0f;
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetAxis (axis) != 0f)
				{
					return Input.GetAxis (axis);
				}
			}
			else
			{
				if (InputGetAxisDelegate != null)
				{
					return InputGetAxisDelegate (axis);
				}

				try
				{
					if (Input.GetAxis (axis) != 0f)
					{
						return Input.GetAxis (axis);
					}
				}
				catch {}
			}

			if (menuButtonInput != "" && menuButtonInput == axis && menuInput == SimulateInputType.Axis)
			{
				return menuButtonValue;
			}
			
			return 0f;
		}
		
		
		private bool InputGetMouseButton (int button)
		{
			if (InputGetMouseButtonDelegate != null)
			{
				return InputGetMouseButtonDelegate (button);
			}
			return Input.GetMouseButton (button);
		}
		
		
		private Vector2 InputMousePosition (bool _cursorIsLocked)
		{
			if (InputMousePositionDelegate != null)
			{
				return InputMousePositionDelegate (_cursorIsLocked);
			}

			if (_cursorIsLocked)
			{
				return new Vector2 (Screen.width / 2f, Screen.height / 2f);
			}
			return Input.mousePosition;
		}


		private Vector2 InputGetFreeAim (bool _cursorIsLocked)
		{
			if (InputGetFreeAimDelegate != null)
			{
				return InputGetFreeAimDelegate (_cursorIsLocked);
			}

			if (_cursorIsLocked)
			{
				return new Vector2 (InputGetAxis ("CursorHorizontal"), InputGetAxis ("CursorVertical"));
			}
			return Vector2.zero;
		}
		
		
		private bool InputGetMouseButtonDown (int button)
		{
			if (InputGetMouseButtonDownDelegate != null)
			{
				return InputGetMouseButtonDownDelegate (button);
			}
			return Input.GetMouseButtonDown (button);
		}
		

		/**
		 * <summary>Replaces "Input.GetButton", allowing for custom overrides.</summary>
		 * <param name = "axis">The Input button to detect</param>
		 * <returns>True if the Input button is pressed</returns>
		 */
		public bool InputGetButton (string axis)
		{
			if (axis == "")
			{
				return false;
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetButton (axis))
				{
					return true;
				}
			}
			else
			{
				if (InputGetButtonDelegate != null)
				{
					return InputGetButtonDelegate (axis);
				}

				try
				{
					if (Input.GetButton (axis))
					{
						return true;
					}
				}
				catch {}
			}

			if (menuButtonInput != "" && menuButtonInput == axis && menuInput == SimulateInputType.Button)
			{
				if (menuButtonValue > 0f)
				{
					ResetClick ();
					StopSimulatingInput ();	
					return true;
				}
				
				StopSimulatingInput ();
			}

			return false;
		}
		

		/**
		 * <summary>Replaces "Input.GetButton", allowing for custom overrides.</summary>
		 * <param name = "axis">The Input button to detect</param>
		 * <param name = "showError">If True, then an error message will appear in the Console window if the button is not defined in the Input manager</param>
		 * <returns>True if the Input button is pressed</returns>
		 */
		public bool InputGetButtonDown (string axis, bool showError = false)
		{
			if (axis == "")
			{
				return false;
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetButtonDown (axis))
				{
					return true;
				}
			}
			else
			{
				if (InputGetButtonDownDelegate != null)
				{
					return InputGetButtonDownDelegate (axis);
				}

				try
				{
					if (Input.GetButtonDown (axis))
					{
						return true;
					}
				}
				catch
				{
					if (showError)
					{
						ACDebug.LogError ("Cannot find Input button '" + axis + "' - please define it in Unity's Input Manager (Edit -> Project settings -> Input).");
					}
				}
			}
			
			if (menuButtonInput != "" && menuButtonInput == axis && menuInput == SimulateInputType.Button)
			{
				if (menuButtonValue > 0f)
				{
					ResetClick ();
					StopSimulatingInput ();	
					return true;
				}
				
				StopSimulatingInput ();
			}
			
			return false;
		}


		/**
		 * <summary>Replaces "Input.GetButtonUp".</summary>
		 * <param name = "axis">The Input button to detect</param>
		 * <returns>True if the Input button is released</returns>
		 */
		public bool InputGetButtonUp (string axis)
		{
			if (axis == "")
			{
				return false;
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetButtonUp (axis))
				{
					return true;
				}
			}
			else
			{
				if (InputGetButtonUpDelegate != null)
				{
					return InputGetButtonUpDelegate (axis);
				}

				try
				{
					if (Input.GetButtonUp (axis))
					{
						return true;
					}
				}
				catch {}
			}
			return false;
		}
		
		
		private void SetDragState ()
		{
			if (KickStarter.runtimeInventory.selectedItem != null && KickStarter.settingsManager.inventoryDragDrop && (KickStarter.stateHandler.gameState == GameState.Normal || KickStarter.stateHandler.gameState == GameState.Paused))
			{
				if (dragVector.magnitude >= KickStarter.settingsManager.dragDropThreshold)
				{
					dragState = DragState.Inventory;
				}
				else
				{
					dragState = DragState.PreInventory;
				}
			}
			else if (activeDragElement != null && (KickStarter.stateHandler.gameState == GameState.Normal || KickStarter.stateHandler.gameState == GameState.Paused))
			{
				dragState = DragState.Menu;
			}
			else if (activeArrows != null && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				dragState = DragState.ScreenArrows;
			}
			else if (dragObject != null)
			{
				dragState = DragState.Moveable;
			}
			else if (KickStarter.mainCamera.attachedCamera && KickStarter.mainCamera.attachedCamera.isDragControlled)
			{
				if (!KickStarter.playerInteraction.IsMouseOverHotspot ())
				{
					dragState = DragState._Camera;
					if (deltaDragMouse.magnitude * Time.deltaTime <= 1f && (GetInvertedMouse () - dragStartPosition).magnitude < 10f)
					{
						dragState = DragState.None;
					}
				}
			}
			else if ((KickStarter.settingsManager.movementMethod == MovementMethod.Drag || KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor ||
			          (KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen))
			         && KickStarter.settingsManager.movementMethod != MovementMethod.None && KickStarter.stateHandler.gameState == GameState.Normal)
			{
				if (!KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerMenus.IsInteractionMenuOn ())
				{
					if (KickStarter.playerInteraction.IsMouseOverHotspot ())
					{}
					else
					{
						dragState = DragState.Player;
					}
				}
			}
			else
			{
				dragState = DragState.None;
			}
		}


		private void SetDragStateTouchScreen ()
		{
			if (KickStarter.runtimeInventory.selectedItem != null && KickStarter.settingsManager.inventoryDragDrop && (KickStarter.stateHandler.gameState == GameState.Normal || KickStarter.stateHandler.gameState == GameState.Paused))
			{}
			else if (activeDragElement != null && (KickStarter.stateHandler.gameState == GameState.Normal || KickStarter.stateHandler.gameState == GameState.Paused))
			{}
			else if (activeArrows != null && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{}
			else if (dragObject != null)
			{}
			else if (KickStarter.mainCamera.attachedCamera && KickStarter.mainCamera.attachedCamera.isDragControlled)
			{}
			else if ((KickStarter.settingsManager.movementMethod == MovementMethod.Drag || KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor ||
			          (KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen))
			         && KickStarter.settingsManager.movementMethod != MovementMethod.None && KickStarter.stateHandler.gameState == GameState.Normal)
			{
				if (!KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerMenus.IsInteractionMenuOn ())
				{
					if (KickStarter.playerInteraction.IsMouseOverHotspot ())
					{}
					else
					{
						dragState = DragState.Player;
					}
				}
			}
			else
			{
				dragState = DragState.None;
			}
		}
		
		
		private void UpdateDrag ()
		{
			if (dragState != DragState.None)
			{
				// Calculate change in mouse position
				if (freeAim.sqrMagnitude > 0f)
				{
					deltaDragMouse = freeAim * 500f / Time.deltaTime;
				}
				else
				{
					deltaDragMouse = (mousePosition - lastMousePosition) / Time.deltaTime;
				}
			}
			else
			{
				lastMousePosition = mousePosition;
			}

			if (dragObject && KickStarter.stateHandler.gameState != GameState.Normal)
			{
				LetGo (false);
			}

			if (mouseState == MouseState.HeldDown && dragState == DragState.None && KickStarter.stateHandler.CanInteract () && !KickStarter.playerMenus.IsMouseOverMenu ())
			{
				Grab ();
			}
			else if (dragState == DragState.Moveable)
			{
				if (dragObject)
				{
					if (dragObject.isHeld && dragObject.IsOnScreen () && dragObject.IsCloseToCamera (KickStarter.settingsManager.moveableRaycastLength))
					{
						Drag ();
					}
					else
					{
						LetGo (true);
					}
				}
			}
			else if (dragObject)
			{
				LetGo (true);
			}
			
			if (dragState != DragState.None)
			{
				lastMousePosition = mousePosition;
			}
		}


		/**
		 * <summary>Enables or disables the free-aiming lock.</summary>
		 * <param name = "_state">If True, the free-aiming lock is enabled, and free-aiming is disabled</param>
		 */
		public void SetFreeAimLock (bool _state)
		{
			freeAimLock = _state;
		}


		private void LetGo (bool unlockFPSCamera)
		{
			dragObject.LetGo ();
			KickStarter.eventManager.Call_OnDropMoveable (dragObject);
			dragObject = null;
		}
		
		
		private void Grab ()
		{
			if (dragObject)
			{
				dragObject.LetGo ();
				KickStarter.eventManager.Call_OnDropMoveable (dragObject);
				dragObject = null;
			}
			else if (canDragMoveable)
			{
				canDragMoveable = false;
				
				Ray ray = Camera.main.ScreenPointToRay (mousePosition); 
				RaycastHit hit = new RaycastHit ();
				
				if (Physics.Raycast (ray, out hit, KickStarter.settingsManager.moveableRaycastLength))
				{
					if (hit.transform.GetComponent <DragBase>())
					{
						dragObject = hit.transform.GetComponent <DragBase>();
						dragObject.Grab (hit.point);
						lastMousePosition = mousePosition;
						lastCameraPosition = Camera.main.transform.position;

						KickStarter.eventManager.Call_OnGrabMoveable (dragObject);
					}
				}
			}
		}

		
		private void Drag ()
		{
			// Convert to a 3D force
			if (dragObject.invertInput)
			{
				dragForce = (-Camera.main.transform.right * deltaDragMouse.x) + (-Camera.main.transform.up * deltaDragMouse.y);
			}
			else
			{
				dragForce = (Camera.main.transform.right * deltaDragMouse.x) + (Camera.main.transform.up * deltaDragMouse.y);
			}
			
			// Scale force with distance to camera, to lessen effects when close
			float distanceToCamera = (Camera.main.transform.position - dragObject.transform.position).magnitude;
			
			// Incoporate camera movement
			Vector3 deltaCamera = Camera.main.transform.position - lastCameraPosition;

			dragForce += deltaCamera * cameraInfluence;
			dragObject.ApplyDragForce (dragForce, mousePosition, distanceToCamera);
			
			lastCameraPosition = Camera.main.transform.position;
		}
		

		/**
		 * <summary>Gets the drag vector.</summary>
		 * <returns>The drag vector</returns>
		 */
		public Vector2 GetDragVector ()
		{
			if (dragState == AC.DragState._Camera)
			{
				return deltaDragMouse;
			}
			return dragVector;
		}
		

		/**
		 * <summary>Enables or disabled the Player's "up movement" lock.</summary>
		 * <param name = "state">If True, the "up movement" lock is enabled, and the player cannot move up</param>
		 */
		public void SetUpLock (bool state)
		{
			isUpLocked = state;
		}


		/**
		 * <summary>Enables or disabled the Player's "left movement" lock.</summary>
		 * <param name = "state">If True, the "up movement" lock is enabled, and the player cannot move left</param>
		 */
		public void SetLeftLock (bool state)
		{
			isLeftLocked = state;
		}


		/**
		 * <summary>Enables or disabled the Player's "right movement" lock.</summary>
		 * <param name = "state">If True, the "up movement" lock is enabled, and the player cannot move right</param>
		 */
		public void SetRightLock (bool state)
		{
			isRightLocked = state;
		}


		/**
		 * <summary>Enables or disabled the Player's "down movement" lock.</summary>
		 * <param name = "state">If True, the "up movement" lock is enabled, and the player cannot move down</param>
		 */
		public void SetDownLock (bool state)
		{
			isDownLocked = state;
		}


		/**
		 * <summary>Checks if the Player can be directly-controlled during gameplay.</summary>
		 * <returns>True if the Player can be directly-controlled during gameplay.</returns>
		 */
		public bool CanDirectControlPlayer ()
		{
			return !isUpLocked;
		}
		

		/**
		 * <summary>Checks if the active ArrowPrompt prevents Hotspots from being interactive.</summary>
		 * <returns>True if the active ArrowPrompt prevents Hotspots from being interactive</returns>
		 */
		public bool ActiveArrowsDisablingHotspots ()
		{
			if (activeArrows != null && activeArrows.disableHotspots)
			{
				return true;
			}
			return false;
		}
		

		private void ToggleCursor ()
		{
			if (dragObject != null && !dragObject.CanToggleCursor ())
			{
				return;
			}
			cursorIsLocked = !cursorIsLocked;
		}
		

		/**
		 * <summary>Checks if a specific DragBase object is being held by the player.</summary>
		 * <param name "_dragBase">The DragBase to check for</param>
		 * <returns>True if the DragBase object is being held by the Player</returns>
		 */
		public bool IsDragObjectHeld (DragBase _dragBase)
		{
			if (_dragBase == null || dragObject == null)
			{
				return false;
			}
			if (_dragBase == dragObject)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets the factor by which Player movement is slowed when holding a DragBase object.</summary>
		 * <returns>The factor by which Player movement is slowed when holding a DragBase object</returns>
		 */
		public float GetDragMovementSlowDown ()
		{
			if (dragObject != null)
			{
				return (1f - dragObject.playerMovementReductionFactor);
			}
			return 1f;
		}
		
		
		private float GetDeltaTime ()
		{
			if (Time.deltaTime == 0f || KickStarter.stateHandler.gameState == GameState.Paused)
			{
				return 0.02f;
			}
			return Time.deltaTime;
		}


		/**
		 * <summary>Sets the timeScale.</summary>
		 * <param name = "_timeScale">The new timeScale. A value of 0 will have no effect<param>
		 */
		public void SetTimeScale (float _timeScale)
		{
			if (_timeScale > 0f)
			{
				timeScale = _timeScale;
				if (KickStarter.stateHandler.gameState != GameState.Paused)
				{
					Time.timeScale = _timeScale;
				}
			}
		}


		/**
		 * <summary>Assigns an AnimationCurve that controls the timeScale over time.</summary>
		 * <param name = "_timeCurve">The AnimationCurve to use</param>
		 */
		public void SetTimeCurve (AnimationCurve _timeCurve)
		{
			timeCurve = _timeCurve;
			changeTimeStart = Time.time;
		}


		/**
		 * <summary>Checks if time is being controlled by an AnimationCurve.</summary>
		 * <returns>True if time is being controlled by an AnimationCurve.</returns>
		 */
		public bool HasTimeCurve ()
		{
			if (timeCurve != null)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Get what kind of object is currently being dragged (None, Player, Inventory, Menu, ScreenArrows, Moveable, _Camera).</summary>
		 * <returns>What kind of object is currently being dragged (None, Player, Inventory, Menu, ScreenArrows, Moveable, _Camera).</returns>
		 */
		public DragState GetDragState ()
		{
			return dragState;
		}


		/**
		 * <summary>Gets the current state of the mouse buttons (Normal, SingleClick, RightClick, DoubleClick, HeldDown, LetGo).</summary>
		 * <returns>The current state of the mouse buttons (Normal, SingleClick, RightClick, DoubleClick, HeldDown, LetGo).</returns>
		 */
		public MouseState GetMouseState ()
		{
			return mouseState;
		}


		/**
		 * Resets the mouse click so that nothing else will be affected by it this frame.
		 */
		public void ResetMouseClick ()
		{
			mouseState = MouseState.Normal;
		}


		/**
		 * <summary>Gets the input movement as a vector</summary>
		 * <returns>The input movement as a vector</returns>
		 */
		public Vector2 GetMoveKeys ()
		{
			return moveKeys;
		}


		/**
		 * <summary>Checks if the Player is running due to user-controlled input.</summary>
		 * <returns>True if the Player is running due to user-controller input</returns>
		 */
		public bool IsPlayerControlledRunning ()
		{
			return playerIsControlledRunning;
		}


		/**
		 * <summary>Assigns a MenuDrag element as the one to drag.</summary>
		 * <param name = "menuDrag">The MenuDrag to begin dragging</param>
		 */
		public void SetActiveDragElement (MenuDrag menuDrag)
		{
			activeDragElement = menuDrag;
		}


		/**
		 * <summary>Checks if the last mouse click made was a double-click.</summary>
		 * <returns>True if the last mouse click made was a double-click</returns>
		 */
		public bool LastClickWasDouble ()
		{
			return lastClickWasDouble;
		}


		/**
		 * Resets the speed of "Drag" Player input.
		 */
		public void ResetDragMovement ()
		{
			dragSpeed = 0f;
		}


		/**
		 * <summary>Checks if the magnitude of "Drag" Player input is above the minimum needed to move the Player.</summary>
		 * <returns>True if the magnitude of "Drag" Player input is above the minimum needed to move the Player.</returns>
		 */
		public bool IsDragMoveSpeedOverWalkThreshold ()
		{
			if (dragSpeed > KickStarter.settingsManager.dragWalkThreshold * 10f)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the cursor's position is within the boundary of the screen.</summary>
		 * <returns>True if the cursor's position is within the boundary of the screen</returns>
		 */
		public bool IsMouseOnScreen ()
		{
			return mouseIsOnScreen;
		}


		/**
		 * <summary>Gets the free-aim input vector.</summary>
		 * <returns>The free-aim input vector</returns>
		 */
		public Vector2 GetFreeAim ()
		{
			return freeAim;
		}


		/**
		 * <summary>Checks if free-aiming is locked.</summary>
		 * <returns>True if free-aiming is locked</returns>
		 */
		public bool IsFreeAimingLocked ()
		{
			return freeAimLock;
		}


		/**
		 * <summary>Checks if the Player is prevented from being moved directly in all four directions.</summary>
		 * <returns>True if the Player is prevented from being moved directly in all four direction</returns>
		 */
		public bool AllDirectionsLocked ()
		{
			if (isDownLocked && isUpLocked && isLeftLocked && isRightLocked)
			{
				return true;
			}
			return false;
		}


		/**
		 * Resets the mouse and assigns the correct gameState in StateHandler after loading a save game.
		 */
		public void ReturnToGameplayAfterLoad ()
		{
			if (activeConversation)
			{
				KickStarter.stateHandler.gameState = GameState.DialogOptions;
			}
			else
			{
				KickStarter.stateHandler.gameState = GameState.Normal;
			}
			ResetMouseClick ();
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			mainData.timeScale = KickStarter.playerInput.timeScale;
			if (activeArrows)
			{
				mainData.activeArrows = Serializer.GetConstantID (activeArrows.gameObject);
			}
			if (activeConversation)
			{
				mainData.activeConversation = Serializer.GetConstantID (activeConversation.gameObject);
			}
			mainData.canKeyboardControlMenusDuringGameplay = canKeyboardControlMenusDuringGameplay;

			return mainData;
		}
		
		
		/**
		 * <summary>Updates its own variables from a MainData class.</summary>
		 * <param name = "mainData">The MainData class to load from</param>
		 */
		public void LoadMainData (MainData mainData)
		{
			// Active screen arrows
			RemoveActiveArrows ();
			ArrowPrompt loadedArrows = Serializer.returnComponent <ArrowPrompt> (mainData.activeArrows);
			if (loadedArrows)
			{
				loadedArrows.TurnOn ();
			}
			
			// Active conversation
			activeConversation = Serializer.returnComponent <Conversation> (mainData.activeConversation);
			timeScale = mainData.timeScale;

			canKeyboardControlMenusDuringGameplay = mainData.canKeyboardControlMenusDuringGameplay;
		}


		/**
		 * <summary>Updates a PlayerData class with its own variables that need saving.</summary>
		 * <param name = "playerData">The original PlayerData class</param>
		 * <returns>The updated PlayerData class</returns>
		 */
		public PlayerData SavePlayerData (PlayerData playerData)
		{
			playerData.playerUpLock = isUpLocked;
			playerData.playerDownLock = isDownLocked;
			playerData.playerLeftlock = isLeftLocked;
			playerData.playerRightLock = isRightLocked;
			playerData.playerRunLock = (int) runLock;
			playerData.playerFreeAimLock = IsFreeAimingLocked ();
			
			return playerData;
		}


		/**
		 * <summary>Updates its own variables from a PlayerData class.</summary>
		 * <param name = "playerData">The PlayerData class to load from</param>
		 */
		public void LoadPlayerData (PlayerData playerData)
		{
			SetUpLock (playerData.playerUpLock);
			isDownLocked = playerData.playerDownLock;
			isLeftLocked = playerData.playerLeftlock;
			isRightLocked = playerData.playerRightLock;
			runLock = (PlayerMoveLock) playerData.playerRunLock;
			SetFreeAimLock (playerData.playerFreeAimLock);
		}


		/**
		 * <summary>Controls an OnGUI-based Menu with keyboard or Controller inputs.</summary>
		 * <param name = "menu">The Menu to control</param>
		 */
		public void InputControlMenu (Menu menu)
		{
			if (KickStarter.settingsManager.inputMethod != InputMethod.KeyboardOrController)
			{
				return;
			}
			if ((KickStarter.stateHandler.gameState == GameState.Paused && menu.IsBlocking () && KickStarter.menuManager.keyboardControlWhenPaused) ||
				(KickStarter.stateHandler.gameState == GameState.DialogOptions && menu.appearType == AppearType.DuringConversation && KickStarter.menuManager.keyboardControlWhenDialogOptions) ||
				(KickStarter.stateHandler.gameState == GameState.Cutscene && menu.CanClickInCutscenes ()) ||
				(KickStarter.stateHandler.gameState == GameState.Normal && canKeyboardControlMenusDuringGameplay && menu.CanPause () && !menu.pauseWhenEnabled))
			{
				selected_option = menu.ControlSelected (selected_option);
			}
		}

	}
	
}

