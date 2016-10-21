/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"PlayerInteraction.cs"
 * 
 *	This script processes cursor clicks over hotspots and NPCs
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script processes Hotspot interactions.
	 * It should be placed on the GameEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_interaction.html")]
	#endif
	public class PlayerInteraction : MonoBehaviour
	{

		/** If True, then gameplay is blocked because the Player prefab is moving towards a Hotspot before the Interaction is run */
		[HideInInspector] public bool inPreInteractionCutscene = false;

		private Hotspot hotspotMovingTo;
		private Hotspot hotspot;
		private Hotspot lastHotspot = null;
		private Button button = null;
		private int interactionIndex = -1;


		/**
		 * Updates the interaction handler.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateInteraction ()
		{
			if (KickStarter.stateHandler.gameState == GameState.Normal)			
			{
				if (KickStarter.playerInput.GetDragState () == DragState.Moveable)
				{
					DeselectHotspot (true);
					return;
				}
				
				if (KickStarter.playerInput.GetMouseState () == MouseState.RightClick && KickStarter.runtimeInventory.selectedItem != null && !KickStarter.playerMenus.IsMouseOverMenu ())
				{
					if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot && KickStarter.settingsManager.cycleInventoryCursors)
					{
						// Don't respond to right-clicks
					}
					else if (KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple || KickStarter.settingsManager.rightClickInventory == RightClickInventory.DeselectsItem)
					{
						KickStarter.playerInput.ResetMouseClick ();
						KickStarter.runtimeInventory.SetNull ();
					}
					else if (KickStarter.settingsManager.rightClickInventory == RightClickInventory.ExaminesItem)
					{
						KickStarter.playerInput.ResetMouseClick ();
						KickStarter.runtimeInventory.Look (KickStarter.runtimeInventory.selectedItem);
					}
				}
				
				if (KickStarter.playerInput.IsCursorLocked () && KickStarter.settingsManager.onlyInteractWhenCursorUnlocked && KickStarter.settingsManager.IsInFirstPerson ())
				{
					DeselectHotspot (true);
					return;
				}
				
				if (!KickStarter.playerInput.IsCursorReadable ())
				{
					return;
				}

				HandleInteractionMenu ();
				
				if (KickStarter.settingsManager.playerFacesHotspots && KickStarter.player != null)
				{
					if (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction || !KickStarter.settingsManager.onlyFaceHotspotOnSelect)
					{
						if (hotspot && hotspot.playerTurnsHead)
						{
							KickStarter.player.SetHeadTurnTarget (hotspot.transform, hotspot.GetIconPosition (true), false, HeadFacing.Hotspot);
						}
						else if (button == null)
						{
							KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
						}
					}
					else if (button == null && hotspot == null && !KickStarter.playerMenus.IsInteractionMenuOn ())
					{
						KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
					}
				}
			}
			else if (KickStarter.stateHandler.gameState == GameState.Paused)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.settingsManager.selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot && KickStarter.playerMenus.IsPausingInteractionMenuOn ())
				{
					HandleInteractionMenu ();
				}
			}
		}


		private void HandleInteractionMenu ()
		{
			if (KickStarter.playerInput.GetMouseState () == MouseState.LetGo && !KickStarter.playerMenus.IsMouseOverInteractionMenu () && KickStarter.settingsManager.ReleaseClickInteractions ())
			{
				KickStarter.playerMenus.SetInteractionMenus (false);
			}

			if (KickStarter.playerInput.GetMouseState () == MouseState.LetGo && !KickStarter.playerMenus.IsMouseOverInteractionMenu () && KickStarter.settingsManager.ReleaseClickInteractions ())
			{
				KickStarter.playerMenus.SetInteractionMenus (false);
			}

			if (!KickStarter.playerMenus.IsMouseOverMenu () && Camera.main && !KickStarter.playerInput.ActiveArrowsDisablingHotspots () &&
			    KickStarter.mainCamera.IsPointInCamera (KickStarter.playerInput.GetMousePosition ()))
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
					{
						ContextSensitiveClick ();
					}
					else if (!KickStarter.playerMenus.IsMouseOverInteractionMenu ())
					{
						ChooseHotspotThenInteractionClick ();
					}
				}
				else
				{
					ContextSensitiveClick ();
				}
			}
			else 
			{
				if (KickStarter.playerMenus.IsMouseOverInteractionMenu () && KickStarter.runtimeInventory.hoverItem == null)
				{
					// Don't deselect Hotspot
					return;
				}

				DeselectHotspot (false);
			}
		}
		

		/**
		 * De-selects the current inventory item, if appropriate.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateInventory ()
		{
			if (hotspot == null && button == null && IsDroppingInventory ())
			{
				KickStarter.runtimeInventory.SetNull ();
			}
		}
		
		
		private Hotspot CheckForHotspots ()
		{
			if (!KickStarter.playerInput.IsMouseOnScreen ())
			{
				return null;
			}

			if (KickStarter.settingsManager.inventoryDragDrop && KickStarter.playerInput.GetMousePosition () == Vector2.zero)
			{
				return null;
			}

			if (KickStarter.playerInput.GetDragState () == DragState._Camera)
			{
				return null;
			}

			if (KickStarter.settingsManager.hotspotDetection == HotspotDetection.PlayerVicinity)
			{
				if (KickStarter.player != null && KickStarter.player.hotspotDetector != null)
				{
					if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct || KickStarter.settingsManager.IsInFirstPerson ())
					{
						if (KickStarter.settingsManager.hotspotsInVicinity == HotspotsInVicinity.ShowAll)
						{
							// Just highlight the nearest hotspot, but don't make it the "active" one
							KickStarter.player.hotspotDetector.HighlightAll ();
						}
						else
						{
							return (KickStarter.player.hotspotDetector.GetSelected ());
						}
					}
					else
					{
						// Just highlight the nearest hotspot, but don't make it the "active" one
						KickStarter.player.hotspotDetector.HighlightAll ();
					}
				}
				else
				{
					ACDebug.LogWarning ("Both a Player and a Hotspot Detector on that Player are required for Hotspots to be detected by 'Player Vicinity'");
				}
			}

			if (KickStarter.settingsManager && KickStarter.settingsManager.IsUnity2D ())
			{
				RaycastHit2D hit;
				if (KickStarter.mainCamera.IsOrthographic ())
				{
					hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (KickStarter.playerInput.GetMousePosition ()), Vector2.zero, KickStarter.settingsManager.navMeshRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer));
				}
				else
				{
					Vector3 pos = KickStarter.playerInput.GetMousePosition ();
					pos.z = -Camera.main.transform.position.z;
					hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (pos), Vector2.zero, KickStarter.settingsManager.navMeshRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer));
				}

				if (hit.collider != null && hit.collider.gameObject.GetComponent <Hotspot>())
				{
					Hotspot hitHotspot = hit.collider.gameObject.GetComponent <Hotspot>();
					if (KickStarter.settingsManager.hotspotDetection != HotspotDetection.PlayerVicinity)
					{
						return (hitHotspot);
					}
					else if (KickStarter.player.hotspotDetector && KickStarter.player.hotspotDetector.IsHotspotInTrigger (hitHotspot))
					{
						return (hitHotspot);
					}
				}
			}
			else
			{
				Camera _camera = Camera.main;

				if (_camera)
				{
					Ray ray = _camera.ScreenPointToRay (KickStarter.playerInput.GetMousePosition ());
					RaycastHit hit;
					
					if (Physics.Raycast (ray, out hit, KickStarter.settingsManager.hotspotRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer)))
					{
						if (hit.collider.gameObject.GetComponent <Hotspot>())
						{
							Hotspot hitHotspot = hit.collider.gameObject.GetComponent <Hotspot>();
							if (KickStarter.settingsManager.hotspotDetection != HotspotDetection.PlayerVicinity)
							{
								return (hitHotspot);
							}
							else if (KickStarter.player.hotspotDetector && KickStarter.player.hotspotDetector.IsHotspotInTrigger (hitHotspot))
							{
								return (hitHotspot);
							}
						}
					}
				}
			}
			
			return null;
		}
		
		
		private bool CanDoDoubleTap ()
		{
			if (KickStarter.runtimeInventory.selectedItem != null && KickStarter.settingsManager.inventoryDragDrop)
				return false;
			
			if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.settingsManager.doubleTapHotspots)
				return true;
			
			return false;
		}
		
		
		private void ChooseHotspotThenInteractionClick ()
		{
			if (CanDoDoubleTap ())
			{
				if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick)
				{
					ChooseHotspotThenInteractionClick_Process (true);
				}
			}
			else
			{
				ChooseHotspotThenInteractionClick_Process (false);
			}
		}
		
		
		private void ChooseHotspotThenInteractionClick_Process (bool doubleTap)
		{
			Hotspot newHotspot = CheckForHotspots ();
			if (hotspot != null && newHotspot == null)
			{
				DeselectHotspot (false);
			}
			else if (newHotspot != null)
			{
				if (newHotspot.IsSingleInteraction ())
				{
					ContextSensitiveClick ();
					return;
				}

				if (KickStarter.playerInput.GetMouseState () == MouseState.HeldDown && KickStarter.playerInput.GetDragState () == DragState.Player)
				{
					// Disable hotspots while dragging player
					DeselectHotspot (false);
				}
				else
				{
					bool clickedNew = false;
					if (newHotspot != hotspot)
					{
						clickedNew = true;
						
						if (hotspot)
						{
							hotspot.Deselect ();
							KickStarter.playerMenus.DisableHotspotMenus ();
						}
						
						/*if (hotspot != null && (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || !KickStarter.settingsManager.CanClickOffInteractionMenu ()))
						{
							KickStarter.playerMenus.SetInteractionMenus (false);
						}*/

						if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || !KickStarter.settingsManager.CanClickOffInteractionMenu ())
						{
							if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
							{
								if (hotspot == null)
								{
									KickStarter.playerMenus.SetInteractionMenus (false);
								}
							}
							if (hotspot != null)
							{
								KickStarter.playerMenus.SetInteractionMenus (false);
							}
						}
						
						lastHotspot = hotspot = newHotspot;
						hotspot.Select ();
					}

					if (hotspot)
					{
						if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick ||
						    (KickStarter.settingsManager.inventoryDragDrop && IsDroppingInventory ()) ||
						    (KickStarter.settingsManager.MouseOverForInteractionMenu () && KickStarter.runtimeInventory.hoverItem == null && KickStarter.runtimeInventory.selectedItem == null && clickedNew && !IsDroppingInventory ()))
						{
							if (KickStarter.runtimeInventory.hoverItem == null && KickStarter.playerInput.GetMouseState () == MouseState.SingleClick && 
							    KickStarter.settingsManager.MouseOverForInteractionMenu () && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.ClickingMenu &&
							    KickStarter.settingsManager.cancelInteractions != CancelInteractions.ClickOffMenu &&
							    !(KickStarter.runtimeInventory.selectedItem != null && !KickStarter.settingsManager.cycleInventoryCursors))
							{
								return;
							}
							if (KickStarter.runtimeInventory.selectedItem != null)
							{
								if (!KickStarter.settingsManager.inventoryDragDrop && clickedNew && doubleTap)
								{
									return;
								} 
								else
								{
									HandleInteraction ();
								}
							}
							else if (KickStarter.playerMenus)
							{
								if (KickStarter.settingsManager.playerFacesHotspots && KickStarter.player != null && KickStarter.settingsManager.onlyFaceHotspotOnSelect)
								{
									if (hotspot && hotspot.playerTurnsHead)
									{
										KickStarter.player.SetHeadTurnTarget (hotspot.transform, hotspot.GetIconPosition (true), false, HeadFacing.Hotspot);
									}
								}

								if (KickStarter.playerMenus.IsInteractionMenuOn () && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot)
								{
									ClickHotspotToInteract ();
									return;
								}
								
								if (clickedNew && doubleTap)
								{
									return;
								}

								KickStarter.playerMenus.SetInteractionMenus (true);
								
								if (KickStarter.settingsManager.seeInteractions == SeeInteractions.ClickOnHotspot)
								{
									if (KickStarter.settingsManager.stopPlayerOnClickHotspot && KickStarter.player)
									{
										KickStarter.player.EndPath ();
									}
									
									hotspotMovingTo = null;
									StopInteraction ();
									KickStarter.runtimeInventory.SetNull ();
								}
							}
						}
						else if (KickStarter.playerInput.GetMouseState () == MouseState.RightClick)
						{
							hotspot.Deselect ();
						}
					}
				}
			}
		}


		private void ContextSensitiveClick ()
		{
			if (CanDoDoubleTap ())
			{
				// Detect Hotspots only on mouse click
				if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick ||
				    KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick)
				{
					// Check Hotspots only when click/tap
					ContextSensitiveClick_Process (true, CheckForHotspots ());
				}
				else if (KickStarter.playerInput.GetMouseState () == MouseState.RightClick)
				{
					HandleInteraction ();
				}
			}
			else
			{
				// Always detect Hotspots
				ContextSensitiveClick_Process (false, CheckForHotspots ());
				
				if (!KickStarter.playerMenus.IsMouseOverMenu () && hotspot)
				{
					if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick || KickStarter.playerInput.GetMouseState () == MouseState.RightClick || IsDroppingInventory ())
					{
						if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot &&
						    (KickStarter.runtimeInventory.selectedItem == null || (KickStarter.runtimeInventory.selectedItem != null && KickStarter.settingsManager.cycleInventoryCursors)))
						{
							if (KickStarter.playerInput.GetMouseState () != MouseState.RightClick)
							{
								ClickHotspotToInteract ();
							}
						}
						else
						{
							HandleInteraction ();
						}
					}
				}
			}
			
		}
		
		
		private void ContextSensitiveClick_Process (bool doubleTap, Hotspot newHotspot)
		{
			if (hotspot != null && newHotspot == null)
			{
				DeselectHotspot (false);
			}
			else if (newHotspot != null)
			{
				if (KickStarter.playerInput.GetMouseState () == MouseState.HeldDown && KickStarter.playerInput.GetDragState () == DragState.Player)
				{
					// Disable hotspots while dragging player
					DeselectHotspot (false); 
				}
				else if (newHotspot != hotspot)
				{
					DeselectHotspot (false); 
					
					lastHotspot = hotspot = newHotspot;
					hotspot.Select ();

					if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
					{
						KickStarter.runtimeInventory.MatchInteractions ();
						RestoreHotspotInteraction ();
					}
				}
				else if (hotspot != null && doubleTap)
				{
					// Still work if not clicking on the active Hotspot
					HandleInteraction ();
				}
			}
		}
		

		/**
		 * <summary>De-selects the active Hotspot.</summary>
		 * <param name = "isInstant">If True, then any highlight effects being applied to the Hotspot will be instantly removed</param>
		 */
		public void DeselectHotspot (bool isInstant = false)
		{
			if (hotspot)
			{
				if (isInstant)
				{
					hotspot.DeselectInstant ();
				}
				else
				{
					hotspot.Deselect ();
				}
				hotspot = null;
			}
		}
		

		/**
		 * <summary>Checks if the active Hotspot has an enabled inventory interaction that matches the currently-selected inventory item.</summary>
		 * <returns>True if the active Hotspot has an an enabled inventory interaction that matches the currently-selected inventory item</returns>
		 */
		public bool DoesHotspotHaveInventoryInteraction ()
		{
			if (hotspot && KickStarter.runtimeInventory && KickStarter.runtimeInventory.selectedItem != null)
			{
				foreach (Button _button in hotspot.invButtons)
				{
					if (_button.invID == KickStarter.runtimeInventory.selectedItem.id && !_button.isDisabled)
					{
						return true;
					}
				}
			}
			
			return false;
		}
		
		
		private void HandleInteraction ()
		{
			if (hotspot)
			{
				if (KickStarter.settingsManager == null || KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick)
					{
						if (KickStarter.runtimeInventory.selectedItem == null && hotspot.HasContextUse ())
						{
							// Perform "Use" interaction
							ClickButton (InteractionType.Use, -1, -1);
						}
						
						else if (KickStarter.runtimeInventory.selectedItem != null)
						{
							// Perform "Use Inventory" interaction
							ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.selectedItem.id);
							
							if (KickStarter.settingsManager.inventoryDisableLeft)
							{
								KickStarter.runtimeInventory.SetNull ();
							}
						}
						
						else if (hotspot.HasContextLook () && KickStarter.cursorManager.leftClickExamine)
						{
							// Perform "Look" interaction
							ClickButton (InteractionType.Examine, -1, -1);
						}
					}
					else if (KickStarter.playerInput.GetMouseState () == MouseState.RightClick)
					{
						if (hotspot.HasContextLook () && KickStarter.runtimeInventory.selectedItem == null)
						{
							// Perform "Look" interaction
							ClickButton (InteractionType.Examine, -1, -1);
						}
					}
					else if (KickStarter.settingsManager.inventoryDragDrop && IsDroppingInventory ())
					{
						// Perform "Use Inventory" interaction (Drag n' drop mode)
						ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.selectedItem.id);
						KickStarter.runtimeInventory.SetNull ();
					}
				}
				
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.playerCursor && KickStarter.cursorManager)
				{
					if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick)
					{
						if (KickStarter.runtimeInventory.selectedItem == null && hotspot.provideUseInteraction)
						{
							// Perform "Use" interaction
							if (GetActiveHotspot () != null && GetActiveHotspot ().IsSingleInteraction ())
							{
								ClickButton (InteractionType.Use, -1, -1);
							}
							else if (KickStarter.playerCursor.GetSelectedCursor () >= 0)
							{
								ClickButton (InteractionType.Use, KickStarter.cursorManager.cursorIcons [KickStarter.playerCursor.GetSelectedCursor ()].id, -1);
							}
							else
							{
								if (KickStarter.cursorManager.allowWalkCursor && hotspot != null && hotspot.walkToMarker)
								{
									ClickHotspotToWalk (hotspot.walkToMarker.transform);
								}
							}
						}
						else if (KickStarter.runtimeInventory.selectedItem != null && KickStarter.playerCursor.GetSelectedCursor () == -2)
						{
							// Perform "Use Inventory" interaction
							KickStarter.playerCursor.ResetSelectedCursor ();
							ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.selectedItem.id);
							
							if (KickStarter.settingsManager.inventoryDisableLeft)
							{
								KickStarter.runtimeInventory.SetNull ();
							}
						}
					}
					else if (KickStarter.settingsManager.inventoryDragDrop && IsDroppingInventory ())
					{
						// Perform "Use Inventory" interaction (Drag n' drop mode)
						ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.selectedItem.id);
					}
				}
				
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.runtimeInventory.selectedItem != null && KickStarter.settingsManager.CanSelectItems (false))
					{
						if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick)
						{
							// Perform "Use Inventory" interaction
							ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.selectedItem.id);
							
							if (KickStarter.settingsManager.inventoryDisableLeft)
							{
								KickStarter.runtimeInventory.SetNull ();
							}
							return;
						}
						else if (KickStarter.settingsManager.inventoryDragDrop && IsDroppingInventory ())
						{
							// Perform "Use Inventory" interaction
							ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.selectedItem.id);
							
							KickStarter.runtimeInventory.SetNull ();
							return;
						}
					}
					else if (KickStarter.runtimeInventory.selectedItem == null && hotspot.IsSingleInteraction ())
					{
						// Perform "Use" interaction
						ClickButton (InteractionType.Use, -1, -1);
						
						if (KickStarter.settingsManager.inventoryDisableLeft)
						{
							KickStarter.runtimeInventory.SetNull ();
						}
					}
				}
			}
		}


		private void ClickHotspotToWalk (Transform walkToMarker)
		{
			StopMovingToHotspot ();
			inPreInteractionCutscene = false;
			StopCoroutine ("UseObject");
			KickStarter.playerInput.ResetMouseClick ();
			KickStarter.playerInput.ResetClick ();
			button = null;

			Vector3[] pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (KickStarter.player.transform.position, walkToMarker.position, KickStarter.player);
			KickStarter.player.MoveAlongPoints (pointArray, false);
		}
		

		/**
		 * <summary>Handles the clicking of a Hotspot, and runs the appropriate interaction based on the current cursor and inventory.</summary>
		 * <param name = "_interactionType">The type of interaction to run (Use, Examine, Inventory)</param>
		 * <param name = "selectedCursorID">The ID number of the current cursor, if _interactionType = InteractionType.Use</param>
		 * <param name = "selectedItemID">The ID number of the current inventory item (see InvItem), if _interactionType = InteractionType.Inventory</param>
		 * <param name = "clickedHotspot">The Hotspot that was clicked on</param>
		 */
		public void ClickButton (InteractionType _interactionType, int selectedCursorID, int selectedItemID, Hotspot clickedHotspot = null)
		{
			inPreInteractionCutscene = false;
			StopCoroutine ("UseObject");

			if (clickedHotspot != null)
			{
				lastHotspot = hotspot = clickedHotspot;
			}
			
			if (KickStarter.player)
			{
				KickStarter.player.EndPath ();
			}
			
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.settingsManager.selectInteractions == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				if (KickStarter.settingsManager.autoCycleWhenInteract)
				{
					SetNextInteraction ();
				}
				else
				{
					ResetInteractionIndex ();
				}
			}
			else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.autoCycleWhenInteract)
			{
				KickStarter.playerCursor.ResetSelectedCursor ();
			}

			KickStarter.playerInput.ResetMouseClick ();
			KickStarter.playerInput.ResetClick ();
			button = null;
			
			if (_interactionType == InteractionType.Use)
			{
				if (selectedCursorID == -1)
				{
					button = hotspot.GetFirstUseButton ();
				}
				else
				{
					foreach (Button _button in hotspot.useButtons)
					{
						if (_button.iconID == selectedCursorID && !_button.isDisabled)
						{
							button = _button;
							break;
						}
					}
					
					if (button == null && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
					{
						ActionListAsset _actionListAsset = KickStarter.cursorManager.GetUnhandledInteraction (selectedCursorID);
						RunUnhandledHotspotInteraction (_actionListAsset, clickedHotspot, KickStarter.cursorManager.passUnhandledHotspotAsParameter);

						KickStarter.runtimeInventory.SetNull ();
						KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
						return;
					}
				}
			}
			else if (_interactionType == InteractionType.Examine)
			{
				button = hotspot.lookButton;
			}
			else if (_interactionType == InteractionType.Inventory && selectedItemID >= 0)
			{
				foreach (Button invButton in hotspot.invButtons)
				{
					if (invButton.invID == selectedItemID && !invButton.isDisabled)
					{
						if ((KickStarter.runtimeInventory.IsGivingItem () && invButton.selectItemMode == SelectItemMode.Give) ||
						    (!KickStarter.runtimeInventory.IsGivingItem () && invButton.selectItemMode == SelectItemMode.Use))
						{
							button = invButton;
							break;
						}
					}
				}

				if (button == null && hotspot.provideUnhandledInvInteraction && hotspot.unhandledInvButton != null)
				{
					button = hotspot.unhandledInvButton;
				}
			}
			
			if (button != null && button.isDisabled)
			{
				button = null;
				
				if (_interactionType != InteractionType.Inventory)
				{
					KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
					return;
				}
			}

			KickStarter.eventManager.Call_OnInteractHotspot (hotspot, button);
			StartCoroutine ("UseObject", selectedItemID);
		}
		
		
		private IEnumerator UseObject (int selectedItemID)
		{
			bool doRun = false;
			bool doSnap = false;

			if (hotspotMovingTo == hotspot && KickStarter.playerInput.LastClickWasDouble ())
			{
				KickStarter.eventManager.Call_OnDoubleClickHotspot (hotspot);

				if (hotspotMovingTo.doubleClickingHotspot == DoubleClickingHotspot.TriggersInteractionInstantly)
				{
					doSnap = true;
				}
				else if (hotspotMovingTo.doubleClickingHotspot == DoubleClickingHotspot.MakesPlayerRun)
				{
					doRun = true;
				}
			}
			
			if (KickStarter.playerInput != null && KickStarter.playerInput.runLock == PlayerMoveLock.AlwaysWalk)
			{
				doRun = false;
			}
			
			if (KickStarter.player)
			{
				if (button != null && (button.playerAction == PlayerAction.WalkToMarker || button.playerAction == PlayerAction.WalkTo))
				{
					if (button.isBlocking)
					{
						inPreInteractionCutscene = true;
						KickStarter.stateHandler.gameState = GameState.Cutscene;
					}
					else
					{
						KickStarter.stateHandler.gameState = GameState.Normal;
					}
					hotspotMovingTo = hotspot;
				}
				else
				{
					if (button != null && button.playerAction != PlayerAction.DoNothing)
					{
						inPreInteractionCutscene = true;
						KickStarter.stateHandler.gameState = GameState.Cutscene;
					}
					else
					{
						KickStarter.stateHandler.gameState = GameState.Normal;
					}
					hotspotMovingTo = null;
				}
			}
			
			Hotspot _hotspot = hotspot;
			if (KickStarter.player == null || inPreInteractionCutscene || (button != null && button.playerAction == PlayerAction.DoNothing))
			{
				DeselectHotspot ();
			}

			if (KickStarter.player)
			{
				if (button != null && button.playerAction != PlayerAction.DoNothing)
				{
					Vector3 lookVector = Vector3.zero;
					Vector3 targetPos = _hotspot.transform.position;
					
					if (KickStarter.settingsManager.ActInScreenSpace ())
					{
						lookVector = AdvGame.GetScreenDirection (KickStarter.player.transform.position, _hotspot.transform.position);
					}
					else
					{
						lookVector = targetPos - KickStarter.player.transform.position;
						lookVector.y = 0;
					}
					
					KickStarter.player.SetLookDirection (lookVector, false);
					
					if (button.playerAction == PlayerAction.TurnToFace)
					{
						while (KickStarter.player.IsTurning ())
						{
							yield return new WaitForFixedUpdate ();			
						}
					}
					
					if (button.playerAction == PlayerAction.WalkToMarker && _hotspot.walkToMarker)
					{
						if (Vector3.Distance (KickStarter.player.transform.position, _hotspot.walkToMarker.transform.position) > KickStarter.settingsManager.GetDestinationThreshold ())
						{
							if (KickStarter.navigationManager)
							{
								Vector3[] pointArray;
								Vector3 targetPosition = _hotspot.walkToMarker.transform.position;
								
								if (KickStarter.settingsManager.ActInScreenSpace ())
								{
									targetPosition = AdvGame.GetScreenNavMesh (targetPosition);
								}
								
								pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (KickStarter.player.transform.position, targetPosition, KickStarter.player);
								KickStarter.player.MoveAlongPoints (pointArray, doRun);
								targetPos = pointArray [pointArray.Length - 1];
							}
							
							while (KickStarter.player.GetPath ())
							{
								if (doSnap)
								{
									KickStarter.player.Teleport (targetPos);
									break;
								}
								yield return new WaitForFixedUpdate ();
							}
						}
						
						if (button.faceAfter)
						{
							lookVector = _hotspot.walkToMarker.transform.forward;
							lookVector.y = 0;
							KickStarter.player.EndPath ();
							KickStarter.player.SetLookDirection (lookVector, false);
							
							while (KickStarter.player.IsTurning ())
							{
								if (doSnap)
								{
									KickStarter.player.SetLookDirection (lookVector, true);
									break;
								}
								yield return new WaitForFixedUpdate ();			
							}
						}
					}
					
					else if (button.playerAction == PlayerAction.WalkTo)
					{
						float dist = Vector3.Distance (KickStarter.player.transform.position, targetPos);
						if (_hotspot.walkToMarker)
						{
							dist = Vector3.Distance (KickStarter.player.transform.position, _hotspot.walkToMarker.transform.position);
						}

						if ((button.setProximity && dist > button.proximity) ||
							(!button.setProximity && dist > 2f))
						{
							if (KickStarter.navigationManager)
							{
								Vector3[] pointArray;
								Vector3 targetPosition = _hotspot.transform.position;
								if (_hotspot.walkToMarker)
								{
									targetPosition = _hotspot.walkToMarker.transform.position;
								}
								
								if (KickStarter.settingsManager.ActInScreenSpace ())
								{
									targetPosition = AdvGame.GetScreenNavMesh (targetPosition);
								}
								
								pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (KickStarter.player.transform.position, targetPosition, KickStarter.player);
								KickStarter.player.MoveAlongPoints (pointArray, doRun);

								if (pointArray.Length > 0)
								{
									targetPos = pointArray [pointArray.Length - 1];
								}
								else
								{
									targetPos = KickStarter.player.transform.position;
								}
							}
							
							if (button.setProximity)
							{
								button.proximity = Mathf.Max (button.proximity, 1f);
								targetPos.y = KickStarter.player.transform.position.y;
								
								while (Vector3.Distance (KickStarter.player.transform.position, targetPos) > button.proximity && KickStarter.player.GetPath ())
								{
									if (doSnap)
									{
										break;
									}
									yield return new WaitForFixedUpdate ();
								}
							}
							else
							{
								if (!doSnap)
								{
									yield return new WaitForSeconds (0.6f);
								}
							}
						}

						if (button.faceAfter)
						{
							if (KickStarter.settingsManager.ActInScreenSpace ())
							{
								lookVector = AdvGame.GetScreenDirection (KickStarter.player.transform.position, _hotspot.transform.position);
							}
							else
							{
								lookVector = _hotspot.transform.position - KickStarter.player.transform.position;
								lookVector.y = 0;
							}
							
							KickStarter.player.EndPath ();
							KickStarter.player.SetLookDirection (lookVector, false);
							
							while (KickStarter.player.IsTurning ())
							{
								if (doSnap)
								{
									KickStarter.player.SetLookDirection (lookVector, true);
								}
								yield return new WaitForFixedUpdate ();			
							}
						}
					}
				}
				else
				{
					KickStarter.player.charState = CharState.Decelerate;
				}
				
				KickStarter.player.EndPath ();
				hotspotMovingTo = null;
			}
			
			DeselectHotspot ();
			inPreInteractionCutscene = false;
			KickStarter.playerMenus.SetInteractionMenus (false);
			
			if (KickStarter.player)
			{
				KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
			}
			
			if (button == null)
			{
				// Unhandled event
				if (selectedItemID >= 0 && KickStarter.runtimeInventory.GetItem (selectedItemID) != null && KickStarter.runtimeInventory.GetItem (selectedItemID).unhandledActionList)
				{
					ActionListAsset unhandledActionList = KickStarter.runtimeInventory.GetItem (selectedItemID).unhandledActionList;
					RunUnhandledHotspotInteraction (unhandledActionList, _hotspot, KickStarter.inventoryManager.passUnhandledHotspotAsParameter);
				}
				else if (selectedItemID >= 0 && KickStarter.runtimeInventory.unhandledGive && KickStarter.runtimeInventory.IsGivingItem ())
				{
					RunUnhandledHotspotInteraction (KickStarter.runtimeInventory.unhandledGive, _hotspot, KickStarter.inventoryManager.passUnhandledHotspotAsParameter);
				}
				else if (selectedItemID >= 0 && KickStarter.runtimeInventory.unhandledHotspot && !KickStarter.runtimeInventory.IsGivingItem ())
				{
					RunUnhandledHotspotInteraction (KickStarter.runtimeInventory.unhandledHotspot, _hotspot, KickStarter.inventoryManager.passUnhandledHotspotAsParameter);
				}
				else
				{
					KickStarter.stateHandler.gameState = GameState.Normal;
					if (KickStarter.settingsManager.inventoryDragDrop)
					{
						KickStarter.runtimeInventory.SetNull ();
					}
				}
			}
			else
			{
				KickStarter.runtimeInventory.SetNull ();
				
				if (_hotspot.interactionSource == InteractionSource.AssetFile)
				{
					if (button.parameterID >= 0 && _hotspot != null)
					{
						AdvGame.RunActionListAsset (button.assetFile, button.parameterID, _hotspot.gameObject);
					}
					else
					{
						AdvGame.RunActionListAsset (button.assetFile);
					}
				}
				else if (_hotspot.interactionSource == InteractionSource.CustomScript)
				{
					if (button.customScriptObject != null && button.customScriptFunction != "")
					{
						button.customScriptObject.SendMessage (button.customScriptFunction);
					}
				}
				else if (_hotspot.interactionSource == InteractionSource.InScene)
				{
					if (button.interaction)
					{
						if (button.parameterID >= 0 && _hotspot != null)
						{
							ActionParameter parameter = button.interaction.GetParameter (button.parameterID);
							if (parameter != null && parameter.parameterType == ParameterType.GameObject)
							{
								parameter.gameObject = _hotspot.gameObject;
							}
						}

						button.interaction.Interact ();
					}
					else
					{
						KickStarter.stateHandler.gameState = GameState.Normal;
					}
				}
			}
			
			button = null;
		}


		private void RunUnhandledHotspotInteraction (ActionListAsset _actionListAsset, Hotspot _hotspot, bool optionValue)
		{
			if (KickStarter.settingsManager.inventoryDisableUnhandled)
			{
				KickStarter.runtimeInventory.SetNull ();
			}

			if (_actionListAsset != null)
			{
				if (optionValue && _hotspot != null)
				{
					AdvGame.RunActionListAsset (_actionListAsset, _hotspot.gameObject);
				}
				else
				{
					AdvGame.RunActionListAsset (_actionListAsset);	
				}
			}
		}
		

		/**
		 * <summary>Gets the current Hotspot label, which may also include the current interaction verb (e.g. "Take X") or the currently-selected item (e.g. "Use Y on Z")</summary>
		 * <param name = "languageNumber">The index number of the language to display the label in</param>
		 * <returns>The current Hotspot label, which may also include the current interaction verb (e.g. "Take X") or the currently-selected item (e.g. "Use Y on Z")</summary>
		 */
		public string GetLabel (int languageNumber)
		{
			if (KickStarter.stateHandler.gameState == GameState.DialogOptions && !KickStarter.settingsManager.allowInventoryInteractionsDuringConversations)
			{
				return "";
			}
			if (KickStarter.runtimeInventory.hoverItem != null)
			{
				return KickStarter.runtimeInventory.hoverItem.GetFullLabel (languageNumber);
			}
			else if (hotspot != null)
			{
				return hotspot.GetFullLabel (languageNumber);
			}
			else if (KickStarter.cursorManager && KickStarter.runtimeInventory.selectedItem != null && KickStarter.cursorManager.inventoryHandling != InventoryHandling.ChangeCursor && KickStarter.settingsManager.CanSelectItems (false) && KickStarter.cursorManager.onlyShowInventoryLabelOverHotspots)
			{
				return "";
			}

			// Prefix only
			return GetLabelPrefix (hotspot, KickStarter.runtimeInventory.hoverItem, languageNumber);
		}


		public string GetLabelPrefix (Hotspot _hotspot, InvItem _invItem, int languageNumber = 0)
		{
			if (_invItem != null)
			{
				_hotspot = null;
			}

			string label = "";
			if (KickStarter.cursorManager && KickStarter.runtimeInventory.selectedItem != null && KickStarter.cursorManager.inventoryHandling != InventoryHandling.ChangeCursor)
			{
				label = KickStarter.runtimeInventory.GetHotspotPrefixLabel (KickStarter.runtimeInventory.selectedItem, KickStarter.runtimeInventory.selectedItem.GetLabel (languageNumber), languageNumber, true);
			}
			else if (KickStarter.cursorManager && KickStarter.cursorManager.addHotspotPrefix)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					if (_hotspot && _hotspot.provideUseInteraction)
					{
						Button _button = _hotspot.GetFirstUseButton ();
						if (_button != null)
						{
							label = KickStarter.cursorManager.GetLabelFromID (_button.iconID, languageNumber);
						}
					}
				}
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					label = KickStarter.cursorManager.GetLabelFromID (KickStarter.playerCursor.GetSelectedCursorID (), languageNumber);
				}
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.settingsManager.selectInteractions == SelectInteractions.CyclingCursorAndClickingHotspot)
					{
						label = KickStarter.cursorManager.GetLabelFromID (KickStarter.playerCursor.GetSelectedCursorID (), languageNumber);
					}
					else if (KickStarter.settingsManager.selectInteractions == SelectInteractions.CyclingMenuAndClickingHotspot)
					{
						if (_invItem != null)
						{
							if (interactionIndex >= 0 && KickStarter.playerMenus.IsInteractionMenuOn ())
							{
								if (_invItem.interactions.Count > interactionIndex)
								{
									label = KickStarter.cursorManager.GetLabelFromID (_invItem.interactions [interactionIndex].icon.id, languageNumber);
								}
								else
								{
									// Inventory item
									int itemIndex = interactionIndex - _invItem.interactions.Count;
									if (_invItem.interactions.Count > itemIndex)
									{
										InvItem item = KickStarter.runtimeInventory.GetItem (_invItem.combineID [itemIndex]);
										if (item != null)
										{
											label = KickStarter.runtimeInventory.GetHotspotPrefixLabel (item, item.GetLabel (languageNumber), languageNumber);
										}
									}
								}
							}
						}
						else if (_hotspot != null)
						{
							if (interactionIndex >= 0 && KickStarter.playerMenus.IsInteractionMenuOn ())
							{
								if (_hotspot.useButtons.Count > interactionIndex)
								{
									label = KickStarter.cursorManager.GetLabelFromID (_hotspot.useButtons [interactionIndex].iconID, languageNumber);
								}
								else
								{
									// Inventory item
									int itemIndex = interactionIndex - _hotspot.useButtons.Count;
									if (_hotspot.invButtons.Count > itemIndex)
									{
										InvItem item = KickStarter.runtimeInventory.GetItem (_hotspot.invButtons [itemIndex].invID);
										if (item != null)
										{
											label = KickStarter.runtimeInventory.GetHotspotPrefixLabel (item, item.GetLabel (languageNumber), languageNumber);
										}
									}
								}
							}
						}
					}
				}
			}

			if (KickStarter.playerCursor.GetSelectedCursor () == -1 && KickStarter.cursorManager.addWalkPrefix && !KickStarter.playerMenus.IsInteractionMenuOn ())
			{
				label = KickStarter.runtimeLanguages.GetTranslation (KickStarter.cursorManager.walkPrefix.label, KickStarter.cursorManager.walkPrefix.lineID, languageNumber) + " ";
			}

			return label;
		}


		private void StopInteraction ()
		{
			button = null;
			inPreInteractionCutscene = false;
			StopCoroutine ("UseObject");
		}
		

		/**
		 * <summary>Gets the centre of the active Hotspot in screen space</summary>
		 * <returns>The centre of the active Hotspot in screen space</returns>
		 */
		public Vector2 GetHotspotScreenCentre ()
		{
			if (hotspot)
			{
				Vector2 screenPos = hotspot.GetIconScreenPosition ();
				return new Vector2 (screenPos.x / Screen.width, 1f - (screenPos.y / Screen.height));
			}
			return Vector2.zero;
		}


		/**
		 * <summary>Gets the centre of the last-active Hotspot in screen space</summary>
		 * <returns>The centre of the last-active Hotspot in screen space</returns>
		 */
		public Vector2 GetLastHotspotScreenCentre ()
		{
			if (GetLastOrActiveHotspot ())
			{
				Vector2 screenPos = GetLastOrActiveHotspot ().GetIconScreenPosition ();
				return new Vector2 (screenPos.x / Screen.width, 1f - (screenPos.y / Screen.height));
			}
			return Vector2.zero;
		}
		

		/**
		 * <summary>Checks if the cursor is currently over a Hotspot.</summary>
		 * <returs>True if the cursor is currently over a Hotspot</returns>
		 */
		public bool IsMouseOverHotspot ()
		{
			// Return false if we're in "Walk mode" anyway
			if (KickStarter.settingsManager && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot
			    && KickStarter.playerCursor && KickStarter.playerCursor.GetSelectedCursor () == -1)
			{
				return false;
			}
			
			if (KickStarter.settingsManager && KickStarter.settingsManager.IsUnity2D ())
			{
				RaycastHit2D hit = new RaycastHit2D ();
				
				if (KickStarter.mainCamera.IsOrthographic ())
				{
					hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (KickStarter.playerInput.GetMousePosition ()), Vector2.zero, KickStarter.settingsManager.navMeshRaycastLength);
				}
				else
				{
					Vector3 pos = KickStarter.playerInput.GetMousePosition ();
					pos.z = -Camera.main.transform.position.z;
					hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint(pos), Vector2.zero, KickStarter.settingsManager.navMeshRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer));
				}
				
				if (hit.collider != null && hit.collider.gameObject.GetComponent <Hotspot>())
				{
					return true;
				}
			}
			else
			{
				Ray ray = Camera.main.ScreenPointToRay (KickStarter.playerInput.GetMousePosition ());
				RaycastHit hit;
				
				if (Physics.Raycast (ray, out hit, KickStarter.settingsManager.hotspotRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer)))
				{
					if (hit.collider.gameObject.GetComponent <Hotspot>())
					{
						return true;
					}
				}
				
				// Include moveables in query
				if (Physics.Raycast (ray, out hit, KickStarter.settingsManager.moveableRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer)))
				{
					if (hit.collider.gameObject.GetComponent <DragBase>())
					{
						return true;
					}
				}
			}
			
			return false;
		}
		

		/**
		 * <summary>Checks if the player is de-selecting or dropping the inventory in this frame.</summary>
		 * <returns>True if the player is de-selecting or dropping the inventory in this frame</returns>
		 */
		public bool IsDroppingInventory ()
		{
			if (!KickStarter.settingsManager.CanSelectItems (false))
			{
				return false;
			}
			
			if (KickStarter.stateHandler.gameState == GameState.Cutscene || KickStarter.stateHandler.gameState == GameState.DialogOptions)
			{
				return false;
			}
			
			if (KickStarter.runtimeInventory.selectedItem == null || !KickStarter.runtimeInventory.localItems.Contains (KickStarter.runtimeInventory.selectedItem))
			{
				return false;
			}
			
			if (KickStarter.settingsManager.inventoryDragDrop && KickStarter.playerInput.GetMouseState () == MouseState.Normal && KickStarter.playerInput.GetDragState () == DragState.Inventory)
			{
				return true;
			}
			
			if (KickStarter.settingsManager.inventoryDragDrop && KickStarter.playerInput.CanClick () && KickStarter.playerInput.GetMouseState () == MouseState.Normal && KickStarter.playerInput.GetDragState () == DragState.None)
			{
				return true;
			}
			
			if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick && KickStarter.settingsManager.inventoryDisableLeft)
			{
				return true;
			}
			
			if (KickStarter.playerInput.GetMouseState () == MouseState.RightClick && KickStarter.settingsManager.rightClickInventory == RightClickInventory.DeselectsItem && (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive || KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Single))
			{
				return true;
			}
			
			return false;
		}
		

		/**
		 * <summary>Gets the active Hotspot.</summary>
		 * <returns>The active Hotspot</returns>
		 */
		public Hotspot GetActiveHotspot ()
		{
			return hotspot;
		}


		/**
		 * <summary>Gets the last Hotspot to be active, even if none is currently active.</summary>
		 * <returns>The last Hotspot to be active</returns>
		 */
		public Hotspot GetLastOrActiveHotspot ()
		{
			if (hotspot != null)
			{
				lastHotspot = hotspot;
				return hotspot;
			}
			return lastHotspot;
		}
		

		/**
		 * <summary>Gets the ID number of the current "Use" Button when the interface allows for cursors being cycled when over Hotspots or inventory items.</summary>
		 * <returns>The ID number of the current "Use" Button when the interface allows for cursors being cycled when over Hotspots or inventory items.</returns>
		 */
		public int GetActiveUseButtonIconID ()
		{
			if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				if (KickStarter.runtimeInventory.hoverItem != null && KickStarter.settingsManager.inventoryInteractions == AC.InventoryInteractions.Multiple)
				{
					if (interactionIndex == -1)
					{
						if (KickStarter.runtimeInventory.hoverItem.interactions == null || KickStarter.runtimeInventory.hoverItem.interactions.Count == 0)
						{
							return -1;
						}
						else
						{
							interactionIndex = 0;
							return 0;
						}
					}
					
					if (KickStarter.runtimeInventory.hoverItem.interactions != null && interactionIndex < KickStarter.runtimeInventory.hoverItem.interactions.Count)
					{
						return KickStarter.runtimeInventory.hoverItem.interactions [interactionIndex].icon.id;
					}
				}
				else if (GetActiveHotspot ())
				{
					if (interactionIndex == -1)
					{
						if (GetActiveHotspot ().GetFirstUseButton () == null)
						{
							return -1;
						}
						else
						{
							interactionIndex = GetActiveHotspot ().FindFirstEnabledInteraction ();
							return interactionIndex;
						}
					}
					
					if (interactionIndex < GetActiveHotspot ().useButtons.Count)
					{
						if (!GetActiveHotspot ().useButtons [interactionIndex].isDisabled)
						{
							return GetActiveHotspot ().useButtons [interactionIndex].iconID;
						}
						else
						{
							interactionIndex = -1;
							if (GetActiveHotspot ().GetFirstUseButton () == null)
							{
								return -1;
							}
							else
							{
								interactionIndex = GetActiveHotspot ().FindFirstEnabledInteraction ();
								return interactionIndex;
							}
						}
					}
				}
			}
			else if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot)
			{
				if (KickStarter.runtimeInventory.hoverItem != null && KickStarter.settingsManager.inventoryInteractions == AC.InventoryInteractions.Multiple)
				{
					if (interactionIndex == -1)
					{
						return -1;
					}
					
					if (KickStarter.runtimeInventory.hoverItem.interactions != null && interactionIndex < KickStarter.runtimeInventory.hoverItem.interactions.Count)
					{
						return KickStarter.runtimeInventory.hoverItem.interactions [interactionIndex].icon.id;
					}
				}
				else if (GetActiveHotspot ())
				{
					if (interactionIndex == -1)
					{
						if (GetActiveHotspot ().GetFirstUseButton () == null)
						{
							//return -1;
							return GetActiveHotspot ().FindFirstEnabledInteraction ();
						}
						else
						{
							interactionIndex = 0;
							return 0;
						}
					}
					
					if (interactionIndex < GetActiveHotspot ().useButtons.Count)
					{
						return GetActiveHotspot ().useButtons [interactionIndex].iconID;
					}
				}
			}
			return -1;
		}
		

		/**
		 * <summary>Gets the ID number of the current "Inventory" Button when the interface allows for cursors being cycled when over Hotspots or inventory items.</summary>
		 * <returns>The ID number of the current "Inventory" Button when the interface allows for cursors being cycled when over Hotspots or inventory items.</returns>
		 */
		public int GetActiveInvButtonID ()
		{
			if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				if (KickStarter.runtimeInventory.hoverItem != null && KickStarter.settingsManager.inventoryInteractions == AC.InventoryInteractions.Multiple)
				{
					int numInteractions = (KickStarter.runtimeInventory.hoverItem.interactions != null) ? KickStarter.runtimeInventory.hoverItem.interactions.Count : 0;
					if (interactionIndex >= numInteractions && KickStarter.runtimeInventory.matchingInvInteractions.Count > 0)
					{
						int combineIndex = KickStarter.runtimeInventory.matchingInvInteractions [interactionIndex - numInteractions];
						return KickStarter.runtimeInventory.hoverItem.combineID [combineIndex];
					}
				}
				else if (GetActiveHotspot ())
				{
					if (interactionIndex >= GetActiveHotspot ().useButtons.Count)
					{
						int matchingIndex = interactionIndex - GetActiveHotspot ().useButtons.Count;
						if (matchingIndex < KickStarter.runtimeInventory.matchingInvInteractions.Count)
						{
							return GetActiveHotspot ().invButtons [KickStarter.runtimeInventory.matchingInvInteractions [matchingIndex]].invID;
						}
					}
				}
			}
			else
			{
				// Cycle menus
				
				if (KickStarter.runtimeInventory.hoverItem != null && KickStarter.settingsManager.inventoryInteractions == AC.InventoryInteractions.Multiple)
				{
					int numInteractions = (KickStarter.runtimeInventory.hoverItem.interactions != null) ? KickStarter.runtimeInventory.hoverItem.interactions.Count : 0;
					if (interactionIndex >= numInteractions && KickStarter.runtimeInventory.matchingInvInteractions.Count > 0)
					{
						return KickStarter.runtimeInventory.hoverItem.combineID [KickStarter.runtimeInventory.matchingInvInteractions [interactionIndex - numInteractions]];
					}
				}
				else if (GetActiveHotspot ())
				{
					int matchingInvIndex = interactionIndex - GetActiveHotspot ().useButtons.Count;
					if (matchingInvIndex >= 0 && KickStarter.runtimeInventory.matchingInvInteractions.Count > matchingInvIndex)
					{
						int invButtonIndex = KickStarter.runtimeInventory.matchingInvInteractions [matchingInvIndex];
						if (GetActiveHotspot ().invButtons.Count > invButtonIndex)
						{
							return GetActiveHotspot ().invButtons [invButtonIndex].invID;
						}
					}
				}
			}
			return -1;
		}
		

		/**
		 * Cycles forward to the next available interaction for the active Hotspot or inventory item.
		 */
		public void SetNextInteraction ()
		{
			if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				if (KickStarter.runtimeInventory.selectedItem != null && KickStarter.runtimeInventory.hoverItem == null && hotspot == null)
				{
					return;
				}
				
				if (KickStarter.runtimeInventory.hoverItem != null && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Single)
				{
					return;
				}
				
				if (KickStarter.runtimeInventory.hoverItem != null)
				{
					interactionIndex = KickStarter.runtimeInventory.hoverItem.GetNextInteraction (interactionIndex, KickStarter.runtimeInventory.matchingInvInteractions.Count);
				}
				else if (GetActiveHotspot () != null)
				{
					interactionIndex = GetActiveHotspot ().GetNextInteraction (interactionIndex, KickStarter.runtimeInventory.matchingInvInteractions.Count);
				}
				
				if (!KickStarter.settingsManager.cycleInventoryCursors && GetActiveInvButtonID () >= 0)
				{
					interactionIndex = -1;
				}
				else
				{
					KickStarter.runtimeInventory.SelectItemByID (GetActiveInvButtonID (), SelectItemMode.Use);
				}
				
				if (KickStarter.runtimeInventory.hoverItem != null)
				{
					KickStarter.runtimeInventory.hoverItem.lastInteractionIndex = interactionIndex;
				}
				else if (GetActiveHotspot () != null)
				{
					GetActiveHotspot ().lastInteractionIndex = interactionIndex;
				}
			}
			else
			{
				// Cycle menus
				if (KickStarter.runtimeInventory.hoverItem != null)
				{
					interactionIndex = KickStarter.runtimeInventory.hoverItem.GetNextInteraction (interactionIndex, KickStarter.runtimeInventory.matchingInvInteractions.Count);
				}
				else if (GetActiveHotspot () != null)
				{
					if (KickStarter.settingsManager.cycleInventoryCursors)
					{
						interactionIndex = GetActiveHotspot ().GetNextInteraction (interactionIndex, KickStarter.runtimeInventory.matchingInvInteractions.Count);
					}
					else
					{
						interactionIndex = GetActiveHotspot ().GetNextInteraction (interactionIndex, 0);
					}
				}
			}
		}


		/**
		 * Cycles backward to the previous available interaction for the active Hotspot or inventory item.
		 */
		public void SetPreviousInteraction ()
		{
			if (KickStarter.runtimeInventory.hoverItem != null)
			{
				interactionIndex = KickStarter.runtimeInventory.hoverItem.GetPreviousInteraction (interactionIndex, KickStarter.runtimeInventory.matchingInvInteractions.Count);
			}
			else if (GetActiveHotspot () != null)
			{
				if (KickStarter.settingsManager.cycleInventoryCursors)
				{
					interactionIndex = GetActiveHotspot ().GetPreviousInteraction (interactionIndex, KickStarter.runtimeInventory.matchingInvInteractions.Count);
				}
				else
				{
					interactionIndex = GetActiveHotspot ().GetPreviousInteraction (interactionIndex, 0);
				}
				
				if (!KickStarter.settingsManager.cycleInventoryCursors && GetActiveInvButtonID () >= 0)
				{
					interactionIndex = -1;
				}
				else
				{
					KickStarter.runtimeInventory.SelectItemByID (GetActiveInvButtonID (), SelectItemMode.Use);
				}
				
				if (KickStarter.runtimeInventory.hoverItem != null)
				{
					KickStarter.runtimeInventory.hoverItem.lastInteractionIndex = interactionIndex;
				}
				else if (GetActiveHotspot () != null)
				{
					GetActiveHotspot ().lastInteractionIndex = interactionIndex;
				}
			}
		}
		

		/**
		 * Resets the active Hotspot or inventory item's selected interaction index.
		 * The interaction index is the position inside a combined List of the Hotspot or inventory item's enabled Use and Inventory Buttons.
		 */
		public void ResetInteractionIndex ()
		{
			interactionIndex = -1;
			
			if (GetActiveHotspot ())
			{
				interactionIndex = GetActiveHotspot ().FindFirstEnabledInteraction ();
			}
			else if (KickStarter.runtimeInventory.hoverItem != null)
			{
				interactionIndex = 0;
			}
		}
		

		/**
		 * <summary>Gets the active Hotspot's selected interaction index.
		 * The interaction index is the position inside a combined List of the Hotspot or inventory item's enabled Use and Inventory Buttons.</summary>
		 * <returns>The active Hotspot's selected interaction index</returns>
		 */
		public int GetInteractionIndex ()
		{
			return interactionIndex;
		}
		

		/**
		 * <summary>Sets the active Hotspot's selected interaction index.
		 * The interaction index is the position inside a combined List of the Hotspot or inventory item's enabled Use and Inventory Buttons.</summary>
		 * <param name = "_interactionIndex">The new interaction index</param>
		 */
		public void SetInteractionIndex (int _interactionIndex)
		{
			interactionIndex = _interactionIndex;
		}
		

		/**
		 * Restores the interaction index to the last value used by the active inventory item.
		 * The interaction index is the position inside a combined List of the inventory item's enabled Use and Inventory Buttons.
		 */
		public void RestoreInventoryInteraction ()
		{
			if (KickStarter.runtimeInventory.selectedItem != null && KickStarter.settingsManager.CanSelectItems (false))
			{
				return;
			}
			
			if (KickStarter.settingsManager.SelectInteractionMethod () != AC.SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				return;
			}
			
			if (KickStarter.runtimeInventory.hoverItem != null)
			{
				interactionIndex = KickStarter.runtimeInventory.hoverItem.lastInteractionIndex;
				if (!KickStarter.settingsManager.cycleInventoryCursors && GetActiveInvButtonID () >= 0)
				{
					interactionIndex = -1;
				}
				else
				{
					int invID = GetActiveInvButtonID ();
					if (invID >= 0)
					{
						KickStarter.runtimeInventory.SelectItemByID (invID, SelectItemMode.Use);
					}
					else if (KickStarter.settingsManager.cycleInventoryCursors && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple)
					{
						KickStarter.runtimeInventory.SetNull ();
					}
				}
			}
		}
		

		/**
		 * Restores the interaction index to the last value used by the active Hotspot.
		 * The interaction index is the position inside a combined List of the Hotspot's enabled Use and Inventory Buttons.
		 */
		private void RestoreHotspotInteraction ()
		{
			if (!KickStarter.settingsManager.cycleInventoryCursors && KickStarter.runtimeInventory.selectedItem != null)
			{
				return;
			}
			
			if (hotspot != null)
			{
				interactionIndex = hotspot.lastInteractionIndex;
				
				if (!KickStarter.settingsManager.cycleInventoryCursors && GetActiveInvButtonID () >= 0)
				{
					interactionIndex = -1;
				}
				else
				{
					int invID = GetActiveInvButtonID ();
					if (invID >= 0)
					{
						KickStarter.runtimeInventory.SelectItemByID (invID, SelectItemMode.Use);
					}
				}
			}
		}


		private void ClickHotspotToInteract ()
		{
			int invID = GetActiveInvButtonID ();
			if (invID == -1)
			{
				ClickButton (InteractionType.Use, GetActiveUseButtonIconID (), -1);
			}
			else
			{
				ClickButton (InteractionType.Inventory, -1, invID);
			}
		}
		

		/**
		 * <summary>Runs the appropriate interaction after the clicking of a MenuInteraction element.</summary>
		 * <param name = "_menu">The Menu that contains the MenuInteraction element</param>
		 * <param name = "iconID">The ID number of the "Use" icon, defined in CursorManager, that was clicked on</param>
		 */
		public void ClickInteractionIcon (AC.Menu _menu, int iconID)
		{
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
			{
				ACDebug.LogWarning ("This element is not compatible with the Context-Sensitive interaction method.");
			}
			else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
			{
				KickStarter.runtimeInventory.SetNull ();
				KickStarter.playerCursor.SetCursorFromID (iconID);
			}
			else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
			{
				if (KickStarter.settingsManager.SelectInteractionMethod () != SelectInteractions.ClickingMenu)
				{
					return;
				}
				if (_menu.GetTargetInvItem () != null)
				{
					_menu.ForceOff ();
					KickStarter.runtimeInventory.RunInteraction (iconID, _menu.GetTargetInvItem ());
				}
				else if (_menu.GetTargetHotspot ())
				{
					_menu.ForceOff ();
					ClickButton (InteractionType.Use, iconID, -1, _menu.GetTargetHotspot ());
				}
			}
		}


		/**
		 * <summary>Gets the Hotspot that the Player is moving towards.</summary>
		 * <returns>The Hotspot that the Player is moving towards</returns>
		 */
		public Hotspot GetHotspotMovingTo ()
		{
			return hotspotMovingTo;
		}


		/**
		 * Cancels the interaction process, that involves the Player prefab moving towards the Hotspot before the Interaction itself is run.
		 */
		public void StopMovingToHotspot ()
		{
			hotspotMovingTo = null;

			if (KickStarter.player)
			{
				KickStarter.player.EndPath ();
				KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
			}
			StopInteraction ();
		}
		
	}
	
}