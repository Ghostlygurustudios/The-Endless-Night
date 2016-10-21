/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"JointBreaker.cs"
 * 
 *	This script is used by the PickUp script to
 *	clean up FixedJoints after they've broken
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * This component is used by PickUp to clean up FixedJoints after they've broken.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_joint_breaker.html")]
	#endif
	public class JointBreaker : MonoBehaviour
	{

		private void OnJointBreak (float breakForce)
		{
			GetComponent <FixedJoint>().connectedBody.GetComponent <Moveable_PickUp>().UnsetFixedJoint ();
			Destroy (this.gameObject);
		}

	}

}