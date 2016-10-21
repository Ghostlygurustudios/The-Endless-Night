/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"NavigationEngine_meshCollider.cs"
 * 
 *	This script uses Unity's built-in Navigation
 *	system to allow pathfinding in a scene.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public class NavigationEngine_UnityNavigation : NavigationEngine
	{

		public override void SceneSettingsGUI ()
		{
			#if UNITY_EDITOR
			if (AdvGame.GetReferences ().settingsManager && AdvGame.GetReferences ().settingsManager.IsUnity2D ())
			{
				EditorGUILayout.HelpBox ("This method is not compatible with 'Unity 2D' mode.", MessageType.Warning);
			}
			#endif
		}


		public override void TurnOn (NavigationMesh navMesh)
		{
			ACDebug.LogWarning ("Cannot enable NavMesh " + navMesh.gameObject.name + " as this scene's Navigation Method is Unity Navigation.");
		}


		public override Vector3[] GetPointsArray (Vector3 startPosition, Vector3 targetPosition, AC.Char _char = null)
		{
			NavMeshPath _path = new NavMeshPath();

			if (!NavMesh.CalculatePath (startPosition, targetPosition, -1, _path))
			{
				// Could not find path with current vectors
				float maxDistance = 0.001f;
				float originalDist = Vector3.Distance (startPosition, targetPosition);

				NavMeshHit hit = new NavMeshHit ();
				for (maxDistance = 0.001f; maxDistance < originalDist; maxDistance += 0.05f)
				{
					if (NavMesh.SamplePosition (startPosition, out hit, maxDistance, -1))
					{
						startPosition = hit.position;
						break;
					}
				}

				bool foundNewEnd = false;
				for (maxDistance = 0.001f; maxDistance < originalDist; maxDistance += 0.05f)
				{
					if (NavMesh.SamplePosition (targetPosition, out hit, maxDistance, -1))
					{
						targetPosition = hit.position;
						foundNewEnd = true;
						break;
					}
				}

				if (!foundNewEnd)
				{
					return new Vector3[0];
				}

				NavMesh.CalculatePath (startPosition, targetPosition, -1, _path);
			}

			List<Vector3> pointArray = new List<Vector3>();
			foreach (Vector3 corner in _path.corners)
			{
				pointArray.Add (corner);
			}
			if (pointArray.Count > 1 && pointArray[0].x == startPosition.x && pointArray[0].z == startPosition.z)
			{
				pointArray.RemoveAt (0);
			}
			else if (pointArray.Count == 0)
			{
				pointArray.Clear ();
				pointArray.Add (targetPosition);
			}

			return (pointArray.ToArray ());
		}


		public override string GetPrefabName ()
		{
			return ("NavMeshSegment");
		}


		public override void SetVisibility (bool visibility)
		{
			NavMeshSegment[] navMeshSegments = FindObjectsOfType (typeof (NavMeshSegment)) as NavMeshSegment[];
			
			#if UNITY_EDITOR
			Undo.RecordObjects (navMeshSegments, "NavMesh visibility");
			#endif
			
			foreach (NavMeshSegment navMeshSegment in navMeshSegments)
			{
				if (visibility)
				{
					navMeshSegment.Show ();
				}
				else
				{
					navMeshSegment.Hide ();
				}
				
				#if UNITY_EDITOR
				UnityVersionHandler.CustomSetDirty (navMeshSegment, true);
				#endif
			}
		}

	}

}