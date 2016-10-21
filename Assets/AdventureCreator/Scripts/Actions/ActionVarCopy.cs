/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionVarCopy.cs"
 * 
 *	This action is used to transfer the value of one Variable to another
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
	public class ActionVarCopy : Action
	{
		
		public int oldParameterID = -1;
		public int oldVariableID;
		public VariableLocation oldLocation;

		public int newParameterID = -1;
		public int newVariableID;
		public VariableLocation newLocation;

		#if UNITY_EDITOR
		private VariableType oldVarType = VariableType.Boolean;
		private VariableType newVarType = VariableType.Boolean;
		#endif

		private LocalVariables localVariables;


		public ActionVarCopy ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Variable;
			title = "Copy";
			description = "Copies the value of one Variable to another. This can be between Global and Local Variables, but only of those with the same type, such as Integer or Float.";
		}


		override public void AssignValues (List<ActionParameter> parameters)
		{
			oldVariableID = AssignVariableID (parameters, oldParameterID, oldVariableID);
			newVariableID = AssignVariableID (parameters, newParameterID, newVariableID);
		}


		override public void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
			}
			if (localVariables == null)
			{
				localVariables = KickStarter.localVariables;
			}
		}


		override public float Run ()
		{
			if (newVariableID != -1 && oldVariableID != -1)
			{
				GVar oldVar;
				if (oldLocation == VariableLocation.Global)
				{
					oldVar = GlobalVariables.GetVariable (oldVariableID);
				}
				else
				{
					oldVar = LocalVariables.GetVariable (oldVariableID, localVariables);
				}

				if (newLocation == VariableLocation.Local && !isAssetFile)
				{
					CopyVariable (LocalVariables.GetVariable (newVariableID, localVariables), oldVar);
				}
				else
				{
					GVar newVar = GlobalVariables.GetVariable (newVariableID);

					if (newVar != null)
					{
						CopyVariable (newVar, oldVar);
						newVar.Upload ();
					}
				}

				KickStarter.actionListManager.VariableChanged ();
			}

			return 0f;
		}

		
		private void CopyVariable (GVar newVar, GVar oldVar)
		{
			if (newVar == null || oldVar == null)
			{
				ACDebug.LogWarning ("Cannot copy variable since it cannot be found!");
				return;
			}

			if (oldLocation == VariableLocation.Global)
			{
				oldVar.Download ();
			}

			if (newVar.type == VariableType.Integer || newVar.type == VariableType.Boolean || newVar.type == VariableType.PopUp)
			{
				int oldValue = oldVar.val;

				if (oldVar.type == VariableType.Float)
				{
					oldValue = (int) oldVar.floatVal;
				}
				else if (oldVar.type == VariableType.String)
				{
					float oldValueAsFloat = 0f;
					float.TryParse (oldVar.textVal, out oldValueAsFloat);
					oldValue = (int) oldValueAsFloat;
				}

				newVar.SetValue (oldValue, SetVarMethod.SetValue);
			}
			else if (newVar.type == VariableType.Float)
			{
				float oldValue = oldVar.floatVal;

				if (oldVar.type == VariableType.Integer || oldVar.type == VariableType.Boolean || oldVar.type == VariableType.PopUp)
				{
					oldValue = (float) oldVar.val;
				}
				else if (oldVar.type == VariableType.String)
				{
					float.TryParse (oldVar.textVal, out oldValue);
				}

				newVar.SetFloatValue (oldValue, AC.SetVarMethod.SetValue);
			}
			else if (newVar.type == VariableType.String)
			{
				string oldValue = oldVar.GetValue ();
				newVar.SetStringValue (oldValue);
			}
		}
		
		
		#if UNITY_EDITOR
		
		override public void ShowGUI (List<ActionParameter> parameters)
		{
			// OLD

			if (isAssetFile)
			{
				oldLocation = VariableLocation.Global;
			}
			else
			{
				oldLocation = (VariableLocation) EditorGUILayout.EnumPopup ("'From' source:", oldLocation);
			}
			
			if (oldLocation == VariableLocation.Global)
			{
				if (AdvGame.GetReferences ().variablesManager)
				{
					oldVariableID = ShowVarGUI (AdvGame.GetReferences ().variablesManager.vars, parameters, ParameterType.GlobalVariable, oldVariableID, oldParameterID, false);
				}
			}
			else if (oldLocation == VariableLocation.Local)
			{
				if (localVariables)
				{
					oldVariableID = ShowVarGUI (localVariables.localVars, parameters, ParameterType.LocalVariable, oldVariableID, oldParameterID, false);
				}
			}

			EditorGUILayout.Space ();

			// NEW

			if (isAssetFile)
			{
				newLocation = VariableLocation.Global;
			}
			else
			{
				newLocation = (VariableLocation) EditorGUILayout.EnumPopup ("'To' source:", newLocation);
			}
			
			if (newLocation == VariableLocation.Global)
			{
				if (AdvGame.GetReferences ().variablesManager)
				{
					newVariableID = ShowVarGUI (AdvGame.GetReferences ().variablesManager.vars, parameters, ParameterType.GlobalVariable, newVariableID, newParameterID, true);
				}
			}
			else if (newLocation == VariableLocation.Local)
			{
				if (localVariables)
				{
					newVariableID = ShowVarGUI (localVariables.localVars, parameters, ParameterType.LocalVariable, newVariableID, newParameterID, true);
				}
			}

			// Types match?
			if (oldParameterID == -1 && newParameterID == -1 && newVarType != oldVarType)
			{
				EditorGUILayout.HelpBox ("The chosen Variables do not share the same Type - a conversion will be attemped", MessageType.Info);
			}

			AfterRunningOption ();
		}


		private int ShowVarGUI (List<GVar> vars, List<ActionParameter> parameters, ParameterType parameterType, int variableID, int parameterID, bool isNew)
		{
			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			
			int i = 0;
			int variableNumber = -1;

			if (vars.Count > 0)
			{
				foreach (GVar _var in vars)
				{
					labelList.Add (_var.label);
					
					// If a GlobalVar variable has been removed, make sure selected variable is still valid
					if (_var.id == variableID)
					{
						variableNumber = i;
					}
					
					i ++;
				}
				
				if (variableNumber == -1 && (parameters == null || parameters.Count == 0 || parameterID == -1))
				{
					// Wasn't found (variable was deleted?), so revert to zero
					ACDebug.LogWarning ("Previously chosen variable no longer exists!");
					variableNumber = 0;
					variableID = 0;
				}

				string label = "'From' variable:";
				if (isNew)
				{
					label = "'To' variable:";
				}

				parameterID = Action.ChooseParameterGUI (label, parameters, parameterID, parameterType);
				if (parameterID >= 0)
				{
					//variableNumber = 0;
					variableNumber = Mathf.Min (variableNumber, vars.Count-1);
					variableID = -1;
				}
				else
				{
					variableNumber = EditorGUILayout.Popup (label, variableNumber, labelList.ToArray());
					variableID = vars [variableNumber].id;
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("No variables exist!", MessageType.Info);
				variableID = -1;
				variableNumber = -1;
			}

			if (isNew)
			{
				newParameterID = parameterID;

				if (variableNumber >= 0)
				{
					newVarType = vars[variableNumber].type;
				}
			}
			else
			{
				oldParameterID = parameterID;

				if (variableNumber >= 0)
				{
					oldVarType = vars[variableNumber].type;
				}
			}

			return variableID;
		}


		override public string SetLabel ()
		{
			if (newLocation == VariableLocation.Local && !isAssetFile)
			{
				if (localVariables)
				{
					return GetLabelString (localVariables.localVars, newVariableID);
				}
			}
			else
			{
				if (AdvGame.GetReferences ().variablesManager)
				{
					return GetLabelString (AdvGame.GetReferences ().variablesManager.vars, newVariableID);
				}
			}
			
			return "";
		}


		private string GetLabelString (List<GVar> vars, int variableNumber)
		{
			string labelAdd = "";

			if (vars.Count > 0 && variableNumber > -1 && vars.Count > variableNumber)
			{
				labelAdd = " (" + vars [variableNumber].label + ")";
			}

			return labelAdd;
		}

		#endif

	}

}