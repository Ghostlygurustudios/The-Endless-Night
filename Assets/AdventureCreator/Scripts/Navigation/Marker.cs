/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"Marker.cs"
 * 
 *	This script allows a simple way of teleporting
 *	characters and objects around the scene.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * A component used to create reference transforms, as needed by the PlayerStart and various Actions.
	 * When the game begins, the renderer will be disabled and the GameObject will be rotated if the game is 2D.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_marker.html")]
	#endif
	[AddComponentMenu("Adventure Creator/Navigation/Marker")]
	public class Marker : MonoBehaviour
	{

		protected void Awake ()
		{
			if (GetComponent <Renderer>())
			{
				GetComponent <Renderer>().enabled = false;
			}
			
			if (KickStarter.settingsManager && KickStarter.settingsManager.IsUnity2D ())
			{
				transform.RotateAround (transform.position, Vector3.right, 90f);
				transform.RotateAround (transform.position, transform.right, -90f);
			}
		}
		
	}

}