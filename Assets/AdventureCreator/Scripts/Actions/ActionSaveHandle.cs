/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionSaveHandle.cs"
 * 
 *	This Action saves and loads save game files
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
	public class ActionSaveHandle : Action
	{

		public SaveHandling saveHandling = SaveHandling.LoadGame;
		public SelectSaveType selectSaveType = SelectSaveType.Autosave;

		public int saveIndex = 0;
		public int saveIndexParameterID = -1;
		
		public int varID;
		public int slotVarID;
		
		public string menuName = "";
		public string elementName = "";

		public bool updateLabel = false;
		public bool customLabel = false;

		public bool doSelectiveLoad = false;
		public SelectiveLoad selectiveLoad = new SelectiveLoad ();

		
		public ActionSaveHandle ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Save;
			title = "Save or load";
			description = "Saves and loads save-game files";
		}
		
		
		override public void AssignValues (List<ActionParameter> parameters)
		{
			saveIndex = AssignInteger (parameters, saveIndexParameterID, saveIndex);
		}
		
		
		override public float Run ()
		{
			if ((saveHandling == SaveHandling.LoadGame || saveHandling == SaveHandling.ContinueFromLastSave) && doSelectiveLoad)
			{
				KickStarter.saveSystem.SetSelectiveLoadOptions (selectiveLoad);
			}

			string newSaveLabel = "";
			if (customLabel && ((updateLabel && saveHandling == SaveHandling.OverwriteExistingSave) || saveHandling == AC.SaveHandling.SaveNewGame))
			{
				if (selectSaveType != SelectSaveType.Autosave)
				{
					GVar gVar = GlobalVariables.GetVariable (varID);
					if (gVar != null)
					{
						newSaveLabel = gVar.textVal;
					}
					else
					{
						ACDebug.LogWarning ("Could not " + saveHandling.ToString () + " - no variable found.");
						return 0f;
					}
				}
			}
			
			int i = Mathf.Max (0, saveIndex);

			if (saveHandling == SaveHandling.ContinueFromLastSave)
			{
				SaveSystem.ContinueGame ();
				return 0f;
			}

			if (saveHandling == SaveHandling.LoadGame || saveHandling == SaveHandling.OverwriteExistingSave)
			{
				if (selectSaveType == SelectSaveType.Autosave)
				{
					if (saveHandling == SaveHandling.LoadGame)
					{
						SaveSystem.LoadAutoSave ();
						return 0f;
					}
					else
					{
						if (!PlayerMenus.IsSavingLocked (this))
						{
							SaveSystem.SaveAutoSave ();
						}
						else
						{
							ACDebug.LogWarning ("Cannot save at this time - either blocking ActionLists, a Converation is active, or saving has been manually locked.");
						}
						return 0f;
					}
				}
				else if (selectSaveType == SelectSaveType.SlotIndexFromVariable)
				{
					GVar gVar = GlobalVariables.GetVariable (slotVarID);
					if (gVar != null)
					{
						i = gVar.val;
					}
					else
					{
						ACDebug.LogWarning ("Could not get save slot index - no variable found.");
						return 0f;
					}
				}
			}

			if (menuName != "" && elementName != "")
			{
				MenuElement menuElement = PlayerMenus.GetElementWithName (menuName, elementName);
				if (menuElement != null && menuElement is MenuSavesList)
				{
					MenuSavesList menuSavesList = (MenuSavesList) menuElement;
					i += menuSavesList.GetOffset ();
				}
				else
				{
					ACDebug.LogWarning ("Cannot find ProfilesList element '" + elementName + "' in Menu '" + menuName + "'.");
				}
			}
			else
			{
				ACDebug.LogWarning ("No SavesList element referenced when trying to find slot slot " + i.ToString ());
			}
			
			if (saveHandling == SaveHandling.LoadGame)
			{
				SaveSystem.LoadGame (i, -1, false);
			}
			else if (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame)
			{
				if (!PlayerMenus.IsSavingLocked (this))
				{
					if (saveHandling == SaveHandling.OverwriteExistingSave)
					{
						SaveSystem.SaveGame (i, -1, false, updateLabel, newSaveLabel);
					}
					else if (saveHandling == SaveHandling.SaveNewGame)
					{
						SaveSystem.SaveNewGame (updateLabel, newSaveLabel);
					}
				}
				else
				{
					ACDebug.LogWarning ("Cannot save at this time - either blocking ActionLists, a Converation is active, or saving has been manually locked.");
				}
			}
			return 0f;
		}


		public override ActionEnd End (List<Action> actions)
		{
			if (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame)
			{
				return base.End (actions);
			}
			return GenerateStopActionEnd ();
		}
		
		
		#if UNITY_EDITOR
		
		override public void ShowGUI (List<ActionParameter> parameters)
		{
			saveHandling = (SaveHandling) EditorGUILayout.EnumPopup ("Method:", saveHandling);
			
			if (saveHandling == SaveHandling.LoadGame || saveHandling == SaveHandling.OverwriteExistingSave)
			{
				string _action = "load";
				if (saveHandling == SaveHandling.OverwriteExistingSave)
				{
					_action = "overwrite";
				}
				
				selectSaveType = (SelectSaveType) EditorGUILayout.EnumPopup ("Save to " + _action + ":", selectSaveType);
				if (selectSaveType == SelectSaveType.SetSlotIndex)
				{
					saveIndexParameterID = Action.ChooseParameterGUI ("Slot index to " + _action + ":", parameters, saveIndexParameterID, ParameterType.Integer);
					if (saveIndexParameterID == -1)
					{
						saveIndex = EditorGUILayout.IntField ("Slot index to " + _action + ":", saveIndex);
					}
				}
				else if (selectSaveType == SelectSaveType.SlotIndexFromVariable)
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

				if (selectSaveType != SelectSaveType.Autosave)
				{
					EditorGUILayout.Space ();
					menuName = EditorGUILayout.TextField ("Menu with SavesList:", menuName);
					elementName = EditorGUILayout.TextField ("SavesList element:", elementName);
				}
			}

			if (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame)
			{
				if (selectSaveType != SelectSaveType.Autosave)
				{
					EditorGUILayout.Space ();
					if (saveHandling == SaveHandling.OverwriteExistingSave)
					{
						updateLabel = EditorGUILayout.Toggle ("Update label?", updateLabel);
					}
					if (updateLabel || saveHandling == SaveHandling.SaveNewGame)
					{
						customLabel = EditorGUILayout.Toggle ("With custom label?", customLabel);
						if (customLabel)
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
					}
				}
			}

			if (saveHandling == SaveHandling.LoadGame || saveHandling == SaveHandling.ContinueFromLastSave)
			{
				doSelectiveLoad = EditorGUILayout.ToggleLeft ("Selective loading?", doSelectiveLoad);
				if (doSelectiveLoad)
				{
					selectiveLoad.ShowGUI ();
				}
			}

			if (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame)
			{
				AfterRunningOption ();
			}
		}
		
		
		public override string SetLabel ()
		{
			return (" (" + saveHandling.ToString () + ")");
		}
		
		#endif
		
	}
	
}