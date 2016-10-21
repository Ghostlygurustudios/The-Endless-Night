/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionManageProfiles.cs"
 * 
 *	This Action creates, renames and and deletes save game profiles
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
	public class ActionManageProfiles : Action
	{

		public ManageProfileType manageProfileType = ManageProfileType.CreateProfile;
		public DeleteProfileType deleteProfileType = DeleteProfileType.ActiveProfile;

		public int profileIndex = 0;
		public int profileIndexParameterID = -1;

		public int varID;
		public int slotVarID;

		public bool useCustomLabel = false;

		public string menuName = "";
		public string elementName = "";

		
		public ActionManageProfiles ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Save;
			title = "Manage profiles";
			description = "Creates, renames and deletes save game profiles.";
		}
		
		
		override public void AssignValues (List<ActionParameter> parameters)
		{
			profileIndex = AssignInteger (parameters, profileIndexParameterID, profileIndex);
		}
		
		
		override public float Run ()
		{
			if (!KickStarter.settingsManager.useProfiles)
			{
				ACDebug.LogWarning ("Save game profiles are not enabled - please set in Settings Manager to use this Action.");
				return 0f;
			}

			string newProfileLabel = "";
			if ((manageProfileType == ManageProfileType.CreateProfile && useCustomLabel) || manageProfileType == ManageProfileType.RenameProfile)
			{
				GVar gVar = GlobalVariables.GetVariable (varID);
				if (gVar != null)
				{
					newProfileLabel = gVar.textVal;
				}
				else
				{
					ACDebug.LogWarning ("Could not " + manageProfileType.ToString () + " - no variable found.");
					return 0f;
				}
			}

			if (manageProfileType == ManageProfileType.CreateProfile)
			{
				KickStarter.options.CreateProfile (newProfileLabel);
			}
			else if (manageProfileType == ManageProfileType.DeleteProfile || manageProfileType == ManageProfileType.RenameProfile)
			{
				if (deleteProfileType == DeleteProfileType.ActiveProfile)
				{
					if (manageProfileType == ManageProfileType.DeleteProfile)
					{
						KickStarter.saveSystem.DeleteProfile ();
						return 0f;
					}
					else
					{
						KickStarter.options.RenameProfile (newProfileLabel);
						return 0f;
					}
				}

				int i = Mathf.Max (0, profileIndex);

				if (deleteProfileType == DeleteProfileType.SlotIndexFromVariable)
				{
					GVar gVar = GlobalVariables.GetVariable (slotVarID);
					if (gVar != null)
					{
						i = gVar.val;
					}
					else
					{
						ACDebug.LogWarning ("Could not create profile - no variable found.");
						return 0f;
					}
				}

				bool includeActive = true;
				if (menuName != "" && elementName != "")
				{
					MenuElement menuElement = PlayerMenus.GetElementWithName (menuName, elementName);
					if (menuElement != null && menuElement is MenuProfilesList)
					{
						MenuProfilesList menuProfilesList = (MenuProfilesList) menuElement;
						i += menuProfilesList.GetOffset ();
						includeActive = menuProfilesList.showActive;
					}
					else
					{
						ACDebug.LogWarning ("Cannot find ProfilesList element '" + elementName + "' in Menu '" + menuName + "'.");
					}
				}
				else
				{
					ACDebug.LogWarning ("No ProfilesList element referenced when trying to delete profile slot " + i.ToString ());
				}

				if (manageProfileType == ManageProfileType.DeleteProfile)
				{
					KickStarter.saveSystem.DeleteProfile (i, includeActive);
				}
				else
				{
					KickStarter.options.RenameProfile (newProfileLabel, i, includeActive);
				}
			}
			
			return 0f;
		}
		

		#if UNITY_EDITOR
		
		override public void ShowGUI (List<ActionParameter> parameters)
		{
			if (AdvGame.GetReferences ().settingsManager != null && !AdvGame.GetReferences ().settingsManager.useProfiles)
			{
				EditorGUILayout.HelpBox ("Save game profiles are not enabled - please set in Settings Manager to use this Action.", MessageType.Warning);
				AfterRunningOption ();
				return;
			}

			manageProfileType = (ManageProfileType) EditorGUILayout.EnumPopup ("Method:", manageProfileType);

			if (manageProfileType == ManageProfileType.CreateProfile)
			{
				useCustomLabel = EditorGUILayout.Toggle ("Use custom label?", useCustomLabel);
			}

			if ((manageProfileType == ManageProfileType.CreateProfile && useCustomLabel) || manageProfileType == AC.ManageProfileType.RenameProfile)
			{
				varID = AdvGame.GlobalVariableGUI ("Label as String variable:", varID);
				if (varID >= 0 && AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
				{
					GVar _var = AdvGame.GetReferences ().variablesManager.GetVariable (varID);
					if (_var != null && _var.type != VariableType.String)
					{
						EditorGUILayout.HelpBox ("The chosen Variable must be a String.", MessageType.Warning);
					}
				}
			}

			if (manageProfileType == ManageProfileType.DeleteProfile || manageProfileType == ManageProfileType.RenameProfile)
			{
				string _action = "delete";
				if (manageProfileType == ManageProfileType.RenameProfile)
				{
					_action = "rename";
				}

				deleteProfileType = (DeleteProfileType) EditorGUILayout.EnumPopup ("Profile to " + _action + ":", deleteProfileType);
				if (deleteProfileType == DeleteProfileType.SetSlotIndex)
				{
					profileIndexParameterID = Action.ChooseParameterGUI ("Slot index to " + _action + ":", parameters, profileIndexParameterID, ParameterType.Integer);
					if (profileIndexParameterID == -1)
					{
						profileIndex = EditorGUILayout.IntField ("Slot index to " + _action + ":", profileIndex);
					}
				}
				else if (deleteProfileType == DeleteProfileType.SlotIndexFromVariable)
				{
					slotVarID = AdvGame.GlobalVariableGUI ("Integer variable:", slotVarID);
					if (slotVarID >= 0 && AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
					{
						GVar _var = AdvGame.GetReferences ().variablesManager.GetVariable (slotVarID);
						if (_var != null && _var.type != VariableType.Integer)
						{
							EditorGUILayout.HelpBox ("The chosen Variable must be an Integer.", MessageType.Warning);
						}
					}
				}

				if (deleteProfileType != DeleteProfileType.ActiveProfile)
				{
					EditorGUILayout.Space ();
					menuName = EditorGUILayout.TextField ("Menu with ProfilesList:", menuName);
					elementName = EditorGUILayout.TextField ("ProfilesList element:", elementName);
				}
			}
			
			AfterRunningOption ();
		}
		
		
		public override string SetLabel ()
		{
			return (" (" + manageProfileType.ToString () + ")");
		}
		
		#endif
		
	}
	
}