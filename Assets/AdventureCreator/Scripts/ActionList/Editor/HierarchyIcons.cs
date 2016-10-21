using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	[InitializeOnLoad]
	public class HierarchyIcons
	{

		private static List<int> actionListIDs;
		private static List<int> rememberIDs;

		private static ActionList[] actionLists;
		private static ConstantID[] constantIDs;


		static HierarchyIcons ()
		{
			EditorApplication.update += UpdateCB;
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
		}


		private static void UpdateCB ()
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().settingsManager && !AdvGame.GetReferences ().settingsManager.showHierarchyIcons)
			{
				return;
			}

			actionLists = Object.FindObjectsOfType (typeof (ActionList)) as ActionList[];

			actionListIDs = new List<int>();
			foreach (ActionList actionList in actionLists)
			{
				actionListIDs.Add (actionList.gameObject.GetInstanceID ());
			}

			constantIDs = Object.FindObjectsOfType (typeof (ConstantID)) as ConstantID[];

			rememberIDs = new List<int>();
			foreach (ConstantID constantID in constantIDs)
			{
				rememberIDs.Add (constantID.gameObject.GetInstanceID());
			}
		}


		private static void HierarchyItemCB (int instanceID, Rect selectionRect)
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().settingsManager && !AdvGame.GetReferences ().settingsManager.showHierarchyIcons)
			{
				return;
			}

			// place the icon to the right of the list:
			Rect r = new Rect (selectionRect);
			r.x = r.width - 20;
			r.width = 18;

			if (actionListIDs != null && actionListIDs.Contains (instanceID))
			{
				foreach (ActionList actionList in actionLists)
				{
					if (actionList != null && actionList.gameObject.GetInstanceID () == instanceID)
					{
						if (GUI.Button (r, "", Resource.NodeSkin.customStyles[13]))
						{
							ActionListEditorWindow.Init (actionList);
							return;
						}
					}
				}
			}

			r.x -= 40;
			if (rememberIDs != null && rememberIDs.Contains (instanceID))
			{
				foreach (ConstantID constantID in constantIDs)
				{
					if (constantID != null && constantID.gameObject.GetInstanceID () == instanceID)
					{
						GUI.Label (r, "", Resource.NodeSkin.customStyles[14]);
						return;
					}
				}
			}
		}
	}

}