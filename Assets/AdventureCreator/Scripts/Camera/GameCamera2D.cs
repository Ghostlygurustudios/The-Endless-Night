/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"GameCamera2D.cs"
 * 
 *	This GameCamera allows scrolling horizontally and vertically without altering perspective.
 *	Based on the work by Eric Haines (Eric5h5) at http://wiki.unity3d.com/index.php?title=OffsetVanishingPoint
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * The standard 2D camera. It can be scrolled horizontally and vertically without altering perspective (causing a "Ken Burns effect" if the camera uses Perspective projection.
	 * Based on the work by Eric Haines (Eric5h5) at http://wiki.unity3d.com/index.php?title=OffsetVanishingPoint
	 */
	[RequireComponent (typeof (Camera))]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_game_camera2_d.html")]
	#endif
	public class GameCamera2D : _Camera
	{

		/** If True, then horizontal panning is prevented */
		public bool lockHorizontal = true;
		/** If True, then vertical panning is prevented */
		public bool lockVertical = true;

		/** If True, then horizontal panning will be limited to minimum and maximum values */
		public bool limitHorizontal;
		/** If True, then vertical panning will be limited to minimum and maximum values */
		public bool limitVertical;

		/** The lower and upper horizontal limits, if limitHorizontal = True */
		public Vector2 constrainHorizontal;
		/** The lower and upper vertical limits, if limitVertical = True */
		public Vector2 constrainVertical;

		/** The amount of freedom when tracking a target. Higher values will result in looser tracking */
		public Vector2 freedom = Vector2.zero;
		/** The follow speed when tracking a target */
		public float dampSpeed = 0.9f;

		/** The influence that the target's facing direction has on the tracking position */
		public Vector2 directionInfluence = Vector2.zero;
		/** The intended horizontal and vertical panning offsets */
		public Vector2 afterOffset = Vector2.zero;
		
		private Vector2 perspectiveOffset = Vector2.zero;
		private Vector2 originalPosition = Vector2.zero;
		private Vector2 desiredOffset = Vector2.zero;
		private bool haveSetOriginalPosition = false;

		private float yDot;

		
		protected override void Awake ()
		{
			SetOriginalPosition ();
			base.Awake ();
		}
		
		
		private void Start ()
		{
			ResetTarget ();
			
			if (target)
			{
				MoveCameraInstant ();
			}
		}


		public override bool Is2D ()
		{
			return true;
		}


		public override void _Update ()
		{
			MoveCamera ();
		}


		/**
		 * <summary>Switches the camera's target.</summary>
		 * <param name = "_target">The new target</param>
		 */
		public void SwitchTarget (Transform _target)
		{
			target = _target;
		}
				

		private void SetDesired ()
		{
			Vector2 targetOffset = GetOffsetForPosition (target.transform.position);
			if (targetOffset.x < (perspectiveOffset.x - freedom.x))
			{
				desiredOffset.x = targetOffset.x + freedom.x;
			}
			else if (targetOffset.x > (perspectiveOffset.x + freedom.x))
			{
				desiredOffset.x = targetOffset.x - freedom.x;
			}

			desiredOffset.x += afterOffset.x;
			if (directionInfluence.x != 0f)
			{
				desiredOffset.x += Vector3.Dot (target.forward, transform.right) * directionInfluence.x;
			}

			if (limitHorizontal)
			{
				desiredOffset.x = ConstrainAxis (desiredOffset.x, constrainHorizontal);
			}
			
			if (targetOffset.y < (perspectiveOffset.y - freedom.y))
			{
				desiredOffset.y = targetOffset.y + freedom.y;
			}
			else if (targetOffset.y > (perspectiveOffset.y + freedom.y))
			{
				desiredOffset.y = targetOffset.y - freedom.y;
			}
			
			desiredOffset.y += afterOffset.y;
			if (directionInfluence.y != 0f)
			{
				if (KickStarter.settingsManager.movingTurning == MovingTurning.TopDown)
				{
					desiredOffset.y += Vector3.Dot (target.forward, transform.up) * directionInfluence.y;
				}
				else
				{
					desiredOffset.y += Vector3.Dot (target.forward, transform.forward) * directionInfluence.y;
				}
			}

			if (limitVertical)
			{
				desiredOffset.y = ConstrainAxis (desiredOffset.y, constrainVertical);
			}
		}	
		
		
		private void MoveCamera ()
		{
			if (targetIsPlayer && KickStarter.player)
			{
				target = KickStarter.player.transform;
			}
			
			if (target && (!lockHorizontal || !lockVertical))
			{
				SetDesired ();
			
				if (!lockHorizontal)
				{
					perspectiveOffset.x = Mathf.Lerp (perspectiveOffset.x, desiredOffset.x, Time.deltaTime * dampSpeed);
				}
				
				if (!lockVertical)
				{
					perspectiveOffset.y = Mathf.Lerp (perspectiveOffset.y, desiredOffset.y, Time.deltaTime * dampSpeed);
				}
			}
			else if (!GetComponent <Camera>().orthographic)
			{
				SnapToOffset ();
			}
			
			SetProjection ();
		}


		private void SetOriginalPosition ()
		{
			if (!haveSetOriginalPosition)
			{
				originalPosition = new Vector2 (transform.position.x, transform.position.y);
				haveSetOriginalPosition = true;
			}

			yDot = Vector3.Dot (transform.forward, Vector3.forward);
		}
		
		
		public override void MoveCameraInstant ()
		{
			if (targetIsPlayer && KickStarter.player)
			{
				target = KickStarter.player.transform;
			}

			SetOriginalPosition ();
			
			if (target && (!lockHorizontal || !lockVertical))
			{
				SetDesired ();
			
				if (!lockHorizontal)
				{
					perspectiveOffset.x = desiredOffset.x;
				}
				
				if (!lockVertical)
				{
					perspectiveOffset.y = desiredOffset.y;
				}
			}
			
			SetProjection ();
		}


		private void SetProjection ()
		{
			if (!_camera.orthographic)
			{
				_camera.projectionMatrix = AdvGame.SetVanishingPoint (_camera, perspectiveOffset);
			}
			else
			{
				transform.position = new Vector3 (originalPosition.x + perspectiveOffset.x, originalPosition.y + perspectiveOffset.y, transform.position.z);
			}
		}


		/**
		 * Snaps the camera to its offset values and recalculates the camera's projection matrix.
		 */
		public void SnapToOffset ()
		{
			perspectiveOffset = afterOffset;
			SetProjection ();
		}


		private Vector2 GetOffsetForPosition (Vector3 targetPosition)
		{
			Vector2 targetOffset = new Vector2 ();
			float forwardOffsetScale = 93 - (299 * _camera.nearClipPlane);

			if (KickStarter.settingsManager && KickStarter.settingsManager.IsTopDown ())
			{
				if (_camera.orthographic)
				{
					targetOffset.x = transform.position.x;
					targetOffset.y = transform.position.z;
				}
				else
				{
					targetOffset.x = - (targetPosition.x - transform.position.x) / (forwardOffsetScale * (targetPosition.y - transform.position.y));
					targetOffset.y = - (targetPosition.z - transform.position.z) / (forwardOffsetScale * (targetPosition.y - transform.position.y));
				}
			}
			else
			{
				if (_camera.orthographic)
				{
					targetOffset.x = targetPosition.x;
					targetOffset.y = targetPosition.y;
				}
				else
				{
					targetOffset.x = (targetPosition.x - transform.position.x) / (forwardOffsetScale * (targetPosition.z - transform.position.z));
					targetOffset.y = yDot * (targetPosition.y - transform.position.y) / (forwardOffsetScale * (targetPosition.z - transform.position.z));
				}
			}

			return targetOffset;
		}


		/**
		 * Sets the camera's rotation and projection according to the chosen settings in SettingsManager.
		 */
		public void SetCorrectRotation ()
		{
			if (_camera == null)
			{
				_camera = GetComponent <Camera>();
			}

			if (KickStarter.settingsManager)
			{
				if (KickStarter.settingsManager.IsTopDown ())
				{
					transform.rotation = Quaternion.Euler (90f, 0, 0);
					return;
				}

				if (KickStarter.settingsManager.IsUnity2D ())
				{
					_camera.orthographic = true;
				}
			}

			transform.rotation = Quaternion.Euler (0, 0, 0);
		}


		/**
		 * <summary>Checks if the GameObject's rotation matches the intended rotation, according to the chosen settings in SettingsManager.</summary>
		 * <returns>True if the GameObject's rotation matches the intended rotation<returns>
		 */
		public bool IsCorrectRotation ()
		{
			if (KickStarter.settingsManager != null && KickStarter.settingsManager.IsTopDown ())
			{
				if (transform.rotation == Quaternion.Euler (90f, 0f, 0f))
				{
					return true;
				}

				return false;
			}

			if (transform.rotation == Quaternion.Euler (0f, 0f, 0f))
			{
				return true;
			}

			return false;
		}


		public override Vector2 GetPerspectiveOffset ()
		{
			return perspectiveOffset;
		}

	}

}