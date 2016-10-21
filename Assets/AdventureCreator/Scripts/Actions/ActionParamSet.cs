/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionParamSet.cs"
 * 
 *	This action sets the value of an ActionList's parameter.
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
	public class ActionParamSet : Action
	{

		public ActionListSource actionListSource = ActionListSource.InScene;
		
		public ActionList actionList;
		public int actionListConstantID;
		
		public ActionListAsset actionListAsset;

		public bool changeOwn;
		public int parameterID = -1;
		
		public int intValue;
		public float floatValue;
		public string stringValue;

		public GameObject gameobjectValue;
		public int gameObjectConstantID;

		public Object unityObjectValue;
		
		private ActionParameter _parameter;
		#if UNITY_EDITOR
		[SerializeField] private string parameterLabel = "";
		#endif
		
		
		public ActionParamSet ()
		{
			this.isDisplayed = true;
			category = ActionCategory.ActionList;
			title = "Set parameter";
			description = "Sets the value of a parameter in an ActionList.";
		}
		
		
		override public void AssignValues (List<ActionParameter> parameters)
		{
			if (!changeOwn)
			{
				if (actionListSource == ActionListSource.InScene)
				{
					actionList = AssignFile <ActionList> (actionListConstantID, actionList);
					if (actionList != null)
					{
						if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null)
						{
							if (actionList.syncParamValues && actionList.assetFile.useParameters)
							{
								_parameter = GetParameterWithID (actionList.assetFile.parameters, parameterID);
							}
							else
							{
								_parameter = GetParameterWithID (actionList.parameters, parameterID);
							}
						}
						else if (actionList.source == ActionListSource.InScene && actionList.useParameters)
						{
							_parameter = GetParameterWithID (actionList.parameters, parameterID);
						}
					}
				}
				else if (actionListSource == ActionListSource.AssetFile)
				{
					if (actionListAsset != null)
					{
						_parameter = GetParameterWithID (actionListAsset.parameters, parameterID);

						if (_parameter.parameterType == ParameterType.GameObject && !isAssetFile && gameobjectValue != null && gameObjectConstantID == 0)
						{
							if (gameobjectValue.GetComponent <ConstantID>())
							{
								gameObjectConstantID = gameobjectValue.GetComponent <ConstantID>().constantID;
							}
							else
							{
								ACDebug.LogWarning ("The GameObject '" + gameobjectValue.name + "' must have a Constant ID component in order to be passed as a parameter to an asset file.", gameobjectValue);
							}
						}
					}
				}
			}
			else
			{
				_parameter = GetParameterWithID (parameters, parameterID);

				if (_parameter.parameterType == ParameterType.GameObject && isAssetFile && gameobjectValue != null && gameObjectConstantID == 0)
				{
					if (gameobjectValue.GetComponent <ConstantID>())
					{
						gameObjectConstantID = gameobjectValue.GetComponent <ConstantID>().constantID;
					}
					else
					{
						ACDebug.LogWarning ("The GameObject '" + gameobjectValue.name + "' must have a Constant ID component in order to be passed as a parameter to an asset file.", gameobjectValue);
					}
				}
			}

			gameobjectValue = AssignFile (gameObjectConstantID, gameobjectValue);
		}
		
		
		override public float Run ()
		{
			if (_parameter == null)
			{
				ACDebug.LogWarning ("Cannot set parameter value since it cannot be found!");
				return 0f;
			}
			
			if (_parameter.parameterType == ParameterType.Boolean ||
			    _parameter.parameterType == ParameterType.Integer ||
			    _parameter.parameterType == ParameterType.GlobalVariable ||
			    _parameter.parameterType == ParameterType.LocalVariable ||
			    _parameter.parameterType == ParameterType.InventoryItem)
			{
				_parameter.intValue = intValue;
			}
			else if (_parameter.parameterType == ParameterType.Float)
			{
				_parameter.floatValue = floatValue;
			}
			else if (_parameter.parameterType == ParameterType.String)
			{
				_parameter.stringValue = stringValue;
			}
			else if (_parameter.parameterType == ParameterType.GameObject)
			{
				_parameter.gameObject = gameobjectValue;
				_parameter.intValue = gameObjectConstantID;
			}
			else if (_parameter.parameterType == ParameterType.UnityObject)
			{
				_parameter.objectValue = unityObjectValue;
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		override public void ShowGUI (List<ActionParameter> parameters)
		{
			changeOwn = EditorGUILayout.Toggle ("Change own?", changeOwn);
			if (changeOwn)
			{
				if (parameters == null || parameters.Count == 0)
				{
					EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
				}

				parameterID = Action.ChooseParameterGUI (parameters, parameterID);
				SetParamGUI (parameters);
			}
			else
			{
				actionListSource = (ActionListSource) EditorGUILayout.EnumPopup ("Source:", actionListSource);
				if (actionListSource == ActionListSource.InScene)
				{
					actionList = (ActionList) EditorGUILayout.ObjectField ("ActionList:", actionList, typeof (ActionList), true);
					
					actionListConstantID = FieldToID <ActionList> (actionList, actionListConstantID);
					actionList = IDToField <ActionList> (actionList, actionListConstantID, true);

					if (actionList != null)
					{
						if (actionList.source == ActionListSource.InScene)
						{
							if (actionList.useParameters && actionList.parameters.Count > 0)
							{
								parameterID = Action.ChooseParameterGUI (actionList.parameters, parameterID);
								SetParamGUI (actionList.parameters);
							}
							else
							{
								EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
							}
						}
						else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null)
						{
							if (actionList.assetFile.useParameters && actionList.assetFile.parameters.Count > 0)
							{
								parameterID = Action.ChooseParameterGUI (actionList.assetFile.parameters, parameterID);
								SetParamGUI (actionList.assetFile.parameters);
							}
							else
							{
								EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
							}
						}
						else
						{
							EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
						}
					}
				}
				else if (actionListSource == ActionListSource.AssetFile)
				{
					actionListAsset = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList asset:", actionListAsset, typeof (ActionListAsset), true);

					if (actionListAsset != null)
					{
						if (actionListAsset.useParameters && actionListAsset.parameters.Count > 0)
						{
							parameterID = Action.ChooseParameterGUI (actionListAsset.parameters, parameterID);
							SetParamGUI (actionListAsset.parameters);
						}
						else
						{
							EditorGUILayout.HelpBox ("This ActionList Asset has no parameters defined!", MessageType.Warning);
						}
					}
				}
			}

			AfterRunningOption ();
		}
		
		
		private void SetParamGUI (List<ActionParameter> parameters)
		{
			if (parameters == null || parameters.Count == 0)
			{
				parameterLabel = "";
				return;
			}

			_parameter = GetParameterWithID (parameters, parameterID);

			if (_parameter == null)
			{
				parameterLabel = "";
				return;
			}

			parameterLabel = _parameter.label;

			if (_parameter.parameterType == ParameterType.Boolean)
			{
				bool boolValue = (intValue == 1) ? true : false;
				boolValue = EditorGUILayout.Toggle ("Set as:", boolValue);
				intValue = (boolValue) ? 1 : 0;
			}
			else if (_parameter.parameterType == ParameterType.Integer)
			{
				intValue = EditorGUILayout.IntField ("Set as:", intValue);
			}
			else if (_parameter.parameterType == ParameterType.Float)
			{
				floatValue = EditorGUILayout.FloatField ("Set as:", floatValue);
			}
			else if (_parameter.parameterType == ParameterType.String)
			{
				stringValue = EditorGUILayout.TextField ("Set as:", stringValue);
			}
			else if (_parameter.parameterType == ParameterType.GameObject)
			{
				gameobjectValue = (GameObject) EditorGUILayout.ObjectField ("Set to:", gameobjectValue, typeof (GameObject), true);

				gameObjectConstantID = FieldToID (gameobjectValue, gameObjectConstantID);
				gameobjectValue = IDToField (gameobjectValue, gameObjectConstantID, false);
			}
			else if (_parameter.parameterType == ParameterType.GlobalVariable)
			{
				if (AdvGame.GetReferences ().variablesManager == null || AdvGame.GetReferences ().variablesManager.vars == null || AdvGame.GetReferences ().variablesManager.vars.Count == 0)
				{
					EditorGUILayout.HelpBox ("No Global variables exist!", MessageType.Info);
				}
				else
				{
					intValue = ShowVarSelectorGUI (AdvGame.GetReferences ().variablesManager.vars, intValue);
				}
			}
			else if (_parameter.parameterType == ParameterType.UnityObject)
			{
				unityObjectValue = (Object) EditorGUILayout.ObjectField ("Set to:", unityObjectValue, typeof (Object), true);
			}
			else if (_parameter.parameterType == ParameterType.InventoryItem)
			{
				intValue = ShowInvSelectorGUI (intValue);
			}
			else if (_parameter.parameterType == ParameterType.LocalVariable)
			{
				if (isAssetFile)
				{
					EditorGUILayout.HelpBox ("Cannot access local variables from an asset file.", MessageType.Warning);
				}
				else if (KickStarter.localVariables == null || KickStarter.localVariables.localVars == null || KickStarter.localVariables.localVars.Count == 0)
				{
					EditorGUILayout.HelpBox ("No Local variables exist!", MessageType.Info);
				}
				else
				{
					intValue = ShowVarSelectorGUI (KickStarter.localVariables.localVars, intValue);
				}
			}
		}
		
		
		override public void AssignConstantIDs (bool saveScriptsToo)
		{
			AssignConstantID (gameobjectValue, gameObjectConstantID, 0);
		}
		
		
		override public string SetLabel ()
		{
			if (parameterLabel != "")
			{
				return (" (" + parameterLabel + ")");
			}
			return "";
		}


		private int ShowVarSelectorGUI (List<GVar> vars, int ID)
		{
			int variableNumber = -1;
			
			List<string> labelList = new List<string>();
			foreach (GVar _var in vars)
			{
				labelList.Add (_var.label);
			}
			
			variableNumber = GetVarNumber (vars, ID);
			
			if (variableNumber == -1)
			{
				// Wasn't found (variable was deleted?), so revert to zero
				ACDebug.LogWarning ("Previously chosen variable no longer exists!");
				variableNumber = 0;
				ID = 0;
			}
			
			variableNumber = EditorGUILayout.Popup ("Variable:", variableNumber, labelList.ToArray());
			ID = vars[variableNumber].id;
			
			return ID;
		}
		
		
		private int ShowInvSelectorGUI (int ID)
		{
			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			if (inventoryManager == null)
			{
				return ID;
			}
			
			int invNumber = -1;
			List<string> labelList = new List<string>();
			int i=0;
			foreach (InvItem _item in inventoryManager.items)
			{
				labelList.Add (_item.label);
				
				// If an item has been removed, make sure selected variable is still valid
				if (_item.id == ID)
				{
					invNumber = i;
				}
				
				i++;
			}
			
			if (invNumber == -1)
			{
				// Wasn't found (item was possibly deleted), so revert to zero
				ACDebug.LogWarning ("Previously chosen item no longer exists!");
				
				invNumber = 0;
				ID = 0;
			}
			
			invNumber = EditorGUILayout.Popup ("Inventory item:", invNumber, labelList.ToArray());
			ID = inventoryManager.items[invNumber].id;
			
			return ID;
		}
		
		
		private int GetVarNumber (List<GVar> vars, int ID)
		{
			int i = 0;
			foreach (GVar _var in vars)
			{
				if (_var.id == ID)
				{
					return i;
				}
				i++;
			}
			return -1;
		}

		#endif
		
	}
	
}