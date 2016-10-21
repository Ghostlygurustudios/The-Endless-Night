
/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"PlayerMenus.cs"
 * 
 *	This script handles the displaying of each of the menus defined in MenuManager.
 *	It avoids referencing specific menus and menu elements as much as possible,
 *	so that the menu can be completely altered using just the MenuSystem script.
 * 
 */

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script handles the initialisation, position and display of all Menus defined in MenuManager.
	 * Menus are transferred from MenuManager to a local List within this script when the game begins.
	 * It must be placed on the PersistentEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_menus.html")]
	#endif
	public class PlayerMenus : MonoBehaviour
	{

		private bool mouseOverMenu = false;
		private bool mouseOverInteractionMenu = false;
		private bool interactionMenuIsOn = false;
		private bool interactionMenuPauses = false;

		private bool lockSave = false;
		private int selected_option;

		private bool foundMouseOverMenu = false;
		private bool foundMouseOverInteractionMenu = false;
		private bool foundMouseOverInventory = false;
		private bool mouseOverInventory = false;

		private bool isPaused;
		private string hotspotLabel = "";
		private float pauseAlpha = 0f;
		private List<Menu> menus = new List<Menu>();
		private List<Menu> dupSpeechMenus = new List<Menu>();
		private List<Menu> dupHotspotMenus = new List<Menu>();
		private Texture2D pauseTexture;
		private string elementIdentifier;
		private string lastElementIdentifier;
		private MenuInput selectedInputBox;
		private string selectedInputBoxMenuName;
		private MenuInventoryBox activeInventoryBox;
		private MenuCrafting activeCrafting;
		private Menu activeInventoryBoxMenu;
		private InvItem oldHoverItem;
		private int doResizeMenus = 0;

		private Menu mouseOverMenuName;
		private MenuElement mouseOverElementName;
		private int mouseOverElementSlot;
		
		private Menu crossFadeTo;
		private Menu crossFadeFrom;
		private UnityEngine.EventSystems.EventSystem eventSystem;

		private int elementOverCursorID = -1;

		private GUIStyle normalStyle = new GUIStyle ();
		private GUIStyle highlightedStyle = new GUIStyle();
		private int lastScreenWidth = 0;
		private int lastScreenHeight = 0;
		
		#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
		private TouchScreenKeyboard keyboard;
		#endif

		
		public void OnStart ()
		{
			RebuildMenus ();
		}


		/**
		 * <summary>Rebuilds the game's Menus, either from the existing MenuManager asset, or from a new one.</summary>
		 * <param name = "menuManager">The Menu Manager to use for Menu generation. If left empty, the default Menu Manager will be used.</param>
		 */
		public void RebuildMenus (MenuManager menuManager = null)
		{
			if (menuManager != null)
			{
				KickStarter.menuManager = menuManager;
			}

			foreach (Menu menu in menus)
			{
				if (menu.menuSource == MenuSource.UnityUiPrefab && menu.canvas != null && menu.canvas.gameObject != null)
				{
					Destroy (menu.canvas.gameObject);
				}
			}

			menus = new List<Menu>();
			
			if (KickStarter.menuManager)
			{
				pauseTexture = KickStarter.menuManager.pauseTexture;
				foreach (AC.Menu _menu in KickStarter.menuManager.menus)
				{
					Menu newMenu = ScriptableObject.CreateInstance <Menu>();
					newMenu.Copy (_menu, false);

					if (_menu.limitToCharacters != "")
					{
						newMenu.limitToCharacters = ";" + _menu.limitToCharacters + ";";
					}
					
					if (_menu.GetsDuplicated ())
					{
						// Don't make canvas object yet!
					}
					else if (newMenu.IsUnityUI ())
					{
						newMenu.LoadUnityUI ();
					}

					newMenu.Recalculate ();

					newMenu.Initalise ();
					menus.Add (newMenu);
				}
			}
			
			CreateEventSystem ();
			
			foreach (AC.Menu menu in menus)
			{
				menu.Recalculate ();
			}
			
			#if UNITY_WEBPLAYER && !UNITY_EDITOR
			// WebPlayer takes another second to get the correct screen dimensions
			foreach (AC.Menu menu in menus)
			{
				menu.Recalculate ();
			}
			#endif

			KickStarter.eventManager.Call_OnGenerateMenus ();

			StartCoroutine (CycleMouseOverUIs ());
		}


		private IEnumerator CycleMouseOverUIs ()
		{
			// MouseOver UI menus need to be enabled in the first frame so that their RectTransforms can be recognised by Unity

			foreach (Menu menu in menus)
			{
				if (menu.menuSource != MenuSource.AdventureCreator && menu.appearType == AppearType.MouseOver)
				{
					menu.EnableUI ();
				}
			}

			yield return new WaitForEndOfFrame ();

			foreach (Menu menu in menus)
			{
				if (menu.menuSource != MenuSource.AdventureCreator && menu.appearType == AppearType.MouseOver)
				{
					menu.DisableUI ();
				}
			}
		}


		private void CreateEventSystem ()
		{
			if (GameObject.FindObjectOfType <UnityEngine.EventSystems.EventSystem>() == null)
			{
				UnityEngine.EventSystems.EventSystem _eventSystem = null;

				if (KickStarter.menuManager)
				{
					if (KickStarter.menuManager.eventSystem != null)
					{
						_eventSystem = (UnityEngine.EventSystems.EventSystem) Instantiate (KickStarter.menuManager.eventSystem);
						_eventSystem.gameObject.name = KickStarter.menuManager.eventSystem.name;
					}
					else if (AreAnyMenusUI ())
					{
						_eventSystem = UnityVersionHandler.CreateEventSystem ();
					}
				}

				if (_eventSystem != null)
				{
					if (GameObject.Find ("_UI"))
					{
						_eventSystem.transform.SetParent (GameObject.Find ("_UI").transform);
					}
					eventSystem = _eventSystem;
				}
			}
		}


		private bool AreAnyMenusUI ()
		{
			foreach (AC.Menu menu in menus)
			{
				if (menu.menuSource == MenuSource.UnityUiInScene || menu.menuSource == MenuSource.UnityUiPrefab)
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * Initialises the menu system after a scene change. This is called manually by SaveSystem so that the order is correct.
		 */
		public void AfterLoad ()
		{
			if (KickStarter.settingsManager != null && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			CreateEventSystem ();

			foreach (AC.Menu _menu in menus)
			{
				if (_menu.menuSource == MenuSource.UnityUiInScene)
				{
					_menu.LoadUnityUI ();
					_menu.Initalise ();
				}
				else if (_menu.menuSource == MenuSource.UnityUiPrefab)
				{
					_menu.SetParent ();
				}
			}

			foreach (Menu menu in menus)
			{
				menu.AfterSceneChange ();
			}

			StartCoroutine (CycleMouseOverUIs ());
		}


		public void AfterSceneAdd ()
		{
			foreach (AC.Menu _menu in menus)
			{
				if (_menu.menuSource == MenuSource.UnityUiInScene)
				{
					_menu.LoadUnityUI ();
					_menu.Initalise ();
				}
			}
		}


		/**
		 * Clears the parents of any Unity UI-based Menu Canvases.
		 * This makes them able to survive a scene change.
		 */
		public void ClearParents ()
		{
			foreach (AC.Menu _menu in menus)
			{
				if (_menu.IsUnityUI () && _menu.canvas != null)
				{
					_menu.ClearParent ();
				}
			}
		}


		private void ShowPauseBackground (bool fadeIn)
		{
			float fadeSpeed = 0.5f;
			if (fadeIn)
			{
				if (pauseAlpha < 1f)
				{
					pauseAlpha += (0.2f * fadeSpeed);
				}				
				else
				{
					pauseAlpha = 1f;
				}
			}
			
			else
			{
				if (pauseAlpha > 0f)
				{
					pauseAlpha -= (0.2f * fadeSpeed);
				}
				else
				{
					pauseAlpha = 0f;
				}
			}
			
			Color tempColor = GUI.color;
			tempColor.a = pauseAlpha;
			GUI.color = tempColor;
			GUI.DrawTexture (AdvGame.GUIRect (0.5f, 0.5f, 1f, 1f), pauseTexture, ScaleMode.ScaleToFit, true, 0f);
		}


		/**
		 * Draws any OnGUI-based Menus set to appear while the game is loading.
		 */
		public void DrawLoadingMenus ()
		{
			for (int i=0; i<menus.Count; i++)
			{
				int languageNumber = Options.GetLanguage ();
				if (menus[i].appearType == AppearType.WhileLoading)
				{
					DrawMenu (menus[i], languageNumber);
				}
			}
		}
		

		/**
		 * Draws all OnGUI-based Menus.
		 */
		public void DrawMenus ()
		{
			if (doResizeMenus > 0)
			{
				return;
			}

			elementOverCursorID = -1;

			if (KickStarter.playerInteraction && KickStarter.playerInput && KickStarter.menuSystem && KickStarter.stateHandler && KickStarter.settingsManager)
			{
				GUI.depth = KickStarter.menuManager.globalDepth;
				
				if (pauseTexture)
				{
					isPaused = false;

					for (int j=0; j<menus.Count; j++)
					{
						if (menus[j].IsEnabled () && menus[j].IsBlocking ())
						{
							isPaused = true;
						}
					}
					
					if (isPaused)
					{
						ShowPauseBackground (true);
					}
					else
					{
						ShowPauseBackground (false);
					}
				}
				
				if (selectedInputBox)
				{
					Event currentEvent = Event.current;
					if (currentEvent.isKey && currentEvent.type == EventType.KeyDown)
					{
						selectedInputBox.CheckForInput (currentEvent.keyCode.ToString (), currentEvent.shift, selectedInputBoxMenuName);
					}
				}
				
				int languageNumber = Options.GetLanguage ();

				for (int j=0; j<menus.Count; j++)
				{
					DrawMenu (menus[j], languageNumber);
				}

				for (int j=0; j<dupSpeechMenus.Count; j++)
				{
					DrawMenu (dupSpeechMenus[j], languageNumber);
				}

				for (int j=0; j<dupHotspotMenus.Count; j++)
				{
					DrawMenu (dupHotspotMenus[j], languageNumber);
				}
			}
		}


		private void DrawMenu (AC.Menu menu, int languageNumber)
		{
			Color tempColor = GUI.color;
			bool isACMenu = !menu.IsUnityUI ();
			
			if (menu.IsEnabled ())
			{
				if (!menu.HasTransition () && menu.IsFading ())
				{
					// Stop until no longer "fading" so that it appears in right place
					return;
				}
				
				if (isACMenu)
				{
					if (menu.transitionType == MenuTransition.Fade || menu.transitionType == MenuTransition.FadeAndPan)
					{
						tempColor.a = 1f - menu.GetFadeProgress ();
						GUI.color = tempColor;
					}
					else
					{
						tempColor.a = 1f;
						GUI.color = tempColor;
					}
					
					menu.StartDisplay ();
				}

				for (int j=0; j<menu.elements.Count; j++)
				{
					if (menu.elements[j].isVisible)
					{
						if (isACMenu)
						{
							SetStyles (menu.elements[j]);
						}

						for (int i=0; i<menu.elements[j].GetNumSlots (); i++)
						{
							if (menu.IsEnabled () && KickStarter.stateHandler.gameState != GameState.Cutscene && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && menu.appearType == AppearType.OnInteraction)
							{
								if (menu.elements[j] is MenuInteraction)
								{
									MenuInteraction menuInteraction = (MenuInteraction) menu.elements[j];
									if (menuInteraction.iconID == KickStarter.playerInteraction.GetActiveUseButtonIconID ())
									{
										if (KickStarter.cursorManager.addHotspotPrefix)
										{
											if (KickStarter.runtimeInventory.hoverItem != null)
											{
												hotspotLabel = KickStarter.cursorManager.GetLabelFromID (menuInteraction.iconID, languageNumber) + KickStarter.runtimeInventory.hoverItem.GetLabel (languageNumber);
											}
											else
											{
												hotspotLabel = KickStarter.cursorManager.GetLabelFromID (menuInteraction.iconID, languageNumber) + KickStarter.playerInteraction.GetLabel (languageNumber);
											}
										}
										if (isACMenu)
										{
											menu.elements[j].Display (highlightedStyle, i, menu.GetZoom (), true);
										}
									}
									else
									{
										if (isACMenu)
										{
											menu.elements[j].Display (normalStyle, i, menu.GetZoom (), false);
										}
									}
								}
								else if (menu.elements[j] is MenuInventoryBox)
								{
									MenuInventoryBox menuInventoryBox = (MenuInventoryBox) menu.elements[j];
									if (menuInventoryBox.inventoryBoxType == AC_InventoryBoxType.HotspotBased && menuInventoryBox.items[i].id == KickStarter.playerInteraction.GetActiveInvButtonID ())
									{
										if (KickStarter.cursorManager.addHotspotPrefix)
										{
											hotspotLabel = KickStarter.runtimeInventory.GetHotspotPrefixLabel (menuInventoryBox.GetItem (i), menuInventoryBox.GetLabel (i, languageNumber), languageNumber);
											
											if (KickStarter.runtimeInventory.selectedItem != null)
											{
												hotspotLabel += KickStarter.runtimeInventory.selectedItem.GetLabel (languageNumber);
											}
											else
											{
												hotspotLabel += KickStarter.playerInteraction.GetLabel (languageNumber);
											}
										}
										if (isACMenu)
										{
											menu.elements[j].Display (highlightedStyle, i, menu.GetZoom (), true);
										}
									}
									else if (isACMenu)
									{
										menu.elements[j].Display (normalStyle, i, menu.GetZoom (), false);
									}
								}
								else if (isACMenu)
								{
									menu.elements[j].Display (normalStyle, i, menu.GetZoom (), false);
								}
							}

							else if (menu.IsClickable () && KickStarter.playerInput.IsCursorReadable () && SlotIsInteractive (menu, j, i))
							{
								if (isACMenu)
								{
									float zoom = 1;
									if (menu.transitionType == MenuTransition.Zoom)
									{
										zoom = menu.GetZoom ();
									}
									
									if ((!interactionMenuIsOn || menu.appearType == AppearType.OnInteraction)
										&& (KickStarter.playerInput.GetDragState () == DragState.None || (KickStarter.playerInput.GetDragState () == DragState.Inventory && CanElementBeDroppedOnto (menu.elements[j]))))
									{
										menu.elements[j].Display (highlightedStyle, i, zoom, true);
										
										if (menu.elements[j].changeCursor)
										{
											elementOverCursorID = menu.elements[j].cursorID;
										}
									}
									else
									{
										menu.elements[j].Display (normalStyle, i, zoom, false);
									}
								}
								else
								{
									// Unity UI
									if ((!interactionMenuIsOn || menu.appearType == AppearType.OnInteraction)
										&& (KickStarter.playerInput.GetDragState () == DragState.None || (KickStarter.playerInput.GetDragState () == DragState.Inventory && CanElementBeDroppedOnto (menu.elements[j]))))
									{
										if (menu.elements[j].changeCursor)
										{
											elementOverCursorID = menu.elements[j].cursorID;
										}
									}
								}
							}
							else if (isACMenu)
							{
								menu.elements[j].Display (normalStyle, i, menu.GetZoom (), false);
							}
						}
						
						if (menu.elements[j] is MenuInput)
						{
							if (selectedInputBox == null)
							{
								if (!menu.IsUnityUI ())
								{
									MenuInput input = (MenuInput) menu.elements[j];
									SelectInputBox (input);
								}
								
								selectedInputBoxMenuName = menu.title;
							}
						}
					}
				}
				
				if (isACMenu)
				{
					menu.EndDisplay ();
				}
			}
			
			if (isACMenu)
			{
				tempColor.a = 1f;
				GUI.color = tempColor;
			}
		}
		

		/**
		 * <summary>Updates a Menu's position.</summary>
		 * <param name = "menu">The Menu to reposition</param>
		 * <param name = "invertedMouse">The y-inverted mouse position</param>
		 */
		public void UpdateMenuPosition (AC.Menu menu, Vector2 invertedMouse)
		{
			if (menu.IsUnityUI ())
			{
				if (Application.isPlaying)
				{
					Vector2 screenPosition = Vector2.zero;

					if (menu.uiPositionType == UIPositionType.Manual)
					{
						return;
					}
					else if (menu.uiPositionType == UIPositionType.FollowCursor)
					{
						screenPosition = new Vector2 (invertedMouse.x, Screen.height + 1f - invertedMouse.y);
						menu.SetCentre (screenPosition);
					}
					else if (menu.uiPositionType == UIPositionType.OnHotspot)
					{
						if (mouseOverMenu) // Should be mouseOverInventory, not mouseOverMenu?
						{
							if (menu.appearType == AppearType.OnInteraction &&
							    menu.GetTargetInvItem () == null &&
							    menu.GetTargetHotspot () != null)
							{
								// Bypass
								return;
							}

							if (activeCrafting != null)
							{
								if (menu.GetTargetInvItem () != null)
								{
									int slot = activeCrafting.GetItemSlot (menu.GetTargetInvItem ().id);
									screenPosition = activeInventoryBoxMenu.GetSlotCentre (activeCrafting, slot);
									menu.SetCentre (new Vector2 (screenPosition.x, Screen.height - screenPosition.y));
								}
								else if (KickStarter.runtimeInventory.hoverItem != null)
								{
									int slot = activeCrafting.GetItemSlot (KickStarter.runtimeInventory.hoverItem.id);
									screenPosition = activeInventoryBoxMenu.GetSlotCentre (activeCrafting, slot);
									menu.SetCentre (new Vector2 (screenPosition.x, Screen.height - screenPosition.y));
								}
							}
							else if (activeInventoryBox)
							{
								if (menu.GetTargetInvItem () != null)
								{
									int slot = activeInventoryBox.GetItemSlot (menu.GetTargetInvItem ().id);
									screenPosition = activeInventoryBoxMenu.GetSlotCentre (activeInventoryBox, slot);
									menu.SetCentre (new Vector2 (screenPosition.x, Screen.height - screenPosition.y));
								}
								else if (KickStarter.runtimeInventory.hoverItem != null)
								{
									int slot = activeInventoryBox.GetItemSlot (KickStarter.runtimeInventory.hoverItem.id);
									screenPosition = activeInventoryBoxMenu.GetSlotCentre (activeInventoryBox, slot);
									menu.SetCentre (new Vector2 (screenPosition.x, Screen.height - screenPosition.y));
								}
							}
						}
						else
						{
							if (menu.appearType == AppearType.OnInteraction &&
							    menu.GetTargetInvItem () != null)
							{
								// Bypass
								return;
							}

							if (menu.GetTargetHotspot ())
							{
								if (menu.canvas.renderMode == RenderMode.WorldSpace)
								{
									menu.SetCentre (menu.GetTargetHotspot ().transform.position);
								}
								else
								{
									Vector2 screenPos = menu.GetTargetHotspot ().GetIconScreenPosition ();
									screenPosition = new Vector2 (screenPos.x / Screen.width, 1f - (screenPos.y / Screen.height));

									screenPosition = new Vector2 (screenPosition.x * Screen.width, (1f - screenPosition.y) * Screen.height);
									menu.SetCentre (screenPosition);
								}
							}
							else if (KickStarter.playerInteraction.GetLastOrActiveHotspot ())
							{
								if (menu.canvas.renderMode == RenderMode.WorldSpace)
								{
									menu.SetCentre (KickStarter.playerInteraction.GetLastOrActiveHotspot ().transform.position);
								}
								else
								{
									screenPosition = KickStarter.playerInteraction.GetLastHotspotScreenCentre ();
									screenPosition = new Vector2 (screenPosition.x * Screen.width, (1f - screenPosition.y) * Screen.height);
									menu.SetCentre (screenPosition);
								}
							}
						}
					}
					else if (menu.uiPositionType == UIPositionType.AboveSpeakingCharacter)
					{
						Char speaker = null;
						if (dupSpeechMenus.Contains (menu))
						{
							if (menu.speech != null)
							{
								speaker = menu.speech.GetSpeakingCharacter ();
							}
						}
						else
						{
							speaker = KickStarter.dialog.GetSpeakingCharacter ();
						}

						if (speaker != null)
						{
							if (menu.canvas != null && menu.canvas.renderMode == RenderMode.WorldSpace)
							{
								menu.SetCentre (speaker.transform.position);
							}
							else
							{
								screenPosition = speaker.GetScreenCentre ();
								screenPosition = new Vector2 (screenPosition.x * Screen.width, (1f - screenPosition.y) * Screen.height);
								menu.SetCentre (screenPosition);
							}
						}
					}
					else if (menu.uiPositionType == UIPositionType.AbovePlayer)
					{
						if (KickStarter.player)
						{
							if (menu.canvas.renderMode == RenderMode.WorldSpace)
							{
								menu.SetCentre (KickStarter.player.transform.position);
							}
							else
							{
								screenPosition = KickStarter.player.GetScreenCentre ();
								screenPosition = new Vector2 (screenPosition.x * Screen.width, (1f - screenPosition.y) * Screen.height);
								menu.SetCentre (screenPosition);
							}
						}
					}

				}

				return;
			}

			if (menu.sizeType == AC_SizeType.Automatic && menu.autoSizeEveryFrame)
			{
				menu.Recalculate ();
			}

			if (invertedMouse == Vector2.zero)
			{
				invertedMouse = KickStarter.playerInput.GetInvertedMouse ();
			}
			
			if (menu.positionType == AC_PositionType.FollowCursor)
			{
				menu.SetCentre (new Vector2 ((invertedMouse.x / Screen.width) + (menu.manualPosition.x / 100f) - 0.5f,
				                             (invertedMouse.y / Screen.height) + (menu.manualPosition.y / 100f) - 0.5f));
			}
			else if (menu.positionType == AC_PositionType.OnHotspot)
			{
				if (mouseOverInventory)
				{
					if (menu.appearType == AppearType.OnInteraction &&
					    menu.GetTargetInvItem () == null &&
					    menu.GetTargetHotspot () != null)
					{
						// Bypass
						return;
					}

					if (activeCrafting != null)
					{
						if (menu.GetTargetInvItem () != null)
						{
							int slot = activeCrafting.GetItemSlot (menu.GetTargetInvItem ().id);
							Vector2 activeInventoryItemCentre = activeInventoryBoxMenu.GetSlotCentre (activeCrafting, slot);

							Vector2 screenPosition = new Vector2 (activeInventoryItemCentre.x / Screen.width, activeInventoryItemCentre.y / Screen.height);
							menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
							                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
						}
						else if (KickStarter.runtimeInventory.hoverItem != null)
						{
							int slot = activeCrafting.GetItemSlot (KickStarter.runtimeInventory.hoverItem.id);
							Vector2 activeInventoryItemCentre = activeInventoryBoxMenu.GetSlotCentre (activeCrafting, slot);

							Vector2 screenPosition = new Vector2 (activeInventoryItemCentre.x / Screen.width, activeInventoryItemCentre.y / Screen.height);
							menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
							                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
						}
					}
					else if (activeInventoryBox != null)
					{
						if (menu.GetTargetInvItem () != null)
						{
							int slot = activeInventoryBox.GetItemSlot (menu.GetTargetInvItem ().id);
							Vector2 activeInventoryItemCentre = activeInventoryBoxMenu.GetSlotCentre (activeInventoryBox, slot);

							Vector2 screenPosition = new Vector2 (activeInventoryItemCentre.x / Screen.width, activeInventoryItemCentre.y / Screen.height);
							menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
							                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
						}
						else if (KickStarter.runtimeInventory.hoverItem != null)
						{
							int slot = activeInventoryBox.GetItemSlot (KickStarter.runtimeInventory.hoverItem.id);
							Vector2 activeInventoryItemCentre = activeInventoryBoxMenu.GetSlotCentre (activeInventoryBox, slot);

							Vector2 screenPosition = new Vector2 (activeInventoryItemCentre.x / Screen.width, activeInventoryItemCentre.y / Screen.height);
							menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
							                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
						}
					}
				}
				else
				{
					if (menu.appearType == AppearType.OnInteraction &&
					    menu.GetTargetInvItem () != null)
					{
						// Bypass
						return;
					}

					if (menu.GetTargetHotspot () != null)
					{
						Vector2 screenPos = menu.GetTargetHotspot ().GetIconScreenPosition ();
						Vector2 screenPosition = new Vector2 (screenPos.x / Screen.width, 1f - (screenPos.y / Screen.height));

						menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
						                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
					}
					else if (KickStarter.playerInteraction.GetLastOrActiveHotspot ())
					{
						Vector2 screenPosition = KickStarter.playerInteraction.GetLastHotspotScreenCentre ();
						menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
						                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
					}
				}
			}
			else if (menu.positionType == AC_PositionType.AboveSpeakingCharacter)
			{
				Char speaker = null;
				if (dupSpeechMenus.Contains (menu))
				{
					if (menu.speech != null)
					{
						speaker = menu.speech.GetSpeakingCharacter ();
					}
				}
				else
				{
					speaker = KickStarter.dialog.GetSpeakingCharacter ();
				}

				if (speaker != null)
				{
					Vector2 screenPosition = speaker.GetScreenCentre ();
					menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
					                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
				}
			}
			else if (menu.positionType == AC_PositionType.AbovePlayer)
			{
				if (KickStarter.player)
				{
					Vector2 screenPosition = KickStarter.player.GetScreenCentre ();
					menu.SetCentre (new Vector2 (screenPosition.x + (menu.manualPosition.x / 100f) - 0.5f,
					                             screenPosition.y + (menu.manualPosition.y / 100f) - 0.5f));
				}
			}
		}


		private void UpdateMenu (AC.Menu menu, bool justPosition = false)
		{
			Vector2 invertedMouse = KickStarter.playerInput.GetInvertedMouse ();
			UpdateMenuPosition (menu, invertedMouse);

			if (justPosition)
			{
				return;
			}

			menu.HandleTransition ();

			if (menu.IsEnabled ())
			{
				if (!KickStarter.playerMenus.IsCyclingInteractionMenu ())
				{
					KickStarter.playerInput.InputControlMenu (menu);
				}
			}

			if (menu.appearType == AppearType.Manual)
			{
				if (menu.IsVisible () && !menu.isLocked && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
				{
					foundMouseOverMenu = true;
				}
			}

			else if (menu.appearType == AppearType.DuringGameplay)
			{
				if (KickStarter.stateHandler.gameState == GameState.Normal && !menu.isLocked)
				{
					if (menu.IsOff ())
					{
						menu.TurnOn (true);
					}

					if (menu.IsOn () && menu.IsPointInside (invertedMouse))
					{
						foundMouseOverMenu = true;
					}
				}
				else if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					menu.TurnOff (true);
				}
				else if (menu.IsOn () && KickStarter.actionListManager.IsGameplayBlocked ())
				{
					menu.TurnOff (true);
				}
			}

			else if (menu.appearType == AppearType.DuringCutscene)
			{
				if (KickStarter.stateHandler.gameState == GameState.Cutscene && !menu.isLocked)
				{
					if (menu.IsOff ())
					{
						menu.TurnOn (true);
					}
					
					if (menu.IsOn () && menu.IsPointInside (invertedMouse))
					{
						foundMouseOverMenu = true;
					}
				}
				else if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					menu.TurnOff (true);
				}
				else if (menu.IsOn () && !KickStarter.actionListManager.IsGameplayBlocked ())
				{
					menu.TurnOff (true);
				}
			}
			
			else if (menu.appearType == AppearType.MouseOver)
			{
				if (KickStarter.stateHandler.gameState == GameState.Normal && !menu.isLocked && menu.IsPointInside (invertedMouse))
				{
					if (menu.IsOff ())
					{
						menu.TurnOn (true);
					}
					
					if (!menu.ignoreMouseClicks)
					{
						foundMouseOverMenu = true;
					}
				}
				else if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					menu.ForceOff ();
				}
				else
				{
					menu.TurnOff (true);
				}
			}
			
			else if (menu.appearType == AppearType.OnContainer)
			{
				if (KickStarter.playerInput.activeContainer != null && !menu.isLocked && (KickStarter.stateHandler.gameState == GameState.Normal || (KickStarter.stateHandler.gameState == AC.GameState.Paused && menu.IsBlocking ())))
				{
					if (menu.IsVisible () && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
					{
						foundMouseOverMenu = true;
					}
					menu.TurnOn (true);
				}
				else
				{
					menu.TurnOff (true);
				}
			}
			
			else if (menu.appearType == AppearType.DuringConversation)
			{
				if (KickStarter.playerInput.activeConversation != null && KickStarter.stateHandler.gameState == GameState.DialogOptions)
				{
					menu.TurnOn (true);
				}
				else if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					menu.ForceOff ();
				}
				else
				{
					menu.TurnOff (true);
				}
			}
			
			else if (menu.appearType == AppearType.OnInputKey)
			{
				if (menu.IsEnabled () && !menu.isLocked && menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
				{
					foundMouseOverMenu = true;
				}
				
				try
				{
					if (KickStarter.playerInput.InputGetButtonDown (menu.toggleKey, true))
					{
						if (!menu.IsEnabled ())
						{
							if (KickStarter.stateHandler.gameState == GameState.Paused)
							{
								CrossFade (menu);
							}
							else
							{
								menu.TurnOn (true);
							}
						}
						else
						{
							menu.TurnOff (true);
						}
					}
				}
				catch
				{
					if (KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen)
					{
						ACDebug.LogWarning ("No '" + menu.toggleKey + "' button exists - please define one in the Input Manager.");
					}
				}
			}
			
			else if (menu.appearType == AppearType.OnHotspot)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive && !menu.isLocked && KickStarter.runtimeInventory.selectedItem == null)
				{
					Hotspot hotspot = KickStarter.playerInteraction.GetActiveHotspot ();
					if (hotspot != null)
					{
						menu.HideInteractions ();
						
						if (hotspot.HasContextUse ())
						{
							menu.MatchUseInteraction (hotspot.GetFirstUseButton ());
						}
						
						if (hotspot.HasContextLook ())
						{
							menu.MatchLookInteraction (hotspot.lookButton);
						}
						
						menu.Recalculate ();
					}
				}

				if (menu.GetsDuplicated ())
				{
					if (KickStarter.stateHandler.gameState == GameState.Cutscene)
					{
						menu.ForceOff ();
					}
					else
					{
						if (menu.GetTargetInvItem () != null)
						{
							InvItem hoverItem = KickStarter.runtimeInventory.hoverItem;
							if (hoverItem != null && menu.GetTargetInvItem () == hoverItem)
							{
								menu.TurnOn (true);
							}
							else
							{
								menu.TurnOff (true);
							}
						}
						else if (menu.GetTargetHotspot () != null)
						{
							Hotspot hotspot = KickStarter.playerInteraction.GetActiveHotspot ();
							if (hotspot != null && menu.GetTargetHotspot () == hotspot)
							{
								menu.TurnOn (true);
							}
							else
							{
								menu.TurnOff (true);
							}
						}
						else
						{
							menu.TurnOff (true);
						}


						/*
						Hotspot hotspot = KickStarter.playerInteraction.GetActiveHotspot ();
						if (hotspot != null && menu.GetTargetHotspot () == hotspot)
						{
							menu.TurnOn (true);
						}
						else
						{
							menu.TurnOff (true);
						}*/
					}
				}
				else
				{
					if (hotspotLabel != "" && !menu.isLocked && KickStarter.stateHandler.gameState != GameState.Cutscene)
					{
						if (!menu.IsOn ())
						{
							menu.TurnOn (true);
							if (menu.IsUnityUI ())
							{
								// Update position before next frame (Unity UI bug)
								UpdateMenuPosition (menu, invertedMouse);
							}
						}
					}
					//else if (KickStarter.stateHandler.gameState == GameState.Paused)
					else if (KickStarter.stateHandler.gameState == GameState.Cutscene)
					{
						menu.ForceOff ();
					}
					else
					{
						menu.TurnOff (true);
					}
				}
			}
			
			else if (menu.appearType == AppearType.OnInteraction)
			{
				if (KickStarter.settingsManager.CanClickOffInteractionMenu ())
				{
					if (menu.IsEnabled () && (KickStarter.stateHandler.gameState == GameState.Normal || menu.pauseWhenEnabled))
					{
						interactionMenuIsOn = true;
						interactionMenuPauses = menu.pauseWhenEnabled;

						if (menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
						{
							foundMouseOverInteractionMenu = true;
						}
						else if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick)
						{
							if (KickStarter.settingsManager.ShouldCloseInteractionMenu ())
							{
								KickStarter.playerInput.ResetMouseClick ();
								interactionMenuIsOn = false;
								menu.TurnOff (true);
							}
						}
					}
					else if (KickStarter.stateHandler.gameState == GameState.Paused)
					{
						interactionMenuIsOn = false;
						menu.ForceOff ();
					}
					else if (KickStarter.playerInteraction.GetActiveHotspot () == null)
					{
						interactionMenuIsOn = false;
						menu.TurnOff (true);
					}
				}
				else
				{
					if (menu.IsEnabled () && (KickStarter.stateHandler.gameState == GameState.Normal || menu.pauseWhenEnabled))
					{
						if (menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks)
						{
							foundMouseOverInteractionMenu = true;
						}
						else if (!menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks && KickStarter.playerInteraction.GetActiveHotspot () == null && KickStarter.runtimeInventory.hoverItem == null &&
						    (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction || KickStarter.settingsManager.cancelInteractions == CancelInteractions.CursorLeavesMenuOrHotspot))
						{
							interactionMenuIsOn = false;
							menu.TurnOff (true);
						}
						else if (!menu.IsPointInside (invertedMouse) && !menu.ignoreMouseClicks && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.settingsManager.cancelInteractions == CancelInteractions.CursorLeavesMenu && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.ClickingMenu && !menu.IsFadingIn ())
						{
							interactionMenuIsOn = false;
							menu.TurnOff (true);
						}
						else if (KickStarter.playerInteraction.GetActiveHotspot () == null && KickStarter.runtimeInventory.hoverItem == null &&
						    KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.settingsManager.selectInteractions == AC.SelectInteractions.CyclingMenuAndClickingHotspot)
						{
							interactionMenuIsOn = false;
							menu.TurnOff (true);
						}
						else if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && KickStarter.playerInteraction.GetActiveHotspot () != null)
						{}
						else if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && KickStarter.runtimeInventory.hoverItem != null)
						{}
						else if (KickStarter.playerInteraction.GetActiveHotspot () == null || KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
						{}
						else if (KickStarter.runtimeInventory.selectedItem == null && KickStarter.playerInteraction.GetActiveHotspot () != null && KickStarter.runtimeInventory.hoverItem != null)
						{
							interactionMenuIsOn = false;
							menu.TurnOff (true);
						}
						else if (KickStarter.runtimeInventory.selectedItem != null && KickStarter.runtimeInventory.selectedItem != KickStarter.runtimeInventory.hoverItem)
						{
							interactionMenuIsOn = false;
							menu.TurnOff (true);
						}
					}
					else if (KickStarter.stateHandler.gameState == GameState.Paused)
					{
						if (menu.GetTargetInvItem () != null && menu.GetGameStateWhenTurnedOn () == GameState.Paused)
						{
							// Don't turn off the Menu if it was open for a paused Inventory
						}
						else
						{
							interactionMenuIsOn = false;
							menu.ForceOff ();
						}
					}
					else if (KickStarter.playerInteraction.GetActiveHotspot () == null)
					{
						interactionMenuIsOn = false;
						menu.TurnOff (true);
					}
				}
			}
			
			else if (menu.appearType == AppearType.WhenSpeechPlays)
			{
				if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					menu.TurnOff ();
				}
				else
				{
					Speech speech = menu.speech;
					if (!menu.oneMenuPerSpeech)
					{
						speech = KickStarter.dialog.GetLatestSpeech ();
					}

					if (speech != null && speech.MenuCanShow (menu))
					{
						if (Options.optionsData == null || (Options.optionsData != null && Options.optionsData.showSubtitles) || (KickStarter.speechManager.forceSubtitles && !KickStarter.dialog.FoundAudio ())) 
						{
							menu.TurnOn (true);
						}
						else
						{
							menu.TurnOff (true);	
						}
					}
					else
					{
						menu.TurnOff (true);
					}
				}
			}

			else if (menu.appearType == AppearType.WhileLoading)
			{
				if (KickStarter.sceneChanger.IsLoading ())
				{
					menu.TurnOn (true);
				}
				else
				{
					menu.TurnOff (true);
				}
			}
		}


		private void UpdateElements (AC.Menu menu, int languageNumber, bool justDisplay = false)
		{
			if (!menu.HasTransition () && menu.IsFading ())
			{
				// Stop until no longer "fading" so that it appears in right place
				return;
			}

			if (!menu.updateWhenFadeOut && menu.IsFadingOut ())
			{
				return;
			}

			if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard && menu.IsPointInside (KickStarter.playerInput.GetInvertedMouse ()))
			{
				elementIdentifier = menu.id.ToString ();
				mouseOverMenuName = menu;
			}

			for (int j=0; j<menu.elements.Count; j++)
			{
				if ((menu.elements[j].GetNumSlots () == 0 || !menu.elements[j].isVisible) && menu.menuSource != MenuSource.AdventureCreator)
				{
					menu.elements[j].HideAllUISlots ();
				}

				for (int i=0; i<menu.elements[j].GetNumSlots (); i++)
				{
					if (KickStarter.stateHandler.gameState == GameState.Cutscene)
					{
						menu.elements[j].PreDisplay (i, languageNumber, false);
					}
					else
					{
						menu.elements[j].PreDisplay (i, languageNumber, menu.IsPointerOverSlot (menu.elements[j], i, KickStarter.playerInput.GetInvertedMouse ()));
					}

					if (justDisplay)
					{
						return;
					}

					if (menu.IsVisible () && menu.elements[j].isVisible && menu.elements[j].isClickable)
					{
						if (i == 0 && menu.elements[j].alternativeInputButton != "")
						{
							if (KickStarter.playerInput.InputGetButtonDown (menu.elements[j].alternativeInputButton))
							{
								CheckClick (menu, menu.elements[j], i, MouseState.SingleClick);
								//	element.ProcessClick (menu, i, MouseState.SingleClick);
							}
						}
					}

					if (menu.elements[j].isVisible && SlotIsInteractive (menu, j, i))
					{
						if ((!interactionMenuIsOn || menu.appearType == AppearType.OnInteraction)
							&& (KickStarter.playerInput.GetDragState () == DragState.None || (KickStarter.playerInput.GetDragState () == DragState.Inventory && CanElementBeDroppedOnto (menu.elements[j]))))
						{
							if (KickStarter.sceneSettings && menu.elements[j].hoverSound && lastElementIdentifier != (menu.id.ToString () + menu.elements[j].ID.ToString () + i.ToString ()))
							{
								KickStarter.sceneSettings.PlayDefaultSound (menu.elements[j].hoverSound, false);
							}
							
							elementIdentifier = menu.id.ToString () + menu.elements[j].ID.ToString () + i.ToString ();
							mouseOverElementName = menu.elements[j];
							mouseOverElementSlot = i;
						}

						if (KickStarter.stateHandler.gameState != GameState.Cutscene)
						{
							if (menu.elements[j] is MenuInventoryBox)
							{
								if (KickStarter.stateHandler.gameState == GameState.Normal || KickStarter.stateHandler.gameState == GameState.Paused ||
									(KickStarter.stateHandler.gameState == GameState.DialogOptions && KickStarter.settingsManager.allowInventoryInteractionsDuringConversations))
								{
									if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Single && KickStarter.runtimeInventory.selectedItem == null)
									{
										KickStarter.playerCursor.ResetSelectedCursor ();
									}
									
									MenuInventoryBox inventoryBox = (MenuInventoryBox) menu.elements[j];
									if (inventoryBox.inventoryBoxType == AC_InventoryBoxType.HotspotBased)
									{
										if (KickStarter.cursorManager.addHotspotPrefix && !menu.ignoreMouseClicks)
										{
											if (KickStarter.runtimeInventory.hoverItem != null)
											{
												hotspotLabel = KickStarter.runtimeInventory.hoverItem.GetLabel (languageNumber);
											}
											else
											{
												hotspotLabel = KickStarter.playerInteraction.GetLabel (languageNumber);
											}
											
											if ((KickStarter.runtimeInventory.selectedItem == null && !interactionMenuIsOn) || interactionMenuIsOn)
											{
												hotspotLabel = KickStarter.runtimeInventory.GetHotspotPrefixLabel (inventoryBox.GetItem (i), inventoryBox.GetLabel (i, languageNumber), languageNumber) + hotspotLabel;
											}
										}
									}
									else
									{
										foundMouseOverInventory = true;

										if (!mouseOverInteractionMenu)
										{
											InvItem newHoverItem = inventoryBox.GetItem (i);
											KickStarter.runtimeInventory.SetHoverItem (newHoverItem, inventoryBox);

											if (oldHoverItem != newHoverItem)
											{
												KickStarter.runtimeInventory.MatchInteractions ();
												KickStarter.playerInteraction.RestoreInventoryInteraction ();
												activeInventoryBox = inventoryBox;
												activeCrafting = null;
												activeInventoryBoxMenu = menu;
												AssignHotspotToMenu (null, newHoverItem);

												if (interactionMenuIsOn)
												{
													SetInteractionMenus (false);
												}
											}
										}

										if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
										{}
										else
										{
											if (!interactionMenuIsOn)
											{
												if (inventoryBox.displayType == ConversationDisplayType.IconOnly)
												{
													if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
													{
														if (KickStarter.playerCursor.GetSelectedCursor () >= 0)
														{
															hotspotLabel = KickStarter.cursorManager.GetCursorIconFromID (KickStarter.playerCursor.GetSelectedCursorID ()).label + " " + inventoryBox.GetLabel (i, languageNumber);
														}
														else if (KickStarter.runtimeInventory.selectedItem == null)
														{
															hotspotLabel = inventoryBox.GetLabel (i, languageNumber);
														}
													}
													else
													{
														if (KickStarter.runtimeInventory.hoverItem != null && KickStarter.runtimeInventory.hoverItem == KickStarter.runtimeInventory.selectedItem)
														{
															hotspotLabel = inventoryBox.GetLabel (i, languageNumber);
														}
													}
												}
											}
											else if (KickStarter.runtimeInventory.selectedItem != null)
											{
												hotspotLabel = KickStarter.runtimeInventory.selectedItem.GetLabel (languageNumber);
											}
										}
									}
								}
							}
							else if (menu.elements[j] is MenuCrafting)
							{
								if (KickStarter.stateHandler.gameState == GameState.Normal || KickStarter.stateHandler.gameState == GameState.Paused)
								{
									MenuCrafting crafting = (MenuCrafting) menu.elements[j];
									KickStarter.runtimeInventory.SetHoverItem (crafting.GetItem (i), crafting);

									if (KickStarter.runtimeInventory.hoverItem != null)
									{
										AssignHotspotToMenu (null, KickStarter.runtimeInventory.hoverItem);

										if (!interactionMenuIsOn)
										{
											hotspotLabel = crafting.GetLabel (i, languageNumber);
										}
										else if (KickStarter.runtimeInventory.selectedItem != null)
										{
											hotspotLabel = KickStarter.runtimeInventory.selectedItem.GetLabel (languageNumber);
										}

										activeCrafting = crafting;
										activeInventoryBox = null;
										activeInventoryBoxMenu = menu;
									}

									foundMouseOverInventory = true;
								}
							}
							else if (menu.elements[j] is MenuInteraction && !menu.ignoreMouseClicks)
							{
								if (KickStarter.runtimeInventory.hoverItem != null)
								{
									hotspotLabel = KickStarter.runtimeInventory.hoverItem.GetLabel (languageNumber);
								}
								else
								{
									hotspotLabel = KickStarter.playerInteraction.GetLabel (languageNumber);
								}

								if (KickStarter.cursorManager.addHotspotPrefix && interactionMenuIsOn && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.ClickingMenu)
								{
									MenuInteraction interaction = (MenuInteraction) menu.elements[j];
									hotspotLabel = KickStarter.cursorManager.GetLabelFromID (interaction.iconID, languageNumber) + hotspotLabel;
								}
							}
							else if (menu.elements[j] is MenuDialogList)
							{
								if (KickStarter.stateHandler.gameState == GameState.DialogOptions)
								{
									MenuDialogList dialogList = (MenuDialogList) menu.elements[j];
									if (dialogList.displayType == ConversationDisplayType.IconOnly)
									{
										hotspotLabel = dialogList.GetLabel (i, languageNumber);
									}
								}
							}
							else if (menu.elements[j] is MenuButton)
							{
								MenuButton button = (MenuButton) menu.elements[j];
								if (button.hotspotLabel != "")
								{
									hotspotLabel = button.GetHotspotLabel (languageNumber);
								}
							}
						}
					}
				}
			}
		}
		
		
		public bool SlotIsInteractive (AC.Menu menu, int elementIndex, int i)
		{
			if (!menu.IsVisible () || !menu.elements[elementIndex].isClickable)
			{
				return false;
			}

			if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard)
			{
				return menu.IsPointerOverSlot (menu.elements[elementIndex], i, KickStarter.playerInput.GetInvertedMouse ());
			}
			else if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				return menu.IsPointerOverSlot (menu.elements[elementIndex], i, KickStarter.playerInput.GetInvertedMouse ());
			}
			else if (KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController)
			{
				if (KickStarter.stateHandler.gameState == GameState.Normal)
				{
					if (!KickStarter.playerInput.canKeyboardControlMenusDuringGameplay && menu.IsPointerOverSlot (menu.elements[elementIndex], i, KickStarter.playerInput.GetInvertedMouse ()))
					{
						return true;
					}
					else if (KickStarter.playerInput.canKeyboardControlMenusDuringGameplay && menu.CanPause () && !menu.pauseWhenEnabled && menu.selected_element == menu.elements[elementIndex] && menu.selected_slot == i)
					{
						return true;
					}
				}
				else if (KickStarter.stateHandler.gameState == GameState.Cutscene)
				{
					if (menu.CanClickInCutscenes () && menu.selected_element == menu.elements[elementIndex] && menu.selected_slot == i)
					{
						return true;
					}
				}
				else if (KickStarter.stateHandler.gameState == GameState.DialogOptions)
				{
					if (KickStarter.menuManager.keyboardControlWhenDialogOptions)
					{
						if (menu.selected_element == menu.elements[elementIndex] && menu.selected_slot == i)
						{
							return true;
						}
					}
					else
					{
						if (menu.IsPointerOverSlot (menu.elements[elementIndex], i, KickStarter.playerInput.GetInvertedMouse ()))
						{
							return true;
						}
					}
				}
				else if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					if (KickStarter.menuManager.keyboardControlWhenPaused)
					{
						if (menu.selected_element == menu.elements[elementIndex] && menu.selected_slot == i)
						{
							return true;
						}
					}
					else
					{
						if (menu.IsPointerOverSlot (menu.elements[elementIndex], i, KickStarter.playerInput.GetInvertedMouse ()))
						{
							return true;
						}
					}
				}
			}

			return false;
		}
		
		
		private void CheckClicks (AC.Menu menu)
		{
			if (!menu.HasTransition () && menu.IsFading ())
			{
				// Stop until no longer "fading" so that it appears in right place
				return;
			}

			if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard && menu.IsPointInside (KickStarter.playerInput.GetInvertedMouse ()))
			{
				elementIdentifier = menu.id.ToString ();
				mouseOverMenuName = menu;
				mouseOverElementName = null;
				mouseOverElementSlot = 0;
			}

			for (int j=0; j<menu.elements.Count; j++)
			{
				if (menu.elements[j].isVisible)
				{
					for (int i=0; i<menu.elements[j].GetNumSlots (); i++)
					{
						if (SlotIsInteractive (menu, j, i))
						{
							if (!menu.IsUnityUI () && KickStarter.playerInput.GetMouseState () != MouseState.Normal && (KickStarter.playerInput.GetDragState () == DragState.None || KickStarter.playerInput.GetDragState () == DragState.Menu))
							{
								if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || KickStarter.playerInput.GetMouseState () == MouseState.LetGo || KickStarter.playerInput.GetMouseState () == MouseState.RightClick)
								{
									if (menu.elements[j] is MenuInput) {}
									else DeselectInputBox ();
									
									CheckClick (menu, menu.elements[j], i, KickStarter.playerInput.GetMouseState ());
								}
								else if (KickStarter.playerInput.GetMouseState () == MouseState.HeldDown)
								{
									CheckContinuousClick (menu, menu.elements[j], i, KickStarter.playerInput.GetMouseState ());
								}
							}
							else if (menu.IsUnityUI () && KickStarter.runtimeInventory.selectedItem == null && KickStarter.settingsManager.inventoryDragDrop && KickStarter.playerInput.GetMouseState () == MouseState.HeldDown && KickStarter.playerInput.GetDragState () == DragState.None)
							{
								if (menu.elements[j] is MenuInventoryBox || menu.elements[j] is MenuCrafting)
								{
									// Begin UI drag drop
									CheckClick (menu, menu.elements[j], i, MouseState.SingleClick);
								}
							}
							else if (KickStarter.playerInteraction.IsDroppingInventory () && CanElementBeDroppedOnto (menu.elements[j]))
							{
								if (menu.IsUnityUI () && KickStarter.settingsManager.inventoryDragDrop && (menu.elements[j] is MenuInventoryBox || menu.elements[j] is MenuCrafting))
								{
									// End UI drag drop
									menu.elements[j].ProcessClick (menu, i, MouseState.SingleClick);
								}
								else
								{
									DeselectInputBox ();
									CheckClick (menu, menu.elements[j], i, MouseState.SingleClick);
								}
							}
						}
					}
				}
			}
		}


		/**
		 * Refreshes any active MenuDialogList elements, after changing the state of dialogue options.
		 */
		public void RefreshDialogueOptions ()
		{
			foreach (Menu menu in menus)
			{
				menu.RefreshDialogueOptions ();
			}
		}


		/**
		 * Updates the state of all Menus set to appear while the game is loading.
		 */
		public void UpdateLoadingMenus ()
		{
			int languageNumber = Options.GetLanguage ();

			for (int i=0; i<menus.Count; i++)
			{
				if (menus[i].appearType == AppearType.WhileLoading)
				{
					UpdateMenu (menus[i]);
					if (menus[i].IsEnabled ())
					{
						UpdateElements (menus[i], languageNumber);
					}
				}
			}
		}


		/**
		 * Checks for inputs made to all Menus.
		 * This is called every frame by StateHandler.
		 */
		public void CheckForInput ()
		{
			if (Time.time > 0f)
			{
				// Check clicks in reverse order
				for (int i=menus.Count-1; i>=0; i--)
				{
					if (menus[i].IsEnabled () && !menus[i].ignoreMouseClicks)
					{
						CheckClicks (menus[i]);
					}
				}
			}
		}
		

		/**
		 * Updates the state of all Menus.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateAllMenus ()
		{
			#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
			if (keyboard != null && selectedInputBox != null)
			{
				selectedInputBox.label = keyboard.text;
			}
			#endif

			if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
			{
				if (lastScreenWidth > 0)
				{
					RecalculateAll ();
				}
				lastScreenWidth = Screen.width;
				lastScreenHeight = Screen.height;
			}
			
			if (doResizeMenus > 0)
			{
				doResizeMenus ++;
				
				if (doResizeMenus == 4)
				{
					doResizeMenus = 0;
					for (int i=0; i<menus.Count; i++)
					{
						menus[i].Recalculate ();
						menus[i].UpdateAspectRect ();
						KickStarter.mainCamera.SetCameraRect ();
						menus[i].Recalculate ();
					}
				}
			}
			
			if (Time.time > 0f)
			{
				int languageNumber = Options.GetLanguage ();
				hotspotLabel = KickStarter.playerInteraction.GetLabel (languageNumber);

				if (!interactionMenuIsOn || !mouseOverInteractionMenu)
				{
					oldHoverItem = KickStarter.runtimeInventory.hoverItem;
					KickStarter.runtimeInventory.hoverItem = null;
				}
				
				if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					if (Time.timeScale != 0f)
					{
						KickStarter.sceneSettings.PauseGame ();
					}
				}

				foundMouseOverMenu = false;
				foundMouseOverInteractionMenu = false;
				foundMouseOverInventory = false;
				
				for (int i=0; i<menus.Count; i++)
				{
					UpdateMenu (menus[i]);
					if (menus[i].IsEnabled ())
					{
						UpdateElements (menus[i], languageNumber);
					}
				}

				for (int i=0; i<dupSpeechMenus.Count; i++)
				{
					UpdateMenu (dupSpeechMenus[i]);
					UpdateElements (dupSpeechMenus[i], languageNumber);

					if (dupSpeechMenus[i].IsOff () && KickStarter.stateHandler.gameState != GameState.Paused)
					{
						Menu oldMenu = dupSpeechMenus[i];
						dupSpeechMenus.RemoveAt (i);
						if (oldMenu.menuSource != MenuSource.AdventureCreator && oldMenu.canvas && oldMenu.canvas.gameObject != null)
						{
							DestroyImmediate (oldMenu.canvas.gameObject);
						}
						DestroyImmediate (oldMenu);
						i=0;
					}
				}

				for (int i=0; i<dupHotspotMenus.Count; i++)
				{
					UpdateMenu (dupHotspotMenus[i]);
					UpdateElements (dupHotspotMenus[i], languageNumber);

					if (dupHotspotMenus[i].IsOff () && KickStarter.stateHandler.gameState != GameState.Paused)
					{
						Menu oldMenu = dupHotspotMenus[i];
						dupHotspotMenus.RemoveAt (i);
						if (oldMenu.menuSource != MenuSource.AdventureCreator && oldMenu.canvas && oldMenu.canvas.gameObject != null)
						{
							DestroyImmediate (oldMenu.canvas.gameObject);
						}
						DestroyImmediate (oldMenu);
						i=0;
					}

				}

				mouseOverMenu = foundMouseOverMenu;
				mouseOverInteractionMenu = foundMouseOverInteractionMenu;
				mouseOverInventory = foundMouseOverInventory;

				if (lastElementIdentifier != elementIdentifier)
				{
					KickStarter.eventManager.Call_OnMouseOverMenuElement (mouseOverMenuName, mouseOverElementName, mouseOverElementSlot);
				}
				lastElementIdentifier = elementIdentifier;

				UpdateAllMenusAgain ();
			}
		}


		private void UpdateAllMenusAgain ()
		{
			// We actually need to go through menu calculations twice before displaying, to update any inter-dependencies between menus
			int languageNumber = Options.GetLanguage ();

			for (int i=0; i<menus.Count; i++)
			{
				UpdateMenu (menus[i], true);
				if (menus[i].IsEnabled ())
				{
					UpdateElements (menus[i], languageNumber, true);
				}
			}
		}
		

		/**
		 * <summary>Begins fading in the second Menu in a crossfade if the first Menu matches the supplied parameter.</summary>
		 * <param name = "_menu">The Menu to check for. If this menu is crossfading out, then it will be turned off, and the second Menu will fade in</param>
		 */
		public void CheckCrossfade (AC.Menu _menu)
		{
			if (crossFadeFrom == _menu && crossFadeTo != null)
			{
				crossFadeFrom.ForceOff ();
				crossFadeTo.TurnOn (true);
				crossFadeTo = null;
			}
		}
		

		/**
		 * <summary>Selects a MenuInput element, allowing the player to enter text into it.</summary>
		 * <param name = "input">The input box to select</param>
		 */
		public void SelectInputBox (MenuInput input)
		{
			selectedInputBox = input;
			
			// Mobile keyboard
			#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
			if (input.inputType == AC_InputType.NumbericOnly)
			{
				keyboard = TouchScreenKeyboard.Open (input.label, TouchScreenKeyboardType.NumberPad, false, false, false, false, "");
			}
			else
			{
				keyboard = TouchScreenKeyboard.Open (input.label, TouchScreenKeyboardType.ASCIICapable, false, false, false, false, "");
			}
			#endif
		}
		
		
		private void DeselectInputBox ()
		{
			if (selectedInputBox)
			{
				selectedInputBox.Deselect ();
				selectedInputBox = null;
				
				// Mobile keyboard
				#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
				if (keyboard != null)
				{
					keyboard.active = false;
					keyboard = null;
				}
				#endif
			}
		}
		
		
		private void CheckClick (AC.Menu _menu, MenuElement _element, int _slot, MouseState _mouseState)
		{
			if (_menu == null || _element == null)
			{
				return;
			}

			KickStarter.playerInput.ResetMouseClick ();

			if (_mouseState == MouseState.LetGo)
			{
				if (_menu.appearType == AppearType.OnInteraction)
				{
					if (KickStarter.settingsManager.ReleaseClickInteractions () && !KickStarter.settingsManager.CanDragCursor () && KickStarter.runtimeInventory.selectedItem == null)
					{
						_mouseState = MouseState.SingleClick;
					}
					else
					{
						_mouseState = MouseState.Normal;
					}
				}
				else if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && !KickStarter.settingsManager.CanDragCursor () && KickStarter.runtimeInventory.selectedItem == null && !(_element is MenuInventoryBox) && !(_element is MenuCrafting))
				{
					_mouseState = MouseState.SingleClick;
				}
				else
				{
					_mouseState = MouseState.Normal;
					return;
				}
			}

			if (_mouseState != MouseState.Normal)
			{
				_element.ProcessClick (_menu, _slot, _mouseState);
				PlayerMenus.ResetInventoryBoxes ();
			}
		}
		
		
		private void CheckContinuousClick (AC.Menu _menu, MenuElement _element, int _slot, MouseState _mouseState)
		{
			if (!_menu.IsClickable ())
			{
				return;
			}

			_element.ProcessContinuousClick (_menu, _mouseState);
		}


		/**
		 * <summary>Unassigns a Speech line from any temporarily-duplicated Menus. This will signal such Menus that they can be removed.</summary>
		 * <param name = "speech">The Speech line to unassign</param>
		 */
		public void RemoveSpeechFromMenu (Speech speech)
		{
			foreach (Menu menu in dupSpeechMenus)
			{
				if (menu.speech == speech)
				{
					menu.speech = null;
				}
			}
		}


		/**
		 * <summary>Duplicates any Menu set to display a single speech line.</summary>
		 * <param name = "speech">The Speech line to assign to any duplicated Menu</param>
		 */
		public void AssignSpeechToMenu (Speech speech)
		{
			foreach (Menu menu in menus)
			{
				if (menu.appearType == AppearType.WhenSpeechPlays && menu.oneMenuPerSpeech && speech.MenuCanShow (menu))
				{
					Menu dupMenu = ScriptableObject.CreateInstance <Menu>();
					dupMenu.Copy (menu, false);
					if (dupMenu.IsUnityUI ())
					{
						dupMenu.LoadUnityUI ();
					}
					dupMenu.Recalculate ();
					dupMenu.title += " (Duplicate)";
					dupMenu.SetSpeech (speech);
					dupMenu.TurnOn (true);
					dupSpeechMenus.Add (dupMenu);
				}
			}
		}


		/**
		 * <summary>Duplicates any Menu set to display a single speech line.</summary>
		 * <param name = "speech">The Speech line to assign to any duplicated Menu</param>
		 */
		public void AssignHotspotToMenu (Hotspot hotspot, InvItem invItem)
		{
			if (invItem != null)
			{
				hotspot = null;
			}

			if (hotspot != null || invItem != null)
			{
				foreach (Menu menu in menus)
				{
					if (menu.appearType == AppearType.OnHotspot && menu.GetsDuplicated ())
					{
						Menu dupMenu = ScriptableObject.CreateInstance <Menu>();
						dupMenu.Copy (menu, false);
						if (dupMenu.IsUnityUI ())
						{
							dupMenu.LoadUnityUI ();
						}
						dupMenu.Recalculate ();
						dupMenu.title += " (Duplicate)";
						dupMenu.SetHotspot (hotspot, invItem);
						dupMenu.TurnOn (true);
						dupHotspotMenus.Add (dupMenu);
					}
				}
			}
		}
		

		/**
		 * <summary>Crossfades to a Menu. Any other Menus will be turned off.</summary>
		 * <param name = "_menuTo">The Menu to crossfade to</param>
		 */
		public void CrossFade (AC.Menu _menuTo)
		{
			if (_menuTo.isLocked)
			{
				ACDebug.Log ("Cannot crossfade to menu " + _menuTo.title + " as it is locked.");
			}
			else if (!_menuTo.IsEnabled())
			{
				// Turn off all other menus
				crossFadeFrom = null;
				
				foreach (AC.Menu menu in menus)
				{
					if (menu.IsVisible ())
					{
						if (menu.appearType == AppearType.OnHotspot || menu.fadeSpeed == 0 || !menu.HasTransition ())
						{
							menu.ForceOff ();
						}
						else
						{
							menu.TurnOff (true);
							crossFadeFrom = menu;
						}
					}
					else
					{
						menu.ForceOff ();
					}
				}
				
				if (crossFadeFrom != null)
				{
					crossFadeTo = _menuTo;
				}
				else
				{
					_menuTo.TurnOn (true);
				}
			}
		}
		

		/**
		 * <summary>Shows or hides any Menus with appearType = AppearType.OnInteraction.</summary>
		 * <param name = "turnOn">If True, such Menus will be enabled. If False, they will be disabled.</param>
		 */
		public void SetInteractionMenus (bool turnOn)
		{
			SetInteractionMenus (turnOn, null, null);
		}


		private void SetInteractionMenus (bool turnOn, Hotspot _hotspotFor, InvItem _itemFor)
		{
			// Bugfix: menus sometimes being turned on and off in one frame
			if (turnOn)
			{
				KickStarter.playerInput.ResetMouseClick ();
			}

			foreach (AC.Menu _menu in menus)
			{
				if (_menu.appearType == AppearType.OnInteraction)
				{
					interactionMenuIsOn = turnOn;

					if (turnOn)
					{
						InteractionMenuData interactionMenuData = new InteractionMenuData (_menu, _hotspotFor, _itemFor);
						interactionMenuPauses = _menu.pauseWhenEnabled;

						StopCoroutine ("SwapInteractionMenu");
						StartCoroutine ("SwapInteractionMenu", interactionMenuData);
					}
					else
					{
						_menu.TurnOff (true);
					}
				}
			}
		}


		private struct InteractionMenuData
		{
			public Menu menuFor;
			public Hotspot hotspotFor;
			public InvItem itemFor;

			public InteractionMenuData (Menu _menuFor, Hotspot _hotspotFor, InvItem _itemFor)
			{
				menuFor = _menuFor;
				hotspotFor = _hotspotFor;
				itemFor = _itemFor;
			}
		}


		/**
		 * <summary>Shows any Menus with appearType = AppearType.OnInteraction, and connected to a given Hotspot.</summary>
		 * <param name = "_hotspotFor">The Hotspot to connect the Menus to.</param>
		 */
		public void EnableInteractionMenus (Hotspot hotspotFor)
		{
			SetInteractionMenus (true, hotspotFor, null);
		}


		/**
		 * <summary>Shows any Menus with appearType = AppearType.OnInteraction, and connected to a given Inventory item to.</summary>
		 * <param name = "_itemFor">The Inventory item to connect the Menus to.</param>
		 */
		public void EnableInteractionMenus (InvItem itemFor)
		{
			SetInteractionMenus (true, null, itemFor);
		}


		private IEnumerator SwapInteractionMenu (InteractionMenuData interactionMenuData)
		{
			if (interactionMenuData.itemFor == null)
			{
				interactionMenuData.itemFor = KickStarter.runtimeInventory.hoverItem;
			}
			if (interactionMenuData.hotspotFor == null)
			{
				interactionMenuData.hotspotFor = KickStarter.playerInteraction.GetActiveHotspot ();
			}

			if (interactionMenuData.itemFor != null && interactionMenuData.menuFor.GetTargetInvItem () != interactionMenuData.itemFor)
			{
				interactionMenuData.menuFor.TurnOff (true);
			}
			else if (interactionMenuData.hotspotFor  != null && interactionMenuData.menuFor.GetTargetHotspot () != interactionMenuData.hotspotFor)
			{
				interactionMenuData.menuFor.TurnOff (true);
			}

			while (interactionMenuData.menuFor.IsFading ())
			{
				yield return new WaitForFixedUpdate ();
			}

			KickStarter.playerInteraction.ResetInteractionIndex ();

			if (interactionMenuData.itemFor != null)
			{
				interactionMenuData.menuFor.MatchInteractions (interactionMenuData.itemFor, KickStarter.settingsManager.cycleInventoryCursors);
			}
			else if (interactionMenuData.hotspotFor  != null)
			{
				interactionMenuData.menuFor.MatchInteractions (interactionMenuData.hotspotFor .useButtons, KickStarter.settingsManager.cycleInventoryCursors);
			}

			interactionMenuData.menuFor.TurnOn (true);
		}


		/**
		 * Turns off any Menus with appearType = AppearType.OnHotspot.
		 */
		public void DisableHotspotMenus ()
		{
			foreach (AC.Menu _menu in menus)
			{
				if (_menu.appearType == AppearType.OnHotspot)
				{
					_menu.ForceOff ();
				}
			}
		}
		

		/**
		 * <summary>Gets the complete Hotspot label to be displayed in a MenuLabel element with labelType = AC_LabelType.Hotspot.</summary>
		 * <returns>The complete Hotspot label to be displayed in a MenuLabel element with labelType = AC_LabelType.Hotspot</returns>
		 */
		public string GetHotspotLabel ()
		{
			return hotspotLabel;
		}
		
		
		private void SetStyles (MenuElement element)
		{
			normalStyle.normal.textColor = element.fontColor;
			normalStyle.font = element.font;
			normalStyle.fontSize = element.GetFontSize ();
			normalStyle.alignment = TextAnchor.MiddleCenter;

			highlightedStyle.font = element.font;
			highlightedStyle.fontSize = element.GetFontSize ();
			highlightedStyle.normal.textColor = element.fontHighlightColor;
			highlightedStyle.normal.background = element.highlightTexture;
			highlightedStyle.alignment = TextAnchor.MiddleCenter;
		}
		
		
		private bool CanElementBeDroppedOnto (MenuElement element)
		{
			if (element is MenuInventoryBox)
			{
				MenuInventoryBox inventoryBox = (MenuInventoryBox) element;
				if (inventoryBox.inventoryBoxType == AC_InventoryBoxType.Default || inventoryBox.inventoryBoxType == AC_InventoryBoxType.Container || inventoryBox.inventoryBoxType == AC_InventoryBoxType.CustomScript)
				{
					return true;
				}
			}
			else if (element is MenuCrafting)
			{
				MenuCrafting crafting = (MenuCrafting) element;
				if (crafting.craftingType == CraftingElementType.Ingredients)
				{
					return true;
				}
			}
			
			return false;
		}
		
		
		private void OnDestroy ()
		{
			menus = null;
		}
		

		/**
		 * <summary>Gets a List of all defined Menus.</summary>
		 * <returns>A List of all defined Menus</returns>
		 */
		public static List<Menu> GetMenus ()
		{
			if (KickStarter.playerMenus)
			{
				if (KickStarter.playerMenus.menus.Count == 0 && KickStarter.menuManager != null && KickStarter.menuManager.menus.Count > 0)
				{
					ACDebug.LogError ("A custom script is calling 'PlayerMenus.GetMenus ()' before the Menus have been initialised - consider adjusting your script's Script Execution Order.");
					return null;
				}

				return KickStarter.playerMenus.menus;
			}
			return null;
		}
		

		/**
		 * <summary>Gets a Menu with a specific name.</summary>
		 * <param name = "menuName">The name (title) of the Menu to find</param>
		 * <returns>The Menu with the specific name</returns>
		 */
		public static Menu GetMenuWithName (string menuName)
		{
			if (KickStarter.playerMenus && KickStarter.playerMenus.menus != null)
			{
				if (KickStarter.playerMenus.menus.Count == 0 && KickStarter.menuManager != null && KickStarter.menuManager.menus.Count > 0)
				{
					ACDebug.LogError ("A custom script is calling 'PlayerMenus.GetMenuWithName ()' before the Menus have been initialised - consider adjusting your script's Script Execution Order.");
					return null;
				}

				for (int i=0; i<KickStarter.playerMenus.menus.Count; i++)
				{
					if (KickStarter.playerMenus.menus[i].title == menuName)
					{
						return KickStarter.playerMenus.menus[i];
					}
				}
			}
			return null;
		}
		

		/**
		 * <summary>Gets a MenuElement with a specific name.</summary>
		 * <param name = "menuName">The name (title) of the Menu to find</param>
		 * <param name = "menuElementName">The name (title) of the MenuElement with the Menu to find</param>
		 * <returns>The MenuElement with the specific name</returns>
		 */
		public static MenuElement GetElementWithName (string menuName, string menuElementName)
		{
			if (KickStarter.playerMenus && KickStarter.playerMenus.menus != null)
			{
				if (KickStarter.playerMenus.menus.Count == 0 && KickStarter.menuManager != null && KickStarter.menuManager.menus.Count > 0)
				{
					ACDebug.LogError ("A custom script is calling 'PlayerMenus.GetElementWithName ()' before the Menus have been initialised - consider adjusting your script's Script Execution Order.");
					return null;
				}

				foreach (AC.Menu menu in KickStarter.playerMenus.menus)
				{
					if (menu.title == menuName)
					{
						foreach (MenuElement menuElement in menu.elements)
						{
							if (menuElement.title == menuElementName)
							{
								return menuElement;
							}
						}
					}
				}
			}
			
			return null;
		}
		

		/**
		 * <summary>Checks if saving cannot be performed at this time.</summary>
		 * <param title = "_actionToIgnore">Any gameplay-blocking ActionList that contains this Action will be excluded from the check</param>
		 * <returns>True if saving cannot be performed at this time</returns>
		 */
		public static bool IsSavingLocked (Action _actionToIgnore = null)
		{
			if (KickStarter.stateHandler.gameState == GameState.DialogOptions)
			{
				return true;
			}

			if (KickStarter.actionListManager.IsGameplayBlocked (_actionToIgnore))
			{
				return true;
			}

			return KickStarter.playerMenus.lockSave;
		}
		

		/**
		 * Calls RecalculateSize() on all MenuInventoryBox elements.
		 */
		public static void ResetInventoryBoxes ()
		{
			if (KickStarter.playerMenus)
			{
				foreach (AC.Menu menu in KickStarter.playerMenus.menus)
				{
					foreach (MenuElement menuElement in menu.elements)
					{
						if (menuElement is MenuInventoryBox)
						{
							menuElement.RecalculateSize (menu.menuSource);
						}
					}
				}
			}
		}
		

		/**
		 * Takes the ingredients supplied to a MenuCrafting element and sets the appropriate outcome of another MenuCrafting element with craftingType = CraftingElementType.Output.
		 */
		public static void CreateRecipe ()
		{
			if (KickStarter.playerMenus)
			{
				foreach (AC.Menu menu in KickStarter.playerMenus.menus)
				{
					foreach (MenuElement menuElement in menu.elements)
					{
						if (menuElement is MenuCrafting)
						{
							MenuCrafting crafting = (MenuCrafting) menuElement;
							crafting.SetOutput (menu.menuSource, false);
						}
					}
				}
			}
		}
		

		/**
		 * <summary>Instantly turns off all Menus.</summary>
		 * <param name = "onlyPausing">If True, then only Menus with pauseWhenEnabled = True will be turned off</param>
		 */
		public static void ForceOffAllMenus (bool onlyPausing = false)
		{
			if (KickStarter.playerMenus)
			{
				foreach (AC.Menu menu in KickStarter.playerMenus.menus)
				{
					if (menu.IsEnabled ())
					{
						if (!onlyPausing || (onlyPausing && menu.IsBlocking ()))
						{
							menu.ForceOff ();
						}
					}
				}
			}
		}


		/**
		 * <summary>Simulates the clicking of a MenuElement.</summary>
		 * <param name = "menuName">The name (title) of the Menu that contains the MenuElement</param>
		 * <param name = "menuElementName">The name (title) of the MenuElement</param>
		 * <param name = "slot">The index number of the slot, if the MenuElement has multiple slots</param>
		 */
		public static void SimulateClick (string menuName, string menuElementName, int slot = 1)
		{
			if (KickStarter.playerMenus)
			{
				AC.Menu menu = PlayerMenus.GetMenuWithName (menuName);
				MenuElement element = PlayerMenus.GetElementWithName (menuName, menuElementName);
				KickStarter.playerMenus.CheckClick (menu, element, slot, MouseState.SingleClick);
			}
		}
		

		/**
		 * <summary>Simulates the clicking of a MenuElement.</summary>
		 * <param name = "menuName">The name (title) of the Menu that contains the MenuElement</param>
		 * <param name = "_element">The MenuElement</param>
		 * <param name = "slot">The index number of the slot, if the MenuElement has multiple slots</param>
		 */
		public static void SimulateClick (string menuName, MenuElement _element, int _slot = 1)
		{
			if (KickStarter.playerMenus)
			{
				AC.Menu menu = PlayerMenus.GetMenuWithName (menuName);
				KickStarter.playerMenus.CheckClick (menu, _element, _slot, MouseState.SingleClick);
			}
		}
		

		/**
		 * <summary>Checks if any Menus that pause the game are currently turned on.</summary>
		 * <param name ="excludingMenu">If assigned, this Menu will be excluded from the check</param>
		 * <returns>True if any Menus that pause the game are currently turned on</returns>
		 */
		public bool ArePauseMenusOn (Menu excludingMenu = null)
		{
			for (int i=0; i<menus.Count; i++)
			{
				if (menus[i].IsEnabled () && menus[i].IsBlocking () && (excludingMenu == null || menus[i] != excludingMenu))
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * Instantly turns off all Menus that have appearType = AppearType.WhenSpeechPlays.
		 */
		public void ForceOffSubtitles ()
		{
			foreach (AC.Menu menu in menus)
			{
				if (menu.IsEnabled () && menu.appearType == AppearType.WhenSpeechPlays)
				{
					menu.ForceOff ();
				}
			}
		}
		

		/**
		 * Recalculates the position, size and display of all Menus.
		 * This is an intensive process, and should not be called every fame.
		 */
		public void RecalculateAll ()
		{
			doResizeMenus = 1;

			// Border camera
			if (KickStarter.mainCamera)
			{
				KickStarter.mainCamera.SetCameraRect ();
			}
		}


		/**
		 * Instantly turns off all Menus that contain a MenuSaveList with savesListType = AC_SavesListType.Save.
		 */
		public void HideSaveMenus ()
		{
			foreach (AC.Menu menu in menus)
			{
				foreach (MenuElement element in menu.elements)
				{
					if (element is MenuSavesList && menu.IsManualControlled ())
					{
						MenuSavesList saveList = (MenuSavesList) element;
						if (saveList.saveListType == AC_SaveListType.Save)
						{
							menu.ForceOff ();
							break;
						}
					}
				}
			}
		}


		/**
		 * Selects the first element GameObject in a Unity UI-based Menu.
		 */
		public void FindFirstSelectedElement ()
		{
			if (eventSystem == null || menus.Count == 0)
			{
				return;
			}

			GameObject objectToSelect = null;
			for (int i=menus.Count-1; i>=0; i--)
			{
				Menu menu = menus[i];

				if (menu.IsEnabled ())
				{
					objectToSelect = menu.GetObjectToSelect ();
					if (objectToSelect != null)
					{
						break;
					}
				}
			}

			eventSystem.SetSelectedGameObject (objectToSelect);
		}


		/**
		 * <summary>Gets the ID number of the CursorIcon, defined in CursorManager, to switch to based on what MenuElement the cursor is currently over</summary>
		 * <returns>The ID number of the CursorIcon, defined in CursorManager, to switch to based on what MenuElement the cursor is currently over</returns>
		 */
		public int GetElementOverCursorID ()
		{
			return elementOverCursorID;
		}


		/**
		 * <summary>Sets the state of the manual save lock.</summary>
		 * <param name = "state">If True, then saving will be manually disabled</param>
		 */
		public void SetManualSaveLock (bool state)
		{
			lockSave = state;
		}


		/**
		 * <summary>Checks if the cursor is hovering over a Menu.</summary>
		 * <returns>True if the cursor is hovering over a Menu</returns>
		 */
		public bool IsMouseOverMenu ()
		{
			return mouseOverMenu;
		}


		/**
		 * <summary>Checks if the cursor is hovering over a Menu with appearType = AppearType.OnInteraction.</summary>
		 * <returns>True if the cursor is hovering over a Menu with appearType = AppearType.OnInteraction.</returns>
		 */
		public bool IsMouseOverInteractionMenu ()
		{
			return mouseOverInteractionMenu;
		}

		/**
		 * <summary>Checks if any Menu with appearType = AppearType.OnInteraction is on.</summary>
		 * <returns>True if any Menu with appearType = AppearType.OnInteraction is on.</returns>
		 */
		public bool IsInteractionMenuOn ()
		{
			return interactionMenuIsOn;
		}


		/**
		 * <summary>Checks if the player is currently manipulating an Interaction Menu by cycling the Interaction elements inside it.</summary>
		 * <returns>True if the player is currently manipulating an Interaction Menu by cycling the Interaction elements inside it.</returns>
		 */
		public bool IsCyclingInteractionMenu ()
		{
			if (interactionMenuIsOn && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the last-opened Menu with appearType = AppearType.OnInteraction is both open and set to pause the game.</summary>
		 * <returns>True if the last-opened Menu with appearType = AppearType.OnInteraction is both open and set to pause the game.</returns>
		 */
		public bool IsPausingInteractionMenuOn ()
		{
			if (interactionMenuIsOn)
			{
				return interactionMenuPauses;
			}
			return false;
		}


		/**
		 * Makes all Menus linked to Unity UI interactive.
		 */
		public void MakeUIInteractive ()
		{
			foreach (Menu menu in menus)
			{
				menu.MakeUIInteractive ();
			}
		}
		
		
		/**
		 * Makes all Menus linked to Unity UI non-interactive.
		 */
		public void MakeUINonInteractive ()
		{
			foreach (Menu menu in menus)
			{
				menu.MakeUINonInteractive ();
			}
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			mainData.menuLockData = CreateMenuLockData ();
			mainData.menuVisibilityData = CreateMenuVisibilityData ();
			mainData.menuElementVisibilityData = CreateMenuElementVisibilityData ();
			mainData.menuJournalData = CreateMenuJournalData ();

			return mainData;
		}
		
		
		/**
		 * <summary>Updates its own variables from a MainData class.</summary>
		 * <param name = "mainData">The MainData class to load from</param>
		 */
		public void LoadMainData (MainData mainData)
		{
			foreach (Menu menu in menus)
			{
				foreach (MenuElement element in menu.elements)
				{
					if (element is MenuInventoryBox)
					{
						MenuInventoryBox invBox = (MenuInventoryBox) element;
						invBox.ResetOffset ();
					}
				}
			}
			
			AssignMenuLocks (mainData.menuLockData);
			AssignMenuVisibility (mainData.menuVisibilityData);
			AssignMenuElementVisibility ( mainData.menuElementVisibilityData);
			AssignMenuJournals (mainData.menuJournalData);
		}


		private string CreateMenuLockData ()
		{
			System.Text.StringBuilder menuString = new System.Text.StringBuilder ();
			
			foreach (AC.Menu _menu in menus)
			{
				menuString.Append (_menu.id.ToString ());
				menuString.Append (SaveSystem.colon);
				menuString.Append (_menu.isLocked.ToString ());
				menuString.Append (SaveSystem.pipe);
			}
			
			if (menus.Count > 0)
			{
				menuString.Remove (menuString.Length-1, 1);
			}
			
			return menuString.ToString ();
		}
		
		
		private string CreateMenuVisibilityData ()
		{
			System.Text.StringBuilder menuString = new System.Text.StringBuilder ();
			bool changeMade = false;
			foreach (AC.Menu _menu in menus)
			{
				if (_menu.IsManualControlled ())
				{
					changeMade = true;
					menuString.Append (_menu.id.ToString ());
					menuString.Append (SaveSystem.colon);
					menuString.Append (_menu.IsEnabled ().ToString ());
					menuString.Append (SaveSystem.pipe);
				}
			}
			
			if (changeMade)
			{
				menuString.Remove (menuString.Length-1, 1);
			}
			return menuString.ToString ();
		}
		
		
		private string CreateMenuElementVisibilityData ()
		{
			System.Text.StringBuilder visibilityString = new System.Text.StringBuilder ();
			
			foreach (AC.Menu _menu in menus)
			{
				if (_menu.elements.Count > 0)
				{
					visibilityString.Append (_menu.id.ToString ());
					visibilityString.Append (SaveSystem.colon);
					
					foreach (MenuElement _element in _menu.elements)
					{
						visibilityString.Append (_element.ID.ToString ());
						visibilityString.Append ("=");
						visibilityString.Append (_element.isVisible.ToString ());
						visibilityString.Append ("+");
					}
					
					visibilityString.Remove (visibilityString.Length-1, 1);
					visibilityString.Append (SaveSystem.pipe);
				}
			}
			
			if (menus.Count > 0)
			{
				visibilityString.Remove (visibilityString.Length-1, 1);
			}
			
			return visibilityString.ToString ();
		}
		
		
		private string CreateMenuJournalData ()
		{
			System.Text.StringBuilder journalString = new System.Text.StringBuilder ();
			
			foreach (AC.Menu _menu in menus)
			{
				foreach (MenuElement _element in _menu.elements)
				{
					if (_element is MenuJournal)
					{
						MenuJournal journal = (MenuJournal) _element;
						journalString.Append (_menu.id.ToString ());
						journalString.Append (SaveSystem.colon);
						journalString.Append (journal.ID);
						journalString.Append (SaveSystem.colon);
						
						foreach (JournalPage page in journal.pages)
						{
							journalString.Append (page.lineID);
							journalString.Append ("*");
							journalString.Append (page.text);
							journalString.Append ("~");
						}
						
						if (journal.pages.Count > 0)
						{
							journalString.Remove (journalString.Length-1, 1);
						}
						
						journalString.Append (SaveSystem.pipe);
					}
				}
			}
			
			if (journalString.ToString () != "")
			{
				journalString.Remove (journalString.Length-1, 1);
			}
			
			return journalString.ToString ();
		}

		private void AssignMenuLocks (string menuLockData)
		{
			if (menuLockData.Length > 0)
			{
				string[] lockArray = menuLockData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in lockArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _id = 0;
					int.TryParse (chunkData[0], out _id);
					
					bool _lock = false;
					bool.TryParse (chunkData[1], out _lock);
					
					foreach (AC.Menu _menu in menus)
					{
						if (_menu.id == _id)
						{
							_menu.isLocked = _lock;
							break;
						}
					}
				}
			}
		}
		
		
		private void AssignMenuVisibility (string menuVisibilityData)
		{
			if (menuVisibilityData.Length > 0)
			{
				string[] visArray = menuVisibilityData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in visArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _id = 0;
					int.TryParse (chunkData[0], out _id);
					
					bool _lock = false;
					bool.TryParse (chunkData[1], out _lock);
					
					foreach (AC.Menu _menu in menus)
					{
						if (_menu.id == _id)
						{
							if (_menu.IsManualControlled ())
							{
								if (_lock)
								{
									_menu.TurnOn (false);
								}
								else
								{
									_menu.TurnOff (false);
								}
							}
							break;
						}
					}
				}
			}
		}
		
		
		private void AssignMenuElementVisibility (string menuElementVisibilityData)
		{
			if (menuElementVisibilityData.Length > 0)
			{
				string[] visArray = menuElementVisibilityData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in visArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _menuID = 0;
					int.TryParse (chunkData[0], out _menuID);
					
					foreach (AC.Menu _menu in menus)
					{
						if (_menu.id == _menuID)
						{
							// Found a match
							string[] perMenuData = chunkData[1].Split ("+"[0]);
							
							foreach (string perElementData in perMenuData)
							{
								string [] chunkData2 = perElementData.Split ("="[0]);
								
								int _elementID = 0;
								int.TryParse (chunkData2[0], out _elementID);
								
								bool _elementVisibility = false;
								bool.TryParse (chunkData2[1], out _elementVisibility);
								
								foreach (MenuElement _element in _menu.elements)
								{
									if (_element.ID == _elementID && _element.isVisible != _elementVisibility)
									{
										_element.isVisible = _elementVisibility;
										break;
									}
								}
							}
							
							_menu.ResetVisibleElements ();
							_menu.Recalculate ();
							break;
						}
					}
				}
			}
		}
		
		
		private void AssignMenuJournals (string menuJournalData)
		{
			if (menuJournalData.Length > 0)
			{
				string[] journalArray = menuJournalData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in journalArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int menuID = 0;
					int.TryParse (chunkData[0], out menuID);
					
					int elementID = 0;
					int.TryParse (chunkData[1], out elementID);
					
					foreach (AC.Menu _menu in menus)
					{
						if (_menu.id == menuID)
						{
							foreach (MenuElement _element in _menu.elements)
							{
								if (_element.ID == elementID && _element is MenuJournal)
								{
									MenuJournal journal = (MenuJournal) _element;
									journal.pages = new List<JournalPage>();
									journal.showPage = 1;
									
									string[] pageArray = chunkData[2].Split ("~"[0]);
									
									foreach (string chunkData2 in pageArray)
									{
										string[] chunkData3 = chunkData2.Split ("*"[0]);
										
										int lineID = -1;
										int.TryParse (chunkData3[0], out lineID);
										
										journal.pages.Add (new JournalPage (lineID, chunkData3[1]));
									}
									
									break;
								}
							}
						}
					}
				}
			}
		}

	}

}