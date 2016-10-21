/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"SortingMap.cs"
 * 
 *	This script is used to change the sorting order of
 *	2D Character sprites based on their Z-position.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script is used to change the sorting order and scale of 2D characters, based on their position in the scene.
	 * The instance of this class stored in SceneSettings' sortingMap variable will be read by FollowSortingMap components to determine what their SpriteRenderer's order and scale should be.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_sorting_map.html")]
	#endif
	public class SortingMap : MonoBehaviour
	{

		/** How SpriteRenderer components that follow this map are effected (OrderInLayer, SortingLayer) */
		public SortingMapType mapType = SortingMapType.OrderInLayer;
		/** A List of SortingArea data that makes up the map */
		public List <SortingArea> sortingAreas = new List<SortingArea>();			
		/** True if characters that follow this map should have their scale affected */
		public bool affectScale = false;
		/** True if characters that follow this map should have their movement speed affected by the scale factor */
		public bool affectSpeed = true;
		/** The scale (as a percentage) that characters will have at the very top of the map (if affectScale = True) */
		public int originScale = 100;
		
		private FollowSortingMap[] followers;
		

		/**
		 * Recalculates the "followers" array, which is an array of all FollowSortingMap components present in the scene.
		 */
		public void GetAllFollowers ()
		{
			FollowSortingMap[] _followers = FindObjectsOfType (typeof (FollowSortingMap)) as FollowSortingMap[];
			List<FollowSortingMap> followerList = new List<FollowSortingMap>();
			foreach (FollowSortingMap _follower in _followers)
			{
				_follower.UpdateSortingMap ();
				if (_follower.GetSortingMap () == this)
				{
					followerList.Add (_follower);
				}
			}
			followers = followerList.ToArray ();
		}
		
		
		private void OnDrawGizmos ()
		{
			for (int i=0; i<sortingAreas.Count; i++)
			{
				Gizmos.DrawIcon (GetAreaPosition (i), "", true);
				
				Gizmos.color = sortingAreas [i].color;
				if (i == 0)
				{
					Gizmos.DrawLine (transform.position, GetAreaPosition (i));
				}
				else
				{
					Gizmos.DrawLine (GetAreaPosition (i-1), GetAreaPosition (i));
				}
			}
		}
		

		/**
		 * <summary>Given a FollowSortingMap component, adjusts all other FollowSortingMaps that are within the same region, so that they are all displayed correctly.</summary>
		 * <param name = "follower">The FollowSortingMap component to check for</param>
		 */
		public void UpdateSimilarFollowers (FollowSortingMap follower)
		{
			if (followers == null || followers.Length <= 1)
			{
				return;
			}
			
			if (follower.followSortingMap)
			{
				string testOrder = follower.GetOrder ();
				List<FollowSortingMap> testFollowers = new List<FollowSortingMap>();
				
				foreach (FollowSortingMap testFollower in followers)
				{
					if (testFollower != null && testFollower.followSortingMap && !testFollower.lockSorting && testFollower != follower && testFollower.GetOrder () == testOrder)
					{
						// Found a follower with the same order/layer
						testFollowers.Add (testFollower);
					}
				}
				
				if (testFollowers.Count > 0)
				{
					testFollowers.Add (follower);
					if (testFollowers.Count > 1)
					{
						testFollowers.Sort (SortByScreenPosition);
					}
					// Now in order from bottom up
					
					for (int i=0; i<testFollowers.Count; i++)
					{
						testFollowers [i].SetDepth (i * 0.001f);
					}
				}
				else
				{
					follower.SetDepth (0f);
				}
			}
		}
		
		
		private static int SortByScreenPosition (FollowSortingMap o1, FollowSortingMap o2)
		{
			return Camera.main.WorldToScreenPoint (o1.transform.position).y.CompareTo (Camera.main.WorldToScreenPoint (o2.transform.position).y);
		}
		

		/**
		 * <summary>Gets the boundary position of a particular SortingArea.</summary>
		 * <param name = "i">The index of the SortingArea to get the boundary position of</param>
		 * <returns>The boundary positon of the SortingArea</returns>
		 */
		public Vector3 GetAreaPosition (int i)
		{
			return (transform.position + (transform.forward * sortingAreas [i].z));
		}
		

		/**
		 * <summary>Gets an interpolated scale factor, based on a position in the scene.</summary>
		 * <param name = "followPosition">The position in the scene to the scale factor for</param>
		 * <returns>The interpolated scale factor for any FollowSortingMap components at the given position</returns>
		 */
		public float GetScale (Vector3 followPosition)
		{
			if (!affectScale)
			{
				return 1f;
			}
			
			if (sortingAreas.Count == 0)
			{
				return (float) originScale;
			}
			
			// Behind first?
			if (Vector3.Angle (transform.forward, transform.position - followPosition) < 90f)
			{
				return (float) originScale;
			}
			
			// In front of last?
			if (Vector3.Angle (transform.forward, GetAreaPosition (sortingAreas.Count-1) - followPosition) > 90f)
			{
				return (float) sortingAreas [sortingAreas.Count-1].scale;
			}
			
			// In between two?
			for (int i=0; i<sortingAreas.Count; i++)
			{
				float angle = Vector3.Angle (transform.forward, GetAreaPosition (i) - followPosition);
				if (angle < 90f)
				{
					float prevZ = 0;
					if (i > 0)
					{
						prevZ = sortingAreas [i-1].z;
					}
					
					float proportionAlong = 1 - Vector3.Distance (GetAreaPosition (i), followPosition) / (sortingAreas [i].z - prevZ) * Mathf.Cos (Mathf.Deg2Rad * angle);
					float previousScale = (float) originScale;
					if (i > 0)
					{
						previousScale = sortingAreas [i-1].scale;
					}
					
					return (previousScale + proportionAlong * ((float) sortingAreas [i].scale - previousScale));
				}
			}
			
			return 1f;
		}
		

		/**
		 * Assigns the scale factors for all SortingArea data that lie in between the top and bottom boundaries.
		 */
		public void SetInBetweenScales ()
		{
			if (sortingAreas.Count < 2)
			{
				return;
			}
			
			float finalScale = sortingAreas [sortingAreas.Count-1].scale;
			float finalZ = sortingAreas [sortingAreas.Count-1].z;
			
			for (int i=0; i<sortingAreas.Count-1; i++)
			{
				float newScale = ((sortingAreas [i].z / finalZ) * ((float) finalScale - (float) originScale)) + (float) originScale;
				sortingAreas [i].scale = (int) newScale;
			}
		}
		
	}
	
}
