/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"PlayerMovement.cs"
 * 
 *	This script analyses the variables in PlayerInput, and moves the character
 *	based on the control style, defined in the SettingsManager.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script analyses the variables in PlayerInput, and moves the character based on the control style, defined in the SettingsManager.
	 * It should be placed on the GameEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_movement.html")]
	#endif
	public class PlayerMovement : MonoBehaviour
	{

		private FirstPersonCamera firstPersonCamera;


		public void OnStart ()
		{
			AssignFPCamera ();
		}


		/**
		 * Updates the first-person camera, if appropriate.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateFPCamera ()
		{
			if (firstPersonCamera != null)
			{
				firstPersonCamera._UpdateFPCamera ();
			}
		}


		/**
		 * A<summary>ssigns the first-person camera as the FirstPersonCamera component placed as a child component on the Player preab.</summary>
		 * <returns>The Transform of the FirstPersonCamera component, if one is present on the Player.</returns>
		 */
		public Transform AssignFPCamera ()
		{
			if (KickStarter.player)
			{
				firstPersonCamera = KickStarter.player.GetComponentInChildren<FirstPersonCamera>();

				if (firstPersonCamera == null && KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson && KickStarter.player.FirstPersonCamera == null)
				{
					ACDebug.LogWarning ("Could not find a FirstPersonCamera script on the Player - one is necessary for first-person movement.");
				}

				if (firstPersonCamera != null)
				{
					return firstPersonCamera.transform;
				}
			}
			return null;
		}


		/**
		 * Updates the movement handler.
		 * This is called every frame by StateHandler.
		 */
		public void UpdatePlayerMovement ()
		{
			if (KickStarter.settingsManager && KickStarter.player && KickStarter.playerInput && KickStarter.playerInteraction)
			{
				if (!KickStarter.playerInput.IsMouseOnScreen () || KickStarter.playerInput.activeArrows != null)
				{
					return;
				}

				if (KickStarter.settingsManager.disableMovementWhenInterationMenusAreOpen && KickStarter.player && KickStarter.stateHandler.gameState == GameState.Normal)
				{
					if (KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick &&
						KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
						KickStarter.settingsManager.selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot &&
						KickStarter.playerMenus.IsInteractionMenuOn ())
					{
						KickStarter.player.Halt ();
						return;
					}
				}

				if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick && !KickStarter.playerMenus.IsInteractionMenuOn () && !KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerInteraction.IsMouseOverHotspot ())
				{
					if (KickStarter.playerInteraction.GetHotspotMovingTo () != null)
					{
						KickStarter.playerInteraction.StopMovingToHotspot ();
					}

					KickStarter.playerInteraction.DeselectHotspot (false);
				}

				if (KickStarter.playerInteraction.GetHotspotMovingTo () != null && KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick && KickStarter.playerInput.GetMoveKeys () != Vector2.zero)
				{
					KickStarter.playerInteraction.StopMovingToHotspot ();
				}

				if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct)
				{
					if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
					{
						DragPlayer (true, KickStarter.playerInput.GetMoveKeys ());
					}
					else
					{
						if (KickStarter.player.GetPath () == null || !KickStarter.player.IsLockedToPath ())
						{
							// Normal gameplay
							DirectControlPlayer (false, KickStarter.playerInput.GetMoveKeys ());
						}
						else
						{
							// Move along pre-determined path
							DirectControlPlayerPath (KickStarter.playerInput.GetMoveKeys ());
						}
					}
				}

				else if (KickStarter.settingsManager.movementMethod == MovementMethod.Drag)
				{
					DragPlayer (true, KickStarter.playerInput.GetMoveKeys ());
				}

				else if (KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor)
				{
					MoveStraightToCursor ();
				}
				
				else if (KickStarter.settingsManager.movementMethod == MovementMethod.PointAndClick)
				{
					PointControlPlayer ();
				}
				
				else if (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson)
				{
					if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
					{
						if (KickStarter.settingsManager.firstPersonTouchScreen == FirstPersonTouchScreen.OneTouchToTurnAndTwoTouchesToMove)
						{
							if (Input.touchCount == 1)
							{
								FirstPersonControlPlayer ();
								DragPlayerLook ();
							}
							else
							{
								DragPlayerTouch (KickStarter.playerInput.GetMoveKeys ());
							}
						}
						else if (KickStarter.settingsManager.firstPersonTouchScreen == FirstPersonTouchScreen.OneTouchToMoveAndTurn)
						{
							FirstPersonControlPlayer ();
							DragPlayer (false, KickStarter.playerInput.GetMoveKeys ());
						}
						else if (KickStarter.settingsManager.firstPersonTouchScreen == FirstPersonTouchScreen.TouchControlsTurningOnly)
						{
							FirstPersonControlPlayer ();
							DragPlayerLook ();
						}
					}
					else
					{
						FirstPersonControlPlayer ();
						DirectControlPlayer (true, KickStarter.playerInput.GetMoveKeys ());
					}
				}
			}
		}


		// Straight to cursor functions

		private void MoveStraightToCursor ()
		{
			if (KickStarter.playerInput.AllDirectionsLocked ())
			{
				if (KickStarter.player.charState == CharState.Move)
				{
					KickStarter.player.charState = CharState.Decelerate;
				}
				return;
			}

			if (KickStarter.playerInput.GetDragState () == DragState.None)
			{
				KickStarter.playerInput.ResetDragMovement ();
				
				if (KickStarter.player.charState == CharState.Move && KickStarter.player.GetPath () == null)
				{
					KickStarter.player.charState = CharState.Decelerate;
				}
			}

			if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick && KickStarter.settingsManager.singleTapStraight)
			{
				if (KickStarter.settingsManager.singleTapStraightPathfind)
				{
					PointControlPlayer ();
					return;
				}

				Vector3 clickPoint = ClickPoint (KickStarter.playerInput.GetMousePosition ());
				Vector3 moveDirection = clickPoint - KickStarter.player.transform.position;
				
				if (clickPoint != Vector3.zero)
				{
					if (moveDirection.magnitude > KickStarter.settingsManager.destinationAccuracy)
					{
						if (KickStarter.settingsManager.IsUnity2D ())
						{
							moveDirection = new Vector3 (moveDirection.x, 0f, moveDirection.y);
						}
						
						bool run = false;
						if (moveDirection.magnitude > KickStarter.settingsManager.dragRunThreshold)
						{
							run = true;
						}
						
						if (KickStarter.playerInput.runLock == PlayerMoveLock.AlwaysRun)
						{
							run = true;
						}
						else if (KickStarter.playerInput.runLock == PlayerMoveLock.AlwaysWalk)
						{
							run = false;
						}

						List<Vector3> pointArray = new List<Vector3>();
						pointArray.Add (clickPoint);
						KickStarter.player.MoveAlongPoints (pointArray.ToArray (), run);
					}
					else
					{
						if (KickStarter.player.charState == CharState.Move)
						{
							KickStarter.player.charState = CharState.Decelerate;
						}
					}
				}
			}

			else if (KickStarter.playerInput.GetDragState () == DragState.Player && (!KickStarter.settingsManager.singleTapStraight || KickStarter.playerInput.CanClick ()))
			{
				Vector3 clickPoint = ClickPoint (KickStarter.playerInput.GetMousePosition ());
				Vector3 moveDirection = clickPoint - KickStarter.player.transform.position;

				if (clickPoint != Vector3.zero)
				{
					if (moveDirection.magnitude > KickStarter.settingsManager.destinationAccuracy)
					{
						if (KickStarter.settingsManager.IsUnity2D ())
						{
							moveDirection = new Vector3 (moveDirection.x, 0f, moveDirection.y);
						}

						bool run = false;
						if (moveDirection.magnitude > KickStarter.settingsManager.dragRunThreshold)
						{
							run = true;
						}

						if (KickStarter.playerInput.runLock == PlayerMoveLock.AlwaysRun)
						{
							run = true;
						}
						else if (KickStarter.playerInput.runLock == PlayerMoveLock.AlwaysWalk)
						{
							run = false;
						}

						KickStarter.player.isRunning = run;
						KickStarter.player.charState = CharState.Move;
						
						KickStarter.player.SetLookDirection (moveDirection, false);
						KickStarter.player.SetMoveDirectionAsForward ();
					}
					else
					{
						if (KickStarter.player.charState == CharState.Move)
						{
							KickStarter.player.charState = CharState.Decelerate;
						}
					}

					if (KickStarter.player.GetPath ())
					{
						KickStarter.player.EndPath ();
					}
				}
				else
				{
					if (KickStarter.player.charState == CharState.Move)
					{
						KickStarter.player.charState = CharState.Decelerate;
					}

					if (KickStarter.player.GetPath ())
					{
						KickStarter.player.EndPath ();
					}
				}
			}
		}


		/**
		 * <summary>Gets the point in world space that a point in screen space is above.</summary>
		 * <param name = "screenPosition">The position in screen space</returns>
		 * <param name = "onNavMesh">If True, then only objects placed on the NavMesh layer will be detected.</param>
		 * <returns>The point in world space that a point in screen space is above</returns>
		 */
		public Vector3 ClickPoint (Vector2 screenPosition, bool onNavMesh = false)
		{
			if (KickStarter.navigationManager.Is2D ())
			{
				RaycastHit2D hit;
				if (KickStarter.mainCamera.IsOrthographic ())
				{
					if (onNavMesh)
					{
						hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (new Vector2 (screenPosition.x, screenPosition.y)), Vector2.zero, KickStarter.settingsManager.navMeshRaycastLength);
					}
					else
					{
						hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (new Vector2 (screenPosition.x, screenPosition.y)), Vector2.zero, KickStarter.settingsManager.navMeshRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer));
					}
				}
				else
				{
					Vector3 pos = screenPosition;
					pos.z = KickStarter.player.transform.position.z - Camera.main.transform.position.z;
					hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint(pos), Vector2.zero);
				}
				
				if (hit.collider != null)
				{
					return hit.point;
				}
			}
			else
			{
				Ray ray = Camera.main.ScreenPointToRay (screenPosition);
				RaycastHit hit = new RaycastHit();

				if (onNavMesh)
				{
					if (KickStarter.settingsManager && KickStarter.sceneSettings && Physics.Raycast (ray, out hit, KickStarter.settingsManager.navMeshRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer)))
					{
						return hit.point;
					}
				}
				else
				{
					if (KickStarter.settingsManager && KickStarter.sceneSettings && Physics.Raycast (ray, out hit, KickStarter.settingsManager.navMeshRaycastLength))
					{
						return hit.point;
					}
				}
			}
			
			return Vector3.zero;
		}
		
		
		// Drag functions

		private void DragPlayer (bool doRotation, Vector2 moveKeys)
		{
			if (KickStarter.playerInput.GetDragState () == DragState.None)
			{
				KickStarter.playerInput.ResetDragMovement ();
				
				if (KickStarter.player.charState == CharState.Move)
				{
					if (KickStarter.playerInteraction.GetHotspotMovingTo () == null)
					{
						KickStarter.player.charState = CharState.Decelerate;
					}
				}
			}
			
			if (KickStarter.playerInput.GetDragState () == DragState.Player)
			{
				Vector3 moveDirectionInput = Vector3.zero;
				
				if (KickStarter.settingsManager.IsTopDown ())
				{
					moveDirectionInput = (moveKeys.y * Vector3.forward) + (moveKeys.x * Vector3.right);
				}
				else
				{
					moveDirectionInput = (moveKeys.y * KickStarter.mainCamera.ForwardVector ()) + (moveKeys.x * KickStarter.mainCamera.RightVector ());
				}
				
				if (KickStarter.playerInput.IsDragMoveSpeedOverWalkThreshold ())
				{
					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;
				
					if (doRotation)
					{
						KickStarter.player.SetLookDirection (moveDirectionInput, false);
						KickStarter.player.SetMoveDirectionAsForward ();
					}
					else
					{
						if (KickStarter.playerInput.GetDragVector ().y < 0f)
						{
							KickStarter.player.SetMoveDirectionAsForward ();
						}
						else
						{
							KickStarter.player.SetMoveDirectionAsBackward ();
						}
					}
				}
				else
				{
					if (KickStarter.player.charState == CharState.Move && KickStarter.playerInteraction.GetHotspotMovingTo () == null)
					{
						KickStarter.player.charState = CharState.Decelerate;
					}
				}
			}
		}


		private void DragPlayerTouch (Vector2 moveKeys)
		{
			if (KickStarter.playerInput.GetDragState () == DragState.None)
			{
				KickStarter.playerInput.ResetDragMovement ();
				
				if (KickStarter.player.charState == CharState.Move)
				{
					KickStarter.player.charState = CharState.Decelerate;
				}
			}
			
			if (KickStarter.playerInput.GetDragState () == DragState.Player)
			{
				Vector3 moveDirectionInput = (moveKeys.y * KickStarter.mainCamera.ForwardVector ()) + (moveKeys.x * KickStarter.mainCamera.RightVector ());

				if (KickStarter.playerInput.IsDragMoveSpeedOverWalkThreshold ())
				{
					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;

					KickStarter.player.SetMoveDirection (KickStarter.player.transform.position + moveDirectionInput);
				}
				else
				{
					if (KickStarter.player.charState == CharState.Move && KickStarter.playerInteraction.GetHotspotMovingTo () == null)
					{
						KickStarter.player.charState = CharState.Decelerate;
					}
				}
			}
		}


		// Direct-control functions
		
		private void DirectControlPlayer (bool isFirstPerson, Vector2 moveKeys)
		{
			KickStarter.player.CancelPathfindRecalculations ();
			if (KickStarter.settingsManager.directMovementType == DirectMovementType.RelativeToCamera)
			{
				if (moveKeys != Vector2.zero)
				{
					Vector3 moveDirectionInput = Vector3.zero;

					if (KickStarter.settingsManager.IsTopDown ())
					{
						moveDirectionInput = (moveKeys.y * Vector3.forward) + (moveKeys.x * Vector3.right);
					}
					else
					{
						if (!isFirstPerson && KickStarter.settingsManager.directMovementPerspective && KickStarter.settingsManager.cameraPerspective == CameraPerspective.ThreeD)
						{
							Vector3 forwardVector = (KickStarter.player.transform.position - Camera.main.transform.position).normalized;
							Vector3 rightVector = -Vector3.Cross (forwardVector, Camera.main.transform.up);
							moveDirectionInput = (moveKeys.y * forwardVector) + (moveKeys.x * rightVector);
						}
						else
						{
							moveDirectionInput = (moveKeys.y * KickStarter.mainCamera.ForwardVector ()) + (moveKeys.x * KickStarter.mainCamera.RightVector ());
						}
					}

					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;

					if (!KickStarter.playerInput.cameraLockSnap)
					{
						if (isFirstPerson)
						{
							KickStarter.player.SetMoveDirection (moveDirectionInput);
						}
						else
						{
							KickStarter.player.SetLookDirection (moveDirectionInput, KickStarter.settingsManager.directTurnsInstantly);
							KickStarter.player.SetMoveDirectionAsForward ();
						}
					}
				}
				else if (KickStarter.player.charState == CharState.Move && KickStarter.playerInteraction.GetHotspotMovingTo () == null)
				{
					KickStarter.player.charState = CharState.Decelerate;
					//KickStarter.player.StopTurning ();
				}
			}
			
			else if (KickStarter.settingsManager.directMovementType == DirectMovementType.TankControls)
			{
				if (KickStarter.settingsManager.magnitudeAffectsDirect || isFirstPerson)
				{
					if (moveKeys.x < 0f)
					{
						KickStarter.player.TankTurnLeft (-moveKeys.x);
					}
					else if (moveKeys.x > 0f)
					{
						KickStarter.player.TankTurnRight (moveKeys.x);
					}
					else
					{
						KickStarter.player.StopTurning ();
					}
				}
				else
				{
					if (moveKeys.x < -0.3f)
					{
						KickStarter.player.TankTurnLeft ();
					}
					else if (moveKeys.x > 0.3f)
					{
						KickStarter.player.TankTurnRight ();
					}
					else
					{
						KickStarter.player.StopTurning ();
					}
				}
				
				if (moveKeys.y > 0f)
				{
					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;
					KickStarter.player.SetMoveDirectionAsForward ();
				}
				else if (moveKeys.y < 0f)
				{
					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;
					KickStarter.player.SetMoveDirectionAsBackward ();
				}
				else if (KickStarter.player.charState == CharState.Move)
				{
					KickStarter.player.charState = CharState.Decelerate;
					KickStarter.player.SetMoveDirectionAsForward ();
				}
			}
		}


		private void DirectControlPlayerPath (Vector2 moveKeys)
		{
			if (moveKeys != Vector2.zero)
			{
				Vector3 moveDirectionInput = Vector3.zero;

				if (KickStarter.settingsManager.IsTopDown ())
				{
					moveDirectionInput = (moveKeys.y * Vector3.forward) + (moveKeys.x * Vector3.right);
				}
				else
				{
					moveDirectionInput = (moveKeys.y * KickStarter.mainCamera.ForwardVector ()) + (moveKeys.x * KickStarter.mainCamera.RightVector ());
				}

				if (Vector3.Dot (moveDirectionInput, KickStarter.player.GetMoveDirection ()) > 0f)
				{
					// Move along path, because movement keys are in the path's forward direction
					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;
				}
			}
			else
			{
				if (KickStarter.player.charState == CharState.Move)
				{
					KickStarter.player.charState = CharState.Decelerate;
				}
			}
		}
		
		
		// Point/click functions
		
		private void PointControlPlayer ()
		{
			if (KickStarter.playerInput.IsCursorLocked ())
			{
				return;
			}

			if (!KickStarter.mainCamera.IsPointInCamera (KickStarter.playerInput.GetMousePosition ()))
			{
				return;
			}

			if (KickStarter.playerInput.AllDirectionsLocked ())
			{
				if (KickStarter.player.GetPath () == null && KickStarter.player.charState == CharState.Move)
				{
					KickStarter.player.charState = CharState.Decelerate;
				}
				return;
			}

			if ((KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick) && !KickStarter.playerMenus.IsInteractionMenuOn () && !KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerInteraction.IsMouseOverHotspot () && KickStarter.playerCursor)
			{
				if (KickStarter.playerCursor.GetSelectedCursor () < 0)
				{
					if (KickStarter.settingsManager.doubleClickMovement && KickStarter.playerInput.GetMouseState () == MouseState.SingleClick)
					{
						return;
					}

					if (KickStarter.playerInput.GetDragState () == DragState.Moveable)
					{
						return;
					}

					if (KickStarter.runtimeInventory.selectedItem != null && !KickStarter.settingsManager.canMoveWhenActive)
					{
						return;
					}

					bool doubleClick = false;
					if (KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick && !KickStarter.settingsManager.doubleClickMovement)
					{
						doubleClick = true;
					}

					if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.playerMenus != null)
					{
						KickStarter.playerMenus.SetInteractionMenus (false);
					}
					if (!RaycastNavMesh (KickStarter.playerInput.GetMousePosition (), doubleClick))
					{
						// Move Ray down screen until we hit something
						Vector3 simulatedMouse = KickStarter.playerInput.GetMousePosition ();
		
						if (((int) Screen.height * KickStarter.settingsManager.walkableClickRange) > 1)
						{
							if (KickStarter.settingsManager.navMeshSearchDirection == NavMeshSearchDirection.StraightDownFromCursor)
							{
								for (float i=1f; i<Screen.height * KickStarter.settingsManager.walkableClickRange; i+=4f)
								{
									// Down
									if (RaycastNavMesh (new Vector2 (simulatedMouse.x, simulatedMouse.y - i), doubleClick))
									{
										return;
									}
								}
							}

							for (float i=1f; i<Screen.height * KickStarter.settingsManager.walkableClickRange; i+=4f)
							{
								// Up
								if (RaycastNavMesh (new Vector2 (simulatedMouse.x, simulatedMouse.y + i), doubleClick))
								{
									return;
								}
								// Down
								if (RaycastNavMesh (new Vector2 (simulatedMouse.x, simulatedMouse.y - i), doubleClick))
								{
									return;
								}
								// Left
								if (RaycastNavMesh (new Vector2 (simulatedMouse.x - i, simulatedMouse.y), doubleClick))
								{
									return;
								}
								// Right
								if (RaycastNavMesh (new Vector2 (simulatedMouse.x + i, simulatedMouse.y), doubleClick))
								{
									return;
								}
								// UpLeft
								if (RaycastNavMesh (new Vector2 (simulatedMouse.x - i, simulatedMouse.y - i), doubleClick))
								{
									return;
								}
								// UpRight
								if (RaycastNavMesh (new Vector2 (simulatedMouse.x + i, simulatedMouse.y - i), doubleClick))
								{
									return;
								}
								// DownLeft
								if (RaycastNavMesh (new Vector2 (simulatedMouse.x - i, simulatedMouse.y + i), doubleClick))
								{
									return;
								}
								// DownRight
								if (RaycastNavMesh (new Vector2 (simulatedMouse.x + i, simulatedMouse.y + i), doubleClick))
								{
									return;
								}
							}
						}
					}
				}
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.autoCycleWhenInteract)
				{
					KickStarter.playerCursor.ResetSelectedCursor ();
				}

			}
			else if (KickStarter.player.GetPath () == null && KickStarter.player.charState == CharState.Move)
			{
				KickStarter.player.charState = CharState.Decelerate;
			}
		}


		private bool ProcessHit (Vector3 hitPoint, GameObject hitObject, bool run)
		{
			if (hitObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer))
			{
				return false;
			}

			if (Vector3.Distance (hitPoint, KickStarter.player.transform.position) < KickStarter.settingsManager.GetDestinationThreshold ())
			{
				return true;
			}

			if (!run)
			{
				ShowClick (hitPoint);
			}

			if (KickStarter.playerInput.runLock == PlayerMoveLock.AlwaysRun)
			{
				run = true;
			}
			else if (KickStarter.playerInput.runLock == PlayerMoveLock.AlwaysWalk)
			{
				run = false;
			}
			else if (Vector3.Distance (hitPoint, KickStarter.player.transform.position) < KickStarter.player.runDistanceThreshold)
			{
				run = false;
			}

			Vector3[] pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (KickStarter.player.transform.position, hitPoint, KickStarter.player);
			KickStarter.player.MoveAlongPoints (pointArray, run);
			return true;
		}


		private bool RaycastNavMesh (Vector3 mousePosition, bool run)
		{
			if (KickStarter.navigationManager.Is2D ())
			{
				RaycastHit2D hit;
				if (KickStarter.mainCamera.IsOrthographic ())
				{
					hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (new Vector2 (mousePosition.x, mousePosition.y)), Vector2.zero, KickStarter.settingsManager.navMeshRaycastLength);
				}
				else
				{
					Vector3 pos = mousePosition;
					pos.z = KickStarter.player.transform.position.z - Camera.main.transform.position.z;
					hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint(pos), Vector2.zero);
				}

				if (hit.collider != null)
				{
					return ProcessHit (hit.point, hit.collider.gameObject, run);
				}
			}
			else
			{
				Ray ray = Camera.main.ScreenPointToRay (mousePosition);
				RaycastHit hit = new RaycastHit();
				
				if (KickStarter.settingsManager && KickStarter.sceneSettings && Physics.Raycast (ray, out hit, KickStarter.settingsManager.navMeshRaycastLength))
				{
					return ProcessHit (hit.point, hit.collider.gameObject, run);
				}
			}
			
			return false;
		}


		private void ShowClick (Vector3 clickPoint)
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.clickPrefab)
			{
				Destroy (GameObject.Find (KickStarter.settingsManager.clickPrefab.name + "(Clone)"));
				Instantiate (KickStarter.settingsManager.clickPrefab, clickPoint, Quaternion.identity);
			}
		}

		
		// First-person functions
		
		private void FirstPersonControlPlayer ()
		{
			if (firstPersonCamera)
			{
				Vector2 freeAim = KickStarter.playerInput.GetFreeAim ();
				if (freeAim.magnitude > KickStarter.settingsManager.dragWalkThreshold / 10f)
				{
					freeAim.Normalize ();
					freeAim *= KickStarter.settingsManager.dragWalkThreshold / 10f;
				}
				float rotationX = KickStarter.player.transform.localEulerAngles.y + freeAim.x * firstPersonCamera.sensitivity.x;
				//firstPersonCamera.rotationY -= freeAim.y * firstPersonCamera.sensitivity.y;
				firstPersonCamera.IncreasePitch (-freeAim.y);
				Quaternion rot = Quaternion.AngleAxis (rotationX, Vector3.up);
				KickStarter.player.SetRotation (rot);
			}
		}


		private void DragPlayerLook ()
		{
			if (KickStarter.playerInput.AllDirectionsLocked ())
			{
				return;
			}

			if (KickStarter.playerInput.GetMouseState () == MouseState.Normal)
			{
				return;
			}
			
			else if (!KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerMenus.IsInteractionMenuOn () && (KickStarter.playerInput.GetMouseState () == MouseState.RightClick || !KickStarter.playerInteraction.IsMouseOverHotspot ()))
			{
				if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick)
				{
					KickStarter.playerInteraction.DeselectHotspot (false);
				}
			}
		}


		private void OnDestroy ()
		{
			firstPersonCamera = null;
		}
		
	}

}