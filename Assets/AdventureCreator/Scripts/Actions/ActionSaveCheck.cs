/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionSaveCheck.cs"
 * 
 *	This Action creates and deletes save game profiles
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
	public class ActionSaveCheck : ActionCheck
	{

		public SaveCheck saveCheck = SaveCheck.NumberOfSaveGames;
		public bool includeAutoSaves = true;

		public int intValue;
		public int checkParameterID = -1;
		public IntCondition intCondition;


		public ActionSaveCheck ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Save;
			title = "Check";
			description = "Queries the number of save files or profiles created, or if saving is possible.";
		}
		
		
		override public void AssignValues (List<ActionParameter> parameters)
		{
			intValue = AssignInteger (parameters, checkParameterID, intValue);
		}
		
		
		override public ActionEnd End (List<AC.Action> actions)
		{
			int actualNumber = 0;

			if (saveCheck == SaveCheck.NumberOfSaveGames)
			{
				actualNumber = KickStarter.saveSystem.GetNumSaves (includeAutoSaves);
			}
			else if (saveCheck == SaveCheck.NumberOfProfiles)
			{
				actualNumber = KickStarter.options.GetNumProfiles ();
			}
			else if (saveCheck == SaveCheck.IsSlotEmpty)
			{
				return ProcessResult (!SaveSystem.DoesSaveExist (intValue), actions);
			}
			else if (saveCheck == SaveCheck.IsSavingPossible)
			{
				return ProcessResult (!PlayerMenus.IsSavingLocked (this), actions);
			}

			return ProcessResult (CheckCondition (actualNumber), actions);
		}
		
		
		private bool CheckCondition (int fieldValue)
		{
			if (intCondition == IntCondition.EqualTo)
			{
				if (fieldValue == intValue)
				{
					return true;
				}
			}
			else if (intCondition == IntCondition.NotEqualTo)
			{
				if (fieldValue != intValue)
				{
					return true;
				}
			}
			else if (intCondition == IntCondition.LessThan)
			{
				if (fieldValue < intValue)
				{
					return true;
				}
			}
			else if (intCondition == IntCondition.MoreThan)
			{
				if (fieldValue > intValue)
				{
					return true;
				}
			}

			return false;
		}
		
		
		#if UNITY_EDITOR
		
		override public void ShowGUI (List<ActionParameter> parameters)
		{
			saveCheck = (SaveCheck) EditorGUILayout.EnumPopup ("Check to make:", saveCheck);
			if (saveCheck == SaveCheck.NumberOfSaveGames)
			{
				includeAutoSaves = EditorGUILayout.Toggle ("Include auto-save?", includeAutoSaves);
			}

			if (saveCheck == SaveCheck.IsSlotEmpty)
			{
				checkParameterID = Action.ChooseParameterGUI ("Save ID:", parameters, checkParameterID, ParameterType.Integer);
				if (checkParameterID < 0)
				{
					intValue = EditorGUILayout.IntField ("Save ID:", intValue);
				}
			}
			else if (saveCheck != SaveCheck.IsSavingPossible)
			{
				intCondition = (IntCondition) EditorGUILayout.EnumPopup ("Value is:", intCondition);
				checkParameterID = Action.ChooseParameterGUI ("Integer:", parameters, checkParameterID, ParameterType.Integer);
				if (checkParameterID < 0)
				{
					intValue = EditorGUILayout.IntField ("Integer:", intValue);
				}
			}
		}
		
		
		public override string SetLabel ()
		{
			return (" (" + saveCheck.ToString () + ")");
		}
		
		#endif
		
	}
	
}