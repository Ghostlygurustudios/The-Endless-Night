/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"AlignToCamera.cs"
 * 
 *	Attach this script to an object you wish to align to a camera's view.
 *	This works best with sprites being used as foreground objects in 2.5D games.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * Aligns an object to a camera's viewport. This is intended for sprites being used as foreground objects in 2.5D games.
	 */
	[ExecuteInEditMode]
	[AddComponentMenu("Adventure Creator/Camera/Align-to camera")]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_align_to_camera.html")]
	#endif
	public class AlignToCamera : MonoBehaviour
	{

		/** The _Camera to align the GameObject to */
		public _Camera cameraToAlignTo;
		/** How far to place the GameObject away from the cameraToAlignTo, once set */
		public float distanceToCamera;
		/** If True, the percieved scale of the GameObject, as seen through the cameraToAlignTo, will be fixed even if the distance between the two changes */
		public bool lockScale;
		/** If lockScale is True, this GameObject's scale will be multiplied by this value */
		public float scaleFactor = 0f;


		private void Awake ()
		{
			Align ();
		}


		#if UNITY_EDITOR
		private void Update ()
		{
			if (!Application.isPlaying)
			{
				Align ();
			}
		}


		/**
		 * Attempts to place the GameObject in the centre of cameraToAlignTo's view.
		 */
		public void CentreToCamera ()
		{
			float distanceFromCamera = Vector3.Dot (cameraToAlignTo.transform.forward, transform.position - cameraToAlignTo.transform.position);
			if (distanceFromCamera == 0f)
			{
				return;
			}
			
			Vector3 newPosition = cameraToAlignTo.transform.position + (cameraToAlignTo.transform.forward * distanceFromCamera);
			transform.position = newPosition;
		}
		#endif


		private void Align ()
		{
			if (cameraToAlignTo)
			{
				transform.rotation = Quaternion.Euler (transform.rotation.eulerAngles.x, cameraToAlignTo.transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

				if (distanceToCamera > 0f)
				{
					Vector3 relativePosition = transform.position - cameraToAlignTo.transform.position;
					float currentDistance = relativePosition.magnitude;
					if (currentDistance != distanceToCamera)
					{
						if (currentDistance != 0)
						{
							transform.position = cameraToAlignTo.transform.position + (relativePosition * distanceToCamera / currentDistance);
						}
						else
						{
							transform.position = cameraToAlignTo.transform.position + cameraToAlignTo.transform.forward * distanceToCamera;
						}
					}

					if (lockScale)
					{
						CalculateScale ();

						if (scaleFactor != 0f)
						{
							transform.localScale = Vector3.one * scaleFactor * distanceToCamera;
						}
					}
				}
				else if (distanceToCamera < 0f)
				{
					distanceToCamera = 0f;
				}
				else if (distanceToCamera == 0f)
				{
					Vector3 relativePosition = transform.position - cameraToAlignTo.transform.position;
					if (relativePosition.magnitude != 0f)
					{
						distanceToCamera = relativePosition.magnitude;
					}
				}
			}

			if (!lockScale || cameraToAlignTo == null)
			{
				scaleFactor = 0f;
			}
		}


		private void CalculateScale ()
		{
			if (scaleFactor == 0f)
			{
				scaleFactor = transform.localScale.y / distanceToCamera;
			}
		}

	}

}