﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"GameCamera2DDrag.cs"
 * 
 *	This GameCamera allows for panning in 2D space by clicking and dragging.
 *	It is best used in games without Player movement, as the player will still move to the click point otherwise.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/*
	 * This GameCamera allows for panning in 2D space by clicking and dragging.
	 * It is best used in games without Player movement, as the player will still move to the click point otherwise.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_game_camera2_d_drag.html")]
	#endif
	public class GameCamera2DDrag : _Camera
	{

		/** How X movement is affected (Free, Limited, Locked) */
		public RotationLock xLock;
		/** How Y movement is affected (Free, Limited, Locked) */
		public RotationLock yLock;

		/** The speed of X movement */
		public float xSpeed = 5f;
		/** The speed of Y movement */
		public float ySpeed = 5f;

		/** The acceleration of X movement */
		public float xAcceleration = 5f;
		/** The deceleration of X movement */
		public float xDeceleration = 5f;

		/** The acceleration of Y movement */
		public float yAcceleration = 5f;
		/** The deceleration of Y movement */
		public float yDeceleration = 5f;

		/** If True, then X movement will be inverted */
		public bool invertX;
		/** If True, then Y movement will be inverted */
		public bool invertY;

		/** The minimum X value, if xLock = RotationLock.Limited */
		public float minX;
		/** The maximum X value, if xLock = RotationLock.Limited */
		public float maxX;
		/** The minimum Y value, if yLock = RotationLock.Limited */
		public float minY;
		/** The maximum Y value, if yLock = RotationLock.Limited */
		public float maxY;

		/** The X offset */
		public float xOffset;
		/** The Y offset */
		public float yOffset;

		private float deltaX;
		private float deltaY;
		private float xPos;
		private float yPos;
		private Vector2 perspectiveOffset;
		private Vector3 originalPosition;

		private bool is2D;


		protected override void Awake ()
		{
			isDragControlled = true;
			targetIsPlayer = false;
			SetOriginalPosition ();

			if (KickStarter.settingsManager)
			{
				is2D = KickStarter.settingsManager.IsUnity2D ();
			}

			base.Awake ();
		}


		public override bool Is2D ()
		{
			return is2D;
		}
		

		public override void _Update ()
		{
			if (KickStarter.stateHandler.gameState != GameState.Normal)
			{
				inputMovement = Vector2.zero;
			}
			else if (KickStarter.playerInput.GetDragState () == DragState._Camera)
			{
				inputMovement = KickStarter.playerInput.GetDragVector ();
			}
			else
			{
				inputMovement = Vector2.zero;
			}

			if (xLock != RotationLock.Locked)
			{
				if (inputMovement.x == 0f)
				{
					deltaX = Mathf.Lerp (deltaX, 0f, xDeceleration * Time.deltaTime);
				}
				else
				{
					float scaleFactor = Mathf.Abs (inputMovement.x) / 1000f;

					if (inputMovement.x > 0f)
					{
						deltaX = Mathf.Lerp (deltaX, xSpeed * scaleFactor, xAcceleration * Time.deltaTime * inputMovement.x);
					}
					else if (inputMovement.x < 0f)
					{
						deltaX = Mathf.Lerp (deltaX, -xSpeed * scaleFactor, xAcceleration * Time.deltaTime * -inputMovement.x);
					}
				}
				
				if (xLock == RotationLock.Limited)
				{
					if ((invertX && deltaX > 0f) || (!invertX && deltaX < 0f))
					{
						if (maxX - xPos < 5f)
						{
							deltaX *= (maxX - xPos) / 5f;
						}
					}
					else if ((invertX && deltaX < 0f) || (!invertX && deltaX > 0f))
					{
						if (minX - xPos > -5f)
						{
							deltaX *= (minX - xPos) / -5f;
						}
					}
				}
				
				if (invertX)
				{
					xPos += deltaX / 100f;
				}
				else
				{
					xPos -= deltaX / 100f;
				}
				
				if (xLock == RotationLock.Limited)
				{
					xPos = Mathf.Clamp (xPos, minX, maxX);
				}
			}

			if (yLock != RotationLock.Locked)
			{
				if (inputMovement.y == 0f)
				{
					deltaY = Mathf.Lerp (deltaY, 0f, xDeceleration * Time.deltaTime);
				}
				else
				{
					float scaleFactor = Mathf.Abs (inputMovement.y) / 1000f;

					if (inputMovement.y > 0f)
					{
						deltaY = Mathf.Lerp (deltaY, ySpeed * scaleFactor, xAcceleration * Time.deltaTime * inputMovement.y);
					}
					else if (inputMovement.y < 0f)
					{
						deltaY = Mathf.Lerp (deltaY, -ySpeed * scaleFactor, xAcceleration * Time.deltaTime * -inputMovement.y);
					}
				}
				
				if (yLock == RotationLock.Limited)
				{
					if ((invertY && deltaY > 0f) || (!invertY && deltaY < 0f))
					{
						if (maxY - yPos < 5f)
						{
							deltaY *= (maxY - yPos) / 5f;
						}
					}
					else if ((invertY && deltaY < 0f) || (!invertY && deltaY > 0f))
					{
						if (minY - yPos > -5f)
						{
							deltaY *= (minY - yPos) / -5f;
						}
					}
				}
				
				if (invertY)
				{
					yPos += deltaY / 100f;
				}
				else
				{
					yPos -= deltaY / 100f;
				}
				
				if (yLock == RotationLock.Limited)
				{
					yPos = Mathf.Clamp (yPos, minY, maxY);
				}
			}

			if (xLock != RotationLock.Locked)
			{
				perspectiveOffset.x = xPos + xOffset;
			}
			if (yLock != RotationLock.Locked)
			{
				perspectiveOffset.y = yPos + yOffset;
			}

			SetProjection ();
		}


		private void SetProjection ()
		{
			if (!_camera.orthographic && is2D)
			{
				_camera.projectionMatrix = AdvGame.SetVanishingPoint (_camera, perspectiveOffset);
			}
			else
			{
				transform.position = new Vector3 (originalPosition.x + perspectiveOffset.x, originalPosition.y + perspectiveOffset.y, originalPosition.z);
			}
		}


		private void SetOriginalPosition ()
		{
			originalPosition = transform.position;
		}


		public override Vector2 GetPerspectiveOffset ()
		{
			return perspectiveOffset;
		}


		/**
		 * <summary>Sets the position to a specific point. This does not account for the offset, minimum or maximum values.</summary>
		 * <param name = "_position">The new position for the camera</param>
		 */
		public void SetPosition (Vector2 _position)
		{
			xPos = _position.x;
			yPos = _position.y;
		}


		/**
		 * <summary>Gets the camera's position, relative to its original position.</summary>
		 * <returns>The camera's position, relative to its original position</returns>
		 */
		public Vector2 GetPosition ()
		{
			return new Vector2 (xPos, yPos);
		}

	}

}