/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"FirstPersonCamera.cs"
 * 
 *	An optional script that allows First Person control.
 *	This is attached to a camera which is a child of the player.
 *	Only one First Person Camera should ever exist in the scene at runtime.
 *	Only the yaw is affected here: the pitch is determined by the player parent object.
 *
 *	Headbobbing code adapted from Mr. Animator's code: http://wiki.unity3d.com/index.php/Headbobber
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * A camera used for first-person games. To use it, attach it to a child object of your Player prefab, as well as a Camera.
	 * It will then be used during gameplay if SettingsManager's movementMethod = MovementMethod.FirstPerson.
	 * This script only affects the pitch rotation - yaw rotation occurs by rotating the base object.
	 * Headbobbing code adapted from Mr. Animator's code: http://wiki.unity3d.com/index.php/Headbobber
	 */
	[AddComponentMenu("Adventure Creator/Camera/First-person camera")]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_first_person_camera.html")]
	#endif
	public class FirstPersonCamera : _Camera
	{

		/** The sensitivity of free-aiming */
		public Vector2 sensitivity = new Vector2 (15f, 15f);

		/** The minimum pitch angle */
		public float minY = -60f;
		/** The maximum pitch angle */
		public float maxY = 60f;

		/** If True, the mousewheel can be used to zoom the camera's FOV */
		public bool allowMouseWheelZooming = false;
		/** The minimum FOV, if allowMouseWheelZooming = True */
		public float minimumZoom = 13f;
		/** The maximum FOV, if allowMouseWheelZooming = True */
		public float maximumZoom = 65f;

		/** If True, then the camera will bob up and down as the Player moves */
		public bool headBob = true;
		/** The bobbing speed, if headBob = True */
		public float bobbingSpeed = 0.18f;
		/** The bobbing magnitude, if headBob = True */
		public float bobbingAmount = 0.2f;
		
		private float rotationY = 0f;
		private float bobTimer = 0f;
		private float height = 0f;
		private float deltaHeight = 0f;


		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			Awake ();
		}


		protected override void Awake ()
		{
			height = transform.localPosition.y;
		}


		/**
		 * Overrides the default in _Camera to do nothing.
		 */
		new public void ResetTarget ()
		{}
		

		/**
		 * Updates the camera's transform.
		 * This is called every frame by StateHandler.
		 */
		public void _UpdateFPCamera ()
		{
			if (KickStarter.stateHandler.gameState != GameState.Normal)
			{
				return;
			}

			if (headBob)
			{
				deltaHeight = 0f;

				Vector2 moveKeys = KickStarter.playerInput.GetMoveKeys ();
				if ((moveKeys.x == 0f && moveKeys.y == 0f) || KickStarter.settingsManager.IsFirstPersonDragRotation () || (KickStarter.settingsManager.IsFirstPersonDragComplex () && Input.touchCount == 1) || !KickStarter.player.IsGrounded ())
				{ 
					bobTimer = 0f;
				} 
				else
				{
					float waveSlice = Mathf.Sin (bobTimer);
					
					if (KickStarter.playerInput.IsPlayerControlledRunning ())
					{
						bobTimer += (2f * bobbingSpeed) * Time.deltaTime * 35f;
					}
					else
					{
						bobTimer += bobbingSpeed * Time.deltaTime * 35f;
					}
					
					if (bobTimer > Mathf.PI * 2)
					{
						bobTimer = bobTimer - (2f * Mathf.PI);
					}
					
					float totalAxes = Mathf.Abs (moveKeys.x) + Mathf.Abs (moveKeys.y);
					totalAxes = Mathf.Clamp (totalAxes, 0f, 1f);
					
					deltaHeight = totalAxes * waveSlice * bobbingAmount;
				}
				
				transform.localPosition = new Vector3 (transform.localPosition.x, height + deltaHeight, transform.localPosition.z);
			}
			
			if (allowMouseWheelZooming && GetComponent <Camera>() && KickStarter.stateHandler.gameState == AC.GameState.Normal)
			{
				try
				{
					if (Input.GetAxis("Mouse ScrollWheel") > 0)
					{
						GetComponent <Camera>().fieldOfView = Mathf.Max (GetComponent <Camera>().fieldOfView - 3, minimumZoom);
					 
					}
					if (Input.GetAxis("Mouse ScrollWheel") < 0)
					{
						GetComponent <Camera>().fieldOfView = Mathf.Min (GetComponent <Camera>().fieldOfView + 3, maximumZoom);
					}
				}
				catch
				{ }
			}
		}
		
		
		private void FixedUpdate ()
		{
			rotationY = Mathf.Clamp (rotationY, minY, maxY);
			transform.localEulerAngles = new Vector3 (rotationY, 0f, 0f);
		}
		

		/**
		 * <summary>Sets the pitch to a specific angle.</summary>
		 * <param name = "angle">The new pitch angle</param>
		 */
		public void SetPitch (float angle)
		{
			rotationY = angle;
		}


		/**
		 * <summary>Increases the pitch, accounting for sensitivity</summary>
		 * <param name = "increase">The amount to increase sensitivity by</param>
		 */
		public void IncreasePitch (float increase)
		{
			rotationY += increase * sensitivity.y;
		}

	}

}
