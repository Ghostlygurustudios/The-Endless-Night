/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"Moveable.cs"
 * 
 *	This script is attached to any gameObject that is to be transformed
 *	during gameplay via the action ActionTransform.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script provides functions to move or transform the GameObject it is attached to.
	 * It is used by the "Object: Transform" Action to move objects without scripting.
	 */
	[AddComponentMenu("Adventure Creator/Misc/Moveable")]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_moveable.html")]
	#endif
	public class Moveable : MonoBehaviour
	{

		private float positionChangeTime;
		private float positionStartTime;
		private AnimationCurve positionTimeCurve;
		private MoveMethod positionMethod;

		private	Vector3 startPosition;
		private Vector3 endPosition;

		private float rotateChangeTime;
		private float rotateStartTime;
		private AnimationCurve rotateTimeCurve;
		private MoveMethod rotateMethod;
		private bool doEulerRotation = false;

		private Vector3 startEulerRotation;
		private Vector3 endEulerRotation;
		
		private Quaternion startRotation;
		private Quaternion endRotation;

		private float scaleChangeTime;
		private float scaleStartTime;
		private AnimationCurve scaleTimeCurve;
		private MoveMethod scaleMethod;

		private Vector3 startScale;
		private Vector3 endScale;


		/**
		 * Halts the GameObject, if it is being moved by this script.
		 */
		public void StopMoving ()
		{
			positionChangeTime = rotateChangeTime = scaleChangeTime = 0f;
		}


		public bool IsMoving (TransformType transformType)
		{
			if (transformType == TransformType.Translate)
			{
				return (positionChangeTime > 0f);
			}
			else if (transformType == TransformType.Rotate)
			{
				return (rotateChangeTime > 0f);
			}
			else if (transformType == TransformType.Scale)
			{
				return (scaleChangeTime > 0f);
			}
			else if (transformType == TransformType.CopyMarker)
			{
				return (positionChangeTime > 0f);
			}

			return false;
		}
		

		/**
		 * Halts the GameObject, and sets its Transform to its target values, if it is being moved by this script.
		 */
		public void EndMovement ()
		{
			if (positionChangeTime > 0f)
			{
				transform.localPosition = endPosition;
			}

			if (rotateChangeTime > 0f)
			{
				if (doEulerRotation)
				{
					transform.localEulerAngles = endEulerRotation;
				}
				else
				{
					transform.localRotation = endRotation;
				}
			}

			if (scaleChangeTime > 0f)
			{
				transform.localScale = endScale;
			}

			StopMoving ();
		}


		private void Update ()
		{
			if (positionChangeTime > 0f)
			{
				if (Time.time < positionStartTime + positionChangeTime)
				{
					if (positionMethod == MoveMethod.Curved)
					{
						transform.localPosition = Vector3.Slerp (startPosition, endPosition, AdvGame.Interpolate (positionStartTime, positionChangeTime, positionMethod, positionTimeCurve)); 
					}
					else
					{
						transform.localPosition = AdvGame.Lerp (startPosition, endPosition, AdvGame.Interpolate (positionStartTime, positionChangeTime, positionMethod, positionTimeCurve)); 
					}
				}
				else
				{
					transform.localPosition = endPosition;
					positionChangeTime = 0f;
				}
			}

			if (rotateChangeTime > 0f)
			{
				if (Time.time < rotateStartTime + rotateChangeTime)
				{
					if (doEulerRotation)
					{
						if (rotateMethod == MoveMethod.Curved)
						{
							transform.localEulerAngles = Vector3.Slerp (startEulerRotation, endEulerRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve)); 
						}
						else
						{
							transform.localEulerAngles = AdvGame.Lerp (startEulerRotation, endEulerRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve)); 
						}
					}
					else
					{
						if (rotateMethod == MoveMethod.Curved)
						{
							transform.localRotation = Quaternion.Slerp (startRotation, endRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve)); 
						}
						else
						{
							transform.localRotation = AdvGame.Lerp (startRotation, endRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve)); 
						}
					}
				}
				else
				{
					if (doEulerRotation)
					{
						transform.localEulerAngles = endEulerRotation;
					}
					else
					{
						transform.localRotation = endRotation;
					}
					rotateChangeTime = 0f;
				}
			}

			if (scaleChangeTime > 0f)
			{
				if (Time.time < scaleStartTime + scaleChangeTime)
				{
					if (scaleMethod == MoveMethod.Curved)
					{
						transform.localScale = Vector3.Slerp (startScale, endScale, AdvGame.Interpolate (scaleStartTime, scaleChangeTime, scaleMethod, scaleTimeCurve)); 
					}
					else
					{
						transform.localScale = AdvGame.Lerp (startScale, endScale, AdvGame.Interpolate (scaleStartTime, scaleChangeTime, scaleMethod, scaleTimeCurve)); 
					}
				}
				else
				{
					transform.localScale = endScale;
					scaleChangeTime = 0f;
				}
			}
		}
		

		/**
		 * <summary>Moves the GameObject by referencing a Vector3 as its target Transform.</summary>
		 * <param name = "_newVector">The target values of either the GameObject's position, rotation or scale</param>
		 * <param name = "_moveMethod">The interpolation method by which the GameObject moves (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_transitionTime">The time, in seconds, that the movement should take place over</param>
		 * <param name = "_transformType">The way in which the GameObject should be transformed (Translate, Rotate, Scale)</param>
		 * <param name = "_doEulerRotation">If True, then the GameObject's eulerAngles will be directly manipulated. Otherwise, the rotation as a Quaternion will be affected.</param>
		 * <param name = "_timeCurve">If _moveMethod = MoveMethod.CustomCurve, then the movement speed will follow the shape of the supplied AnimationCurve. This curve can exceed "1" in the Y-scale, allowing for overshoot effects.</param>
		 * <param name = "clearExisting">If True, then existing transforms will be stopped before new transforms will be made</param>
		 */
		public void Move (Vector3 _newVector, MoveMethod _moveMethod, float _transitionTime, TransformType _transformType, bool _doEulerRotation, AnimationCurve _timeCurve, bool clearExisting)
		{
			if (GetComponent <Rigidbody>() && !GetComponent <Rigidbody>().isKinematic)
			{
				GetComponent <Rigidbody>().velocity = GetComponent <Rigidbody>().angularVelocity = Vector3.zero;
			}
			
			if (_transitionTime <= 0f)
			{
				if (clearExisting)
				{
					positionChangeTime = rotateChangeTime = scaleChangeTime = 0f;
				}

				if (_transformType == TransformType.Translate)
				{
					transform.localPosition = _newVector;
					positionChangeTime = 0f;
				}
				else if (_transformType == TransformType.Rotate)
				{
					transform.localEulerAngles = _newVector;
					rotateChangeTime = 0f;
				}
				else if (_transformType == TransformType.Scale)
				{
					transform.localScale = _newVector;
					scaleChangeTime = 0f;
				}
			}
			else
			{
				if (_transformType == TransformType.Translate)
				{
					startPosition = endPosition = transform.localPosition;
					endPosition = _newVector;

					positionMethod = _moveMethod;

					positionChangeTime = _transitionTime;
					positionStartTime = Time.time;

					positionMethod = _moveMethod;
					if (positionMethod == MoveMethod.CustomCurve)
					{
						positionTimeCurve = _timeCurve;
					}
					else
					{
						positionTimeCurve = null;
					}

					if (clearExisting)
					{
						rotateChangeTime = scaleChangeTime = 0f;
					}
				}
				else if (_transformType == TransformType.Rotate)
				{
					startEulerRotation = endEulerRotation = transform.localEulerAngles;
					startRotation = endRotation = transform.localRotation;
					endRotation = Quaternion.Euler (_newVector);
					endEulerRotation = _newVector;

					doEulerRotation = _doEulerRotation;
					rotateMethod = _moveMethod;

					rotateChangeTime = _transitionTime;
					rotateStartTime = Time.time;

					rotateMethod = _moveMethod;
					if (rotateMethod == MoveMethod.CustomCurve)
					{
						rotateTimeCurve = _timeCurve;
					}
					else
					{
						rotateTimeCurve = null;
					}

					if (clearExisting)
					{
						positionChangeTime = scaleChangeTime = 0f;
					}
				}
				else if (_transformType == TransformType.Scale)
				{
					startScale = endScale = transform.localScale;
					endScale = _newVector;

					scaleMethod = _moveMethod;

					scaleChangeTime = _transitionTime;
					scaleStartTime = Time.time;

					scaleMethod = _moveMethod;
					if (scaleMethod == MoveMethod.CustomCurve)
					{
						scaleTimeCurve = _timeCurve;
					}
					else
					{
						scaleTimeCurve = null;
					}

					if (clearExisting)
					{
						positionChangeTime = rotateChangeTime = 0f;
					}
				}
			}
		}
		

		/**
		 * <summary>Moves the GameObject by referencing a Marker component as its target Transform.</summary>
		 * <param name = "_marker">A Marker whose position, rotation and scale will be the target values of the GameObject</param>
		 * <param name = "_moveMethod">The interpolation method by which the GameObject moves (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_transitionTime">The time, in seconds, that the movement should take place over</param>
		 * <param name = "_timeCurve">If _moveMethod = MoveMethod.CustomCurve, then the movement speed will follow the shape of the supplied AnimationCurve. This curve can exceed "1" in the Y-scale, allowing for overshoot effects.</param>
		 */
		public void Move (Marker _marker, MoveMethod _moveMethod, float _transitionTime, AnimationCurve _timeCurve)
		{
			if (GetComponent <Rigidbody>() && !GetComponent <Rigidbody>().isKinematic)
			{
				GetComponent <Rigidbody>().velocity = GetComponent <Rigidbody>().angularVelocity = Vector3.zero;
			}
			
			if (_transitionTime == 0f)
			{
				positionChangeTime = rotateChangeTime = scaleChangeTime = 0f;
				
				transform.localPosition = _marker.transform.localPosition;
				transform.localEulerAngles = _marker.transform.localEulerAngles;
				transform.localScale = _marker.transform.localScale;
			}
			else
			{
				doEulerRotation = false;
				positionMethod = rotateMethod = scaleMethod = _moveMethod;
				
				startPosition = transform.localPosition;
				startRotation = transform.localRotation;
				startScale = transform.localScale;
				
				endPosition = _marker.transform.localPosition;
				endRotation = _marker.transform.localRotation;
				endScale = _marker.transform.localScale;
				
				positionChangeTime = rotateChangeTime = scaleChangeTime = _transitionTime;
				positionStartTime = rotateStartTime = scaleStartTime = Time.time;
				
				if (_moveMethod == MoveMethod.CustomCurve)
				{
					positionTimeCurve = _timeCurve;
					rotateTimeCurve = _timeCurve;
					scaleTimeCurve = _timeCurve;
				}
				else
				{
					positionTimeCurve = rotateTimeCurve = scaleTimeCurve = null;
				}
			}
		}


		/**
		 * <summary>Updates a MoveableData class with its own variables that need saving.</summary>
		 * <param name = "saveData">The original MoveableData class</param>
		 * <returns>The updated MoveableData class</returns>
		 */
		public MoveableData SaveData (MoveableData saveData)
		{
			if (positionChangeTime > 0f)
			{
				saveData.LocX = endPosition.x;
				saveData.LocY = endPosition.y;
				saveData.LocZ = endPosition.z;
			}

			if (rotateChangeTime > 0f)
			{
				saveData.doEulerRotation = doEulerRotation;

				if (doEulerRotation)
				{
					saveData.LocX = endEulerRotation.x;
					saveData.LocY = endEulerRotation.y;
					saveData.LocZ = endEulerRotation.z;
				}
				else
				{
					saveData.RotW = endRotation.w;
					saveData.RotX = endRotation.x;
					saveData.RotY = endRotation.y;
					saveData.RotZ = endRotation.z;
				}
			}
			else
			{
				saveData.doEulerRotation = true;
			}

			if (scaleChangeTime > 0f)
			{
				saveData.ScaleX = endScale.x;
				saveData.ScaleY = endScale.y;
				saveData.ScaleZ = endScale.z;
			}

			return saveData;
		}


		/**
		 * <summary>Updates its own variables from a MoveableData class.</summary>
		 * <param name = "saveData">The MoveableData class to load from</param>
		 */
		public void LoadData (MoveableData saveData)
		{
			if (!saveData.doEulerRotation)
			{
				transform.localRotation = new Quaternion (saveData.RotW, saveData.RotX, saveData.RotY, saveData.RotZ);
			}

			StopMoving ();
		}


		/**
		 * An alias of StopMoving, for easy use in the "Object: Send message" Action.
		 */
		private void Kill ()
		{
			StopMoving ();
		}
		
	}
	
}