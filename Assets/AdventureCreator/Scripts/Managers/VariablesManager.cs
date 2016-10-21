/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"VariablesManager.cs"
 * 
 *	This script handles the "Variables" tab of the main wizard.
 *	Boolean and integer, which can be used regardless of scene, are defined here.
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

	/**
	 * Handles the "Variables" tab of the Game Editor window.
	 * All global variables are defined here. Local variables are also managed here, but they are stored within the LocalVariables component on the GameEngine prefab.
	 * When the game begins, global variables are transferred to the RuntimeVariables component on the PersistentEngine prefab.
	 */
	[System.Serializable]
	public class VariablesManager : ScriptableObject
	{

		/** A List of the game's global variables */
		public List<GVar> vars = new List<GVar>();
		/** A List of preset values that the variables can be bulk-assigned to */
		public List<VarPreset> varPresets = new List<VarPreset>();
		/** If True, then the Variables Manager GUI will show the live values of each variable, rather than their default values */
		public bool updateRuntime = true;

		
		#if UNITY_EDITOR

		private int chosenPresetID = 0;

		private GVar selectedVar;
		private int sideVar = -1;
		private VariableLocation sideVarLocation = VariableLocation.Global;
		private string[] boolType = {"False", "True"};
		private string filter = "";

		private Vector2 scrollPos;
		private bool showGlobal = true;
		private bool showLocal = false;


		/**
		 * Shows the GUI.
		 */
		public void ShowGUI ()
		{
			string sceneName = MultiSceneChecker.EditActiveScene ();
			if (sceneName != "")
			{
				EditorGUILayout.LabelField ("Editing scene: '" + sceneName + "'",  CustomStyles.subHeader);
				EditorGUILayout.Space ();
			}

			EditorGUILayout.Space ();
			GUILayout.BeginHorizontal ();

			string label = (vars.Count > 0) ? ("Global (" + vars.Count + ")") : "Global";
			if (GUILayout.Toggle (showGlobal, label, "toolbarbutton"))
			{
				SetTab (0);
			}

			label = (KickStarter.localVariables != null && KickStarter.localVariables.localVars.Count > 0) ? ("Local (" +  KickStarter.localVariables.localVars.Count + ")") : "Local";
			if (GUILayout.Toggle (showLocal, label, "toolbarbutton"))
			{
				SetTab (1);
			}

			GUILayout.EndHorizontal ();
			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical ( CustomStyles.thinBox);
			EditorGUILayout.LabelField ("Editor settings",  CustomStyles.subHeader);
			EditorGUILayout.Space ();
			updateRuntime = CustomGUILayout.Toggle ("Show realtime values?", updateRuntime, "AC.KickStarter.variablesManager.updateRuntime");
			filter = EditorGUILayout.TextField ("Filter by name:", filter);
			EditorGUILayout.EndVertical ();

			EditorGUILayout.Space ();

			if (showGlobal)
			{
				varPresets = ShowPresets (varPresets, vars, VariableLocation.Global);

				if (Application.isPlaying && updateRuntime && KickStarter.runtimeVariables != null)
				{
					ShowVarList (KickStarter.runtimeVariables.globalVars, VariableLocation.Global, false);
				}
				else
				{
					ShowVarList (vars, VariableLocation.Global, true);

					foreach (VarPreset varPreset in varPresets)
					{
						varPreset.UpdateCollection (vars);
					}
				}
			}
			else if (showLocal)
			{
				if (KickStarter.localVariables != null)
				{
					KickStarter.localVariables.varPresets = ShowPresets (KickStarter.localVariables.varPresets, KickStarter.localVariables.localVars, VariableLocation.Local);

					if (Application.isPlaying && updateRuntime)
					{
						ShowVarList (KickStarter.localVariables.localVars, VariableLocation.Local, false);
					}
					else
					{
						ShowVarList (KickStarter.localVariables.localVars, VariableLocation.Local, true);
					}
				}
				else
				{
					EditorGUILayout.LabelField ("Local variables",  CustomStyles.subHeader);
					EditorGUILayout.HelpBox ("A GameEngine prefab must be present in the scene before Local variables can be defined", MessageType.Info);
				}
			}

			EditorGUILayout.Space ();
			if (selectedVar != null && (!Application.isPlaying || !updateRuntime))
			{
				int i = selectedVar.id;
				if (vars.Contains (selectedVar))
				{
					ShowVarGUI (VariableLocation.Global, varPresets, "AC.GlobalVariables.GetVariable (" + i + ")");
				}
				else if (KickStarter.localVariables != null && KickStarter.localVariables.localVars.Contains (selectedVar))
				{
					ShowVarGUI (VariableLocation.Local, KickStarter.localVariables.varPresets, "AC.LocalVariables.GetVariable (" + i + ")");
				}
			}

			if (GUI.changed)
			{
				EditorUtility.SetDirty (this);

				if (KickStarter.localVariables != null)
				{
					UnityVersionHandler.CustomSetDirty (KickStarter.localVariables);
				}
			}
		}


		private void ResetFilter ()
		{
			filter = "";
		}


		private void SideMenu (GVar _var, List<GVar> _vars, VariableLocation location)
		{
			GenericMenu menu = new GenericMenu ();
			sideVar = _vars.IndexOf (_var);
			sideVarLocation = location;

			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			if (_vars.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			}
			if (sideVar > 0 || sideVar < _vars.Count-1)
			{
				menu.AddSeparator ("");
			}
			if (sideVar > 0)
			{
				menu.AddItem (new GUIContent ("Move up"), false, Callback, "Move up");
			}
			if (sideVar < _vars.Count-1)
			{
				menu.AddItem (new GUIContent ("Move down"), false, Callback, "Move down");
			}
			
			menu.ShowAsContext ();
		}
		
		
		private void Callback (object obj)
		{
			if (sideVar >= 0)
			{
				ResetFilter ();
				List<GVar> _vars = new List<GVar>();

				if (sideVarLocation == VariableLocation.Global)
				{
					_vars = vars;
				}
				else
				{
					_vars = KickStarter.localVariables.localVars;
				}
				GVar tempVar = _vars[sideVar];

				switch (obj.ToString ())
				{
				case "Insert after":
					Undo.RecordObject (this, "Insert variable");
					_vars.Insert (sideVar+1, new GVar (GetIDArray (_vars)));
					DeactivateAllVars ();
					break;
					
				case "Delete":
					Undo.RecordObject (this, "Delete variable");
					_vars.RemoveAt (sideVar);
					DeactivateAllVars ();
					break;

				case "Move up":
					Undo.RecordObject (this, "Move variable up");
					_vars.RemoveAt (sideVar);
					_vars.Insert (sideVar-1, tempVar);
					break;

				case "Move down":
					Undo.RecordObject (this, "Move variable down");
					_vars.RemoveAt (sideVar);
					_vars.Insert (sideVar+1, tempVar);
					break;
				}
			}

			sideVar = -1;

			if (sideVarLocation == AC.VariableLocation.Global)
			{
				EditorUtility.SetDirty (this);
				AssetDatabase.SaveAssets ();
			}
			else
			{
				if (KickStarter.localVariables)
				{
					EditorUtility.SetDirty (KickStarter.localVariables);
				}
			}
		}


		private void ActivateVar (GVar var)
		{
			if (selectedVar != var)
			{
				var.isEditing = true;
				selectedVar = var;
				EditorGUIUtility.editingTextField = false;
			}
		}
		
		
		private void DeactivateAllVars ()
		{
			if (KickStarter.localVariables)
			{
				foreach (GVar var in KickStarter.localVariables.localVars)
				{
					var.isEditing = false;
				}
			}

			foreach (GVar var in vars)
			{
				var.isEditing = false;
			}
			selectedVar = null;
			EditorGUIUtility.editingTextField = false;
		}


		private int[] GetIDArray (List<GVar> _vars)
		{
			// Returns a list of id's in the list
			
			List<int> idArray = new List<int>();
			
			foreach (GVar variable in _vars)
			{
				idArray.Add (variable.id);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}


		private int[] GetIDArray (List<VarPreset> _varPresets)
		{
			// Returns a list of id's in the list
			
			List<int> idArray = new List<int>();
			
			foreach (VarPreset _varPreset in _varPresets)
			{
				idArray.Add (_varPreset.ID);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}


		private void ShowVarList (List<GVar> _vars, VariableLocation location, bool allowEditing)
		{
			EditorGUILayout.BeginVertical ( CustomStyles.thinBox);
			EditorGUILayout.LabelField (location + " variables",  CustomStyles.subHeader);
			EditorGUILayout.Space ();

			scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (Mathf.Min (_vars.Count * 21, 235f)+5));
			foreach (GVar _var in _vars)
			{
				if (filter == "" || _var.label.ToLower ().Contains (filter.ToLower ()))
				{
					EditorGUILayout.BeginHorizontal ();
					
					string buttonLabel = _var.id + ": ";
					if (buttonLabel == "")
					{
						_var.label += "(Untitled)";	
					}
					else
					{
						buttonLabel += _var.label;

						if (buttonLabel.Length > 30)
						{
							buttonLabel = buttonLabel.Substring (0, 30);
						}
					}

					string varValue = _var.GetValue ();
					if (varValue.Length > 20)
					{
						varValue = varValue.Substring (0, 20);
					}

					buttonLabel += " (" + _var.type.ToString () + " - " + varValue + ")";

					if (allowEditing)
					{
						if (GUILayout.Toggle (_var.isEditing, buttonLabel, "Button"))
						{
							if (selectedVar != _var)
							{
								DeactivateAllVars ();
								ActivateVar (_var);
							}
						}
						
						if (GUILayout.Button (Resource.CogIcon, GUILayout.Width (20f), GUILayout.Height (15f)))
						{
							SideMenu (_var, _vars, location);
						}
					}
					else
					{
						GUILayout.Label (buttonLabel, "Button");
					}
					
					EditorGUILayout.EndHorizontal ();
				}
			}
			EditorGUILayout.EndScrollView ();

			if (allowEditing)
			{
				EditorGUILayout.Space ();
				if (GUILayout.Button("Create new " + location + " variable"))
				{
					ResetFilter ();
					Undo.RecordObject (this, "Add " + location + " variable");
					_vars.Add (new GVar (GetIDArray (_vars)));
					DeactivateAllVars ();
					ActivateVar (_vars [_vars.Count-1]);
				}
			}

			EditorGUILayout.EndVertical ();
		}


		private void ShowVarGUI (VariableLocation location, List<VarPreset> _varPresets = null, string apiPrefix = "")
		{
			EditorGUILayout.BeginVertical ( CustomStyles.thinBox);
			EditorGUILayout.LabelField (location + " variable '" + selectedVar.label + "' properties",  CustomStyles.subHeader);
			EditorGUILayout.Space ();
			
			selectedVar.label = CustomGUILayout.TextField ("Label:", selectedVar.label, apiPrefix + ".label");
			selectedVar.type = (VariableType) CustomGUILayout.EnumPopup ("Type:", selectedVar.type, apiPrefix + ".type");

			if (location == VariableLocation.Local)
			{
				EditorGUILayout.LabelField ("Replacement token:", "[localvar:" + selectedVar.id.ToString () + "]");
			}
			else
			{
				EditorGUILayout.LabelField ("Replacement token:", "[var:" + selectedVar.id.ToString () + "]");
			}
			
			if (selectedVar.type == VariableType.Boolean)
			{
				if (selectedVar.val != 1)
				{
					selectedVar.val = 0;
				}
				selectedVar.val = CustomGUILayout.Popup ("Initial value:", selectedVar.val, boolType, apiPrefix + ".val");
			}
			else if (selectedVar.type == VariableType.Integer)
			{
				selectedVar.val = CustomGUILayout.IntField ("Initial value:", selectedVar.val, apiPrefix + ".val");
			}
			else if (selectedVar.type == VariableType.PopUp)
			{
				selectedVar.popUps = PopupsGUI (selectedVar.popUps);
				selectedVar.val = CustomGUILayout.Popup ("Initial value:", selectedVar.val, selectedVar.popUps, apiPrefix + ".val");
				selectedVar.canTranslate = EditorGUILayout.Toggle ("Values can be translated?", selectedVar.canTranslate);
			}
			else if (selectedVar.type == VariableType.String)
			{
				selectedVar.textVal = CustomGUILayout.TextField ("Initial value:", selectedVar.textVal, apiPrefix + ".textVal");
				selectedVar.canTranslate = EditorGUILayout.Toggle ("Value can be translated?", selectedVar.canTranslate);
			}
			else if (selectedVar.type == VariableType.Float)
			{
				selectedVar.floatVal = CustomGUILayout.FloatField ("Initial value:", selectedVar.floatVal, apiPrefix + ".floatVal");
			}

			if (_varPresets != null)
			{
				foreach (VarPreset _varPreset in _varPresets)
				{
					// Local
					string apiPrefix2 = (location == VariableLocation.Local) ? 
										"AC.KickStarter.localVariables.GetPreset (" + _varPreset.ID + ").GetPresetValue (" + selectedVar.id + ")" :
										"AC.KickStarter.runtimeVariables.GetPreset (" + _varPreset.ID + ").GetPresetValue (" + selectedVar.id + ")";

					_varPreset.UpdateCollection (selectedVar);

					string label = "'" + _varPreset.label + "' value:";
					PresetValue presetValue = _varPreset.GetPresetValue (selectedVar);
					if (selectedVar.type == VariableType.Boolean)
					{
						presetValue.val = CustomGUILayout.Popup (label, presetValue.val, boolType, apiPrefix2 + ".val");
					}
					else if (selectedVar.type == VariableType.Integer)
					{
						presetValue.val = CustomGUILayout.IntField (label, presetValue.val, apiPrefix2 + ".val");
					}
					else if (selectedVar.type == VariableType.PopUp)
					{
						presetValue.val = CustomGUILayout.Popup (label, presetValue.val, selectedVar.popUps, apiPrefix2 + ".val");
					}
					else if (selectedVar.type == VariableType.String)
					{
						presetValue.textVal = CustomGUILayout.TextField (label, presetValue.textVal, apiPrefix2 + ".textVal");
					}
					else if (selectedVar.type == VariableType.Float)
					{
						presetValue.floatVal = CustomGUILayout.FloatField (label, presetValue.floatVal, apiPrefix2 + ".floatVal");
					}
				}
			}

			if (location == VariableLocation.Local)
			{
				selectedVar.link = VarLink.None;
			}
			else
			{
				EditorGUILayout.Space ();
				selectedVar.link = (VarLink) CustomGUILayout.EnumPopup ("Link to:", selectedVar.link, apiPrefix + ".link");
				if (selectedVar.link == VarLink.PlaymakerGlobalVariable)
				{
					if (PlayMakerIntegration.IsDefinePresent ())
					{
						selectedVar.pmVar = CustomGUILayout.TextField ("Playmaker Global Variable:", selectedVar.pmVar, apiPrefix + ".pmVar");
						selectedVar.updateLinkOnStart = CustomGUILayout.Toggle ("Use PM for initial value?", selectedVar.updateLinkOnStart, apiPrefix + ".updateLinkOnStart");
					}
					else
					{
						EditorGUILayout.HelpBox ("The 'PlayMakerIsPresent' Scripting Define Symbol must be listed in the\nPlayer Settings. Please set it from Edit -> Project Settings -> Player", MessageType.Warning);
					}
				}
				else if (selectedVar.link == VarLink.OptionsData)
				{
					EditorGUILayout.HelpBox ("This Variable will be stored in PlayerPrefs, and not in saved game files.", MessageType.Info);
				}
			}
			EditorGUILayout.EndVertical ();
		}


		public static string[] PopupsGUI (string[] popUps)
		{
			List<string> popUpList = new List<string>();
			if (popUps != null && popUps.Length > 0)
			{
				foreach (string p in popUps)
				{
					popUpList.Add (p);
				}
			}

			int numValues = popUpList.Count;
			numValues = EditorGUILayout.IntField ("Number of values:", numValues);
			if (numValues < 0)
			{
				numValues = 0;
			}
			
			if (numValues < popUpList.Count)
			{
				popUpList.RemoveRange (numValues, popUpList.Count - numValues);
			}
			else if (numValues > popUpList.Count)
			{
				if (numValues > popUpList.Capacity)
				{
					popUpList.Capacity = numValues;
				}
				for (int i=popUpList.Count; i<numValues; i++)
				{
					popUpList.Add ("");
				}
			}
			
			for (int i=0; i<popUpList.Count; i++)
			{
				popUpList[i] = EditorGUILayout.TextField (i.ToString ()+":", popUpList[i]);
			}

			return popUpList.ToArray ();
		}


		private void SetTab (int tab)
		{
			if (tab == 0)
			{
				if (showLocal)
				{
					selectedVar = null;
					EditorGUIUtility.editingTextField = false;
				}
				showGlobal = true;
				showLocal = false;
			}
			else if (tab == 1)
			{
				if (showGlobal)
				{
					selectedVar = null;
					EditorGUIUtility.editingTextField = false;
				}
				showLocal = true;
				showGlobal = false;
			}
		}


		private List<VarPreset> ShowPresets (List<VarPreset> _varPresets, List<GVar> _vars, VariableLocation location)
		{
			if (_vars == null || _vars.Count == 0)
			{
				return _varPresets;
			}

			if (!Application.isPlaying || _varPresets.Count > 0)
			{
				EditorGUILayout.BeginVertical ( CustomStyles.thinBox);
				EditorGUILayout.LabelField ("Preset configurations",  CustomStyles.subHeader);
				EditorGUILayout.Space ();
			}

			List<string> labelList = new List<string>();
			
			int i = 0;
			int presetNumber = -1;
			
			if (_varPresets.Count > 0)
			{
				foreach (VarPreset _varPreset in _varPresets)
				{
					if (_varPreset.label != "")
					{
						labelList.Add (i.ToString () + ": " + _varPreset.label);
					}
					else
					{
						labelList.Add (i.ToString () + ": (Untitled)");
					}
					
					if (_varPreset.ID == chosenPresetID)
					{
						presetNumber = i;
					}
					i++;
				}
				
				if (presetNumber == -1)
				{
					chosenPresetID = 0;
				}
				else if (presetNumber >= _varPresets.Count)
				{
					presetNumber = Mathf.Max (0, _varPresets.Count - 1);
				}
				else
				{
					presetNumber = EditorGUILayout.Popup ("Created presets:", presetNumber, labelList.ToArray());
					chosenPresetID = _varPresets[presetNumber].ID;
				}
			}
			else
			{
				chosenPresetID = presetNumber = -1;
			}

			if (presetNumber >= 0)
			{
				string apiPrefix = ((location == VariableLocation.Local) ? "AC.KickStarter.localVariables.GetPreset (" + chosenPresetID + ")" : "AC.KickStarter.runtimeVariables.GetPreset (" + chosenPresetID + ")");

				if (!Application.isPlaying)
				{
					_varPresets [presetNumber].label = CustomGUILayout.TextField ("Preset name:", _varPresets [presetNumber].label, apiPrefix + ".label");
				}

				EditorGUILayout.BeginHorizontal ();
				if (!Application.isPlaying)
				{
					GUI.enabled = false;
				}
				if (GUILayout.Button ("Bulk-assign"))
				{
					if (presetNumber >= 0 && _varPresets.Count > presetNumber)
					{
						if (location == VariableLocation.Global)
						{
							if (KickStarter.runtimeVariables)
							{
								KickStarter.runtimeVariables.AssignFromPreset (_varPresets [presetNumber]);
								ACDebug.Log ("Global variables updated to " + _varPresets [presetNumber].label);
							}
						}
						else if (location == VariableLocation.Local)
						{
							if (KickStarter.localVariables)
							{
								KickStarter.localVariables.AssignFromPreset (_varPresets [presetNumber]);
								ACDebug.Log ("Local variables updated to " + _varPresets [presetNumber].label);
							}
						}
					}
				}

				GUI.enabled = !Application.isPlaying;
				if (GUILayout.Button ("Delete"))
				{
					_varPresets.RemoveAt (presetNumber);
					presetNumber = 0;
					chosenPresetID = 0;
				}

				GUI.enabled = true;
				EditorGUILayout.EndHorizontal ();
			}

			if (!Application.isPlaying)
			{
				if (GUILayout.Button ("Create new preset"))
				{
					VarPreset newVarPreset = new VarPreset (_vars, GetIDArray (_varPresets));
					_varPresets.Add (newVarPreset);
					chosenPresetID = newVarPreset.ID;
				}
			}

			if (!Application.isPlaying || _varPresets.Count > 0)
			{
				EditorGUILayout.EndVertical ();
			}

			EditorGUILayout.Space ();

			return _varPresets;
		}

		#endif


		/**
		 * <summary>Gets a global variable</summary>
		 * <param name = "_id">The ID number of the global variable to find</param>
		 * <returns>The global variable</returns>
		 */
		public GVar GetVariable (int _id)
		{
			foreach (GVar _var in vars)
			{
				if (_var.id == _id)
				{
					return _var;
				}
			}
			return null;
		}

	}

}