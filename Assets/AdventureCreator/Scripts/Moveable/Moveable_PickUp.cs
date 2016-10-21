/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"Moveable_PickUp.cs"
 * 
 *	Attaching this script to a GameObject allows it to be
 *	picked up and manipulated freely by the player.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * Attaching this component to a GameObject allows it to be picked up and manipulated freely by the player.
	 */
	[RequireComponent (typeof (Rigidbody))]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_moveable___pick_up.html")]
	#endif
	public class Moveable_PickUp : DragBase
	{

		/** If True, the object can be rotated */
		public bool allowRotation = false;
		/** The maximum force magnitude that can be applied by the player - if exceeded, control will be removed */
		public float breakForce = 300f;
		/** If True, the object can be thrown */
		public bool allowThrow = false;
		/** How long a "charge" takes, if the object cen be thrown */
		public float chargeTime = 0.5f;
		/** How far the object is pulled back while chargine, if the object can be thrown */
		public float pullbackDistance = 0.6f;
		/** How far the object can be thrown */
		public float throwForce = 400f;
		/** The Interaction to run whenever the object is picked up by the player */
		public Interaction interactionOnGrab;

		private bool preRotationCursorLock;
		private bool isChargingThrow = false;
		private float throwCharge = 0f;
		private float chargeStartTime;
		private bool inRotationMode = false;
		private FixedJoint fixedJoint;
		private float originalDistanceToCamera;
		private Vector3 lastPosition;

		
		protected override void Awake ()
		{
			base.Awake ();
			LimitCollisions ();
		}


		/**
		 * Called every frame by StateHandler.
		 */
		public override void UpdateMovement ()
		{
			base.UpdateMovement ();

			if (moveSound && moveSoundClip && !inRotationMode)
			{
				if (numCollisions > 0)
			    {
					PlayMoveSound (_rigidbody.velocity.magnitude, 0.5f);
				}
				else if (moveSound.IsPlaying ())
				{
					moveSound.Stop ();
				}
			}
		}


		private void ChargeThrow ()
		{
			if (!isChargingThrow)
			{
				isChargingThrow = true;
				chargeStartTime = Time.time;
				throwCharge = 0f;
			}
			else if (throwCharge < 1f)
			{
				throwCharge = (Time.time - chargeStartTime) / chargeTime;
			}

			if (throwCharge > 1f)
			{
				throwCharge = 1f;
			}
		}


		private void ReleaseThrow ()
		{
			LetGo ();

			_rigidbody.useGravity = true;
			_rigidbody.drag = originalDrag;
			_rigidbody.angularDrag = originalAngularDrag;

			//isHeld = false;
			Vector3 moveVector = (transform.position - cameraTransform.position).normalized;
			_rigidbody.AddForce (throwForce * throwCharge * moveVector);
		}
		
		
		private void CreateFixedJoint ()
		{
			GameObject go = new GameObject (this.name + " (Joint)");
			Rigidbody body = go.AddComponent <Rigidbody>();
			body.constraints = RigidbodyConstraints.FreezeAll;
			body.useGravity = false;
			fixedJoint = go.AddComponent <FixedJoint>();
			fixedJoint.breakForce = fixedJoint.breakTorque = breakForce;
			//fixedJoint.enableCollision = false;
			go.AddComponent <JointBreaker>();
		}
		

		/**
		 * <summary>Attaches the object to the player's control.</summary>
		 * <param name = "grabPosition">The point of contact on the object</param>
		 */
		public override void Grab (Vector3 grabPosition)
		{
			base.Grab (grabPosition);

			inRotationMode = false;
			isChargingThrow = false;
			throwCharge = 0f;
			
			if (fixedJoint == null)
			{
				CreateFixedJoint ();
			}

			_rigidbody.velocity = _rigidbody.angularVelocity = Vector3.zero;
			lastPosition = transform.position;
			originalDistanceToCamera = (grabPosition - cameraTransform.position).magnitude;

			if (interactionOnGrab)
			{
				interactionOnGrab.Interact ();
			}
		}


		/**
		 * Detaches the object from the player's control.
		 */
		public override void LetGo ()
		{
			if (inRotationMode)
			{
				SetRotationMode (false);
			}

			if (fixedJoint != null && fixedJoint.connectedBody)
			{
				fixedJoint.connectedBody = null;
			}

			_rigidbody.drag = originalDrag;
			_rigidbody.angularDrag = originalAngularDrag;

			if (inRotationMode)
			{
				_rigidbody.velocity = Vector3.zero;
			}
			else if (!isChargingThrow)
			{
				Vector3 deltaPosition = transform.position - lastPosition;
				_rigidbody.AddForce (deltaPosition * Time.deltaTime * 100000f);
			}

			_rigidbody.useGravity = true;
			isHeld = false;
		}


		/**
		 * If True, 'ToggleCursor' can be used while the object is held.
		 */
		public override bool CanToggleCursor ()
		{
			if (isChargingThrow || inRotationMode)
			{
				return false;
			}
			return true;
		}


		private void SetRotationMode (bool on)
		{
			_rigidbody.velocity = Vector3.zero;
			_rigidbody.useGravity = !on;

			if (inRotationMode != on)
			{
				if (on)
				{
					preRotationCursorLock = KickStarter.playerInput.cursorIsLocked;
					KickStarter.playerInput.cursorIsLocked = false;
					fixedJoint.connectedBody = null;
				}
				else
				{
					KickStarter.playerInput.cursorIsLocked = preRotationCursorLock;
				}
			}

			inRotationMode = on;
		}


		/**
		 * <summary>Applies a drag force on the object, based on the movement of the cursor.</summary>
		 * <param name = "force">The force vector to apply</param>
		 * <param name = "mousePos">The position of the mouse</param>
		 * <param name = "_distanceToCamera">The distance between the object's centre and the camera</param>
		 */
		public override void ApplyDragForce (Vector3 force, Vector3 mousePos, float _distanceToCamera)
		{
			distanceToCamera = _distanceToCamera;

			if (allowThrow)
			{
				if (KickStarter.playerInput.InputGetButton ("ThrowMoveable"))
				{
					ChargeThrow ();
				}
				else if (isChargingThrow)
				{
					ReleaseThrow ();
				}
			}

			if (allowRotation)
			{
				if (KickStarter.playerInput.InputGetButton ("RotateMoveable"))
				{
					SetRotationMode (true);
				}
				else if (KickStarter.playerInput.InputGetButtonUp ("RotateMoveable"))
				{
					SetRotationMode (false);
					return;
				}
			
				if (KickStarter.playerInput.InputGetButtonDown ("RotateMoveableToggle"))
				{
					SetRotationMode (!inRotationMode);
					if (!inRotationMode)
					{
						return;
					}
				}
			}

			// Scale force
			force *= speedFactor * _rigidbody.drag * distanceToCamera * Time.deltaTime;
			
			// Limit magnitude
			if (force.magnitude > maxSpeed)
			{
				force *= maxSpeed / force.magnitude;
			}

			lastPosition = transform.position;

			if (inRotationMode)
			{
				Vector3 newRot = Vector3.Cross (force, cameraTransform.forward);
				newRot /= Mathf.Sqrt ((grabPoint.position - transform.position).magnitude) * 2.4f * rotationFactor;
				_rigidbody.AddTorque (newRot);
			}
			else
			{
				mousePos.z = originalDistanceToCamera - (throwCharge * pullbackDistance);
				Vector3 worldPos = Camera.main.ScreenToWorldPoint (mousePos);

				fixedJoint.transform.position = worldPos;
				fixedJoint.connectedBody = _rigidbody;
			}

			if (allowZooming)
			{
				UpdateZoom ();
			}
		}


		new private void UpdateZoom ()
		{
			float zoom = Input.GetAxis ("ZoomMoveable");
			Vector3 moveVector = (transform.position - cameraTransform.position).normalized;
			
			if (distanceToCamera - minZoom < 1f && zoom < 0f)
			{
				moveVector *= (originalDistanceToCamera - minZoom);
			}
			else if (maxZoom - originalDistanceToCamera < 1f && zoom > 0f)
			{
				moveVector *= (maxZoom - originalDistanceToCamera);
			}
			
			if ((originalDistanceToCamera <= minZoom && zoom < 0f) || (originalDistanceToCamera >= maxZoom && zoom > 0f))
			{}
			else
			{
				originalDistanceToCamera += (zoom * zoomSpeed / 10f * Time.deltaTime);
			}
		}


		/**
		 * Unsets the FixedJoint used to hold the object in place
		 */
		public void UnsetFixedJoint ()
		{
			fixedJoint = null;
			isHeld = false;
		}


		protected void LimitCollisions ()
		{
			Collider[] ownColliders = GetComponentsInChildren <Collider>();

			foreach (Collider _collider1 in ownColliders)
			{
				foreach (Collider _collider2 in ownColliders)
				{
					if (_collider1 == _collider2)
					{
						continue;
					}
					Physics.IgnoreCollision (_collider1, _collider2, true);
					Physics.IgnoreCollision (_collider1, _collider2, true);
				}

				if (ignorePlayerCollider)
				{
					if (GameObject.FindWithTag (Tags.player) && GameObject.FindWithTag (Tags.player).GetComponent <Collider>())
					{
						Collider playerCollider = GameObject.FindWithTag (Tags.player).GetComponent <Collider>();
						Physics.IgnoreCollision (playerCollider, _collider1, true);
					}
				}
			}

		}
		
		
		private void OnDestroy ()
		{
			if (fixedJoint)
			{
				Destroy (fixedJoint.gameObject);
				fixedJoint = null;
			}
		}

	}

}