﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionCharRename.cs"
 * 
 *	This action renames Hotspots. A "Remember NPC" script needs to be
 *	attached to the Character unless it is a Player prefab.
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

	[System.Serializable]
	public class ActionCharRename : Action
	{
		
		public int _charID = 0;
		public Char _char;
		public bool isPlayer;

		public string newName;
		public int lineID = -1;
		
		
		public ActionCharRename ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Character;
			title = "Rename";
			lineID = -1;
			description = "Changes the display name of a Character when subtitles are used.";
		}
		
		
		override public void AssignValues (List<ActionParameter> parameters)
		{
			_char = AssignFile <Char> (_charID, _char);

			if (isPlayer)
			{
				_char = KickStarter.player;
			}
		}
		
		
		override public float Run ()
		{
			if (_char && newName != "")
			{
				_char.SetName (newName, lineID);
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		override public void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);
			if (!isPlayer)
			{
				_char = (Char) EditorGUILayout.ObjectField ("Character:", _char, typeof (Char), true);
				
				_charID = FieldToID <Char> (_char, _charID);
				_char = IDToField <Char> (_char, _charID, true);
			}
			
			newName = EditorGUILayout.TextField ("New name:", newName);
			
			AfterRunningOption ();
		}


		override public void AssignConstantIDs (bool saveScriptsToo)
		{
			if (!isPlayer)
			{
				if (saveScriptsToo)
				{
					if (_char != null && _char.GetComponent <NPC>())
					{
						AddSaveScript <RememberNPC> (_char);
					}
				}

				AssignConstantID <Char> (_char, _charID, 0);
			}
		}

		
		override public string SetLabel ()
		{
			string labelAdd = "";
			
			if (_char && newName != "")
			{
				labelAdd = " (" + _char.name + " to " + newName + ")";
			}
			
			return labelAdd;
		}
		
		#endif
		
	}

}