/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"NavMeshBase.cs"
 * 
 *	A base class for NavigationMesh and NavMeshSegment
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * A base class for NavigationMesh and NavMeshSegment, which control scene objects used by pathfinding algorithms.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_nav_mesh_base.html")]
	#endif
	public class NavMeshBase : MonoBehaviour
	{

		/** Disables the Renderer when the game begins */
		public bool disableRenderer = true;

		#if UNITY_5
		/** If True, then Physics collisions with this GameObject's Collider will be disabled (Unity 5 only) */
		public bool ignoreCollisions = true;
		#endif


		protected void BaseAwake ()
		{
			if (disableRenderer)
			{
				Hide ();
			}
			#if !UNITY_5
			if (GetComponent <Collider>())
			{
				GetComponent <Collider>().isTrigger = true;
			}
			#endif
		}


		/**
		 * Disables the Renderer component.
		 */
		public void Hide ()
		{
			if (GetComponent <MeshRenderer>())
			{
				GetComponent <MeshRenderer>().enabled = false;
			}
		}


		/**
		 * Enables the Renderer component.
		 * If the GameObject has both a MeshFilter and a MeshCollider, then the MeshColliders's mesh will be used by the MeshFilter.
		 */
		public void Show ()
		{
			if (GetComponent <MeshRenderer>())
			{
				GetComponent <MeshRenderer>().enabled = true;

				if (GetComponent <MeshFilter>() && GetComponent <MeshCollider>() && GetComponent <MeshCollider>().sharedMesh)
				{
					GetComponent <MeshFilter>().mesh = GetComponent <MeshCollider>().sharedMesh;
				}
			}
		}

	}

}
