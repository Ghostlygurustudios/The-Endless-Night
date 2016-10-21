/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionParameter.cs"
 * 
 *	This defines a parameter that can be used by ActionLists
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * A data container for an ActionList parameter. A parameter can change the value of an Action's public variables dynamically during gameplay, allowing the same Action to be repurposed for different tasks.
	 */
	[System.Serializable]
	public class ActionParameter
	{

		/** The display name in the Editor */
		public string label = "";
		/** A unique identifier */
		public int ID = 0;
		/** The type of variable it overrides (GameObject, InventoryItem, GlobalVariable, LocalVariable, String, Float, Integer, Boolean) */
		public ParameterType parameterType = ParameterType.GameObject;
		/** The new value or ID number, if parameterType = ParameterType.Integer / Boolean / LocalVariable / GlobalVariable / InventoryItem.  If parameterType = ParameterType.GameObject, it is the ConstantID number of the GameObject if it is not currently accessible */
		public int intValue = -1;
		/** The new value, if parameterType = ParameterType.Float */
		public float floatValue = 0f;
		/** The new value, if parameterType = ParameterType.String */
		public string stringValue = "";
		/** The new value, if parameterType = ParameterType.GameObject */
		public GameObject gameObject;
		/** The new value, if parameterType = ParameterType.UnityObject */
		public Object objectValue;


		/**
		 * <summary>A Constructor that generates a unique ID number.</summary>
		 * <param name = "idArray">An array of previously-used ID numbers, to ensure its own ID is unique.</param>
		 */
		public ActionParameter (int[] idArray)
		{
			label = "";
			ID = 0;
			intValue = -1;
			floatValue = 0f;
			stringValue = "";
			gameObject = null;
			objectValue = null;
			parameterType = ParameterType.GameObject;
			
			// Update id based on array
			foreach (int _id in idArray)
			{
				if (ID == _id)
					ID ++;
			}
			
			label = "Parameter " + (ID + 1).ToString ();
		}


		/**
		 * <summary>A Constructor that sets the ID number explicitly.</summary>
		 * <param name = "id">The unique identifier to assign</param>
		 */
		public ActionParameter (int id)
		{
			label = "";
			ID = id;
			intValue = -1;
			floatValue = 0f;
			stringValue = "";
			gameObject = null;
			objectValue = null;
			parameterType = ParameterType.GameObject;
			
			label = "Parameter " + (ID + 1).ToString ();
		}


		/**
		 * <summary>A Constructor that duplicates another ActionParameter.</summary>
		 */
		public ActionParameter (ActionParameter _actionParameter)
		{
			label = _actionParameter.label;
			ID = _actionParameter.ID;
			parameterType = _actionParameter.parameterType;

			intValue = -1;
			floatValue = 0f;
			stringValue = "";
			gameObject = null;
			objectValue = null;
		}


		/**
		 * <summary>Copies the "value" variables from another ActionParameter, without changing the type, ID, or label.</summary>
		 * <parameter name = "otherParameter">The ActionParameter to copy from</param>
		 */
		public void CopyValues (ActionParameter otherParameter)
		{
			intValue = otherParameter.intValue;
			floatValue = otherParameter.floatValue;
			stringValue = otherParameter.stringValue;
			gameObject = otherParameter.gameObject;
			objectValue = otherParameter.objectValue;
		}


		/**
		 * Resets the value that the parameter assigns.
		 */
		public void Reset ()
		{
			intValue = -1;
			floatValue = 0f;
			stringValue = "";
			gameObject = null;
			objectValue = null;
		}


		/**
		 * <summary>Checks if the parameter's value is an integer. This is the case if parameterType = ParameterType.GameObject, GlobalVariable, Integer, InventoryItem or LocalVariable.</summary>
		 * <returns>True if the parameter's value is an integer.</returns>
		 */
		public bool IsIntegerBased ()
		{
			if (parameterType == ParameterType.GameObject ||
			    parameterType == ParameterType.GlobalVariable ||
			    parameterType == ParameterType.Integer ||
				parameterType == ParameterType.Boolean ||
			    parameterType == ParameterType.InventoryItem ||
			    parameterType == ParameterType.LocalVariable)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Sets the intValue that the parameter assigns</summary>
		 * <param name = "_value">The new value or ID number, if parameterType = ParameterType.Integer / Boolean / LocalVariable / GlobalVariable / InventoryItem.  If parameterType = ParameterType.GameObject, it is the ConstantID number of the GameObject if it is not currently accessible</param>
		 */
		public void SetValue (int _value)
		{
			intValue = _value;
			floatValue = 0f;
			stringValue = "";
			gameObject = null;
			objectValue = null;
		}


		/**
		 * <summary>Sets the floatValue that the parameter assigns</summary>
		 * <param name = "_value">The new value, if parameterType = ParameterType.Float</param>
		 */
		public void SetValue (float _value)
		{
			floatValue = _value;
			stringValue = "";
			intValue = -1;
			gameObject = null;
			objectValue = null;
		}


		/**
		 * <summary>Sets the stringValue that the parameter assigns</summary>
		 * <param name = "_value">The new value, if parameterType = ParameterType.String</param>
		 */
		public void SetValue (string _value)
		{
			stringValue = AdvGame.ConvertTokens (_value);
			floatValue = 0f;
			intValue = -1;
			gameObject = null;
			objectValue = null;
		}


		/**
		 * <summary>Sets the gameObject that the parameter assigns</summary>
		 * <param name = "_object">The new GameObject, if parameterType = ParameterType.GameObject</param>
		 */
		public void SetValue (GameObject _object)
		{
			gameObject = _object;
			floatValue = 0f;
			stringValue = "";
			intValue = -1;
			objectValue = null;
		}


		/**
		 * <summary>Sets the objectValue that the parameter assigns</summary>
		 * <param name = "_object">The new Unity Object, if parameterType = ParameterType.UnityObject</param>
		 */
		public void SetValue (Object _object)
		{
			gameObject = null;
			floatValue = 0f;
			stringValue = "";
			intValue = -1;
			objectValue = _object;
		}


		/**
		 * <summary>Sets the gameObject that the parameter assigns</summary>
		 * <param name = "_object">The new GameObject, if parameterType = ParameterType.GameObject</param>
		 * <param name = "_value">The GameObject's ConstantID number, which is used to find the GameObject if it is not always in the same scene as the ActionParameter class</param>
		 */
		public void SetValue (GameObject _object, int _value)
		{
			gameObject = _object;
			floatValue = 0f;
			stringValue = "";
			intValue = _value;
			objectValue = null;
		}


		public string GetSaveData ()
		{
			if (parameterType == ParameterType.Float)
			{
				return floatValue.ToString ();
			}
			else if (parameterType == ParameterType.String)
			{
				return AdvGame.PrepareStringForSaving (stringValue);
			}
			else if (parameterType == ParameterType.GameObject)
			{
				if (gameObject != null)
				{
					if (gameObject.GetComponent <ConstantID>())
					{
						return gameObject.GetComponent <ConstantID>().constantID.ToString ();
					}
					ACDebug.LogWarning ("Could not save parameter data for '" + gameObject.name + "' as it has no Constant ID number.", gameObject);
				}
			}
			else if (parameterType == ParameterType.UnityObject)
			{
				if (objectValue != null)
				{
					return objectValue.name;
				}
			}
			else
			{
				return intValue.ToString ();
			}
			return "";
		}


		public void LoadData (string dataString)
		{
			if (parameterType == ParameterType.Float)
			{
				floatValue = 0f;
				float.TryParse (dataString, out floatValue);
			}
			else if (parameterType == ParameterType.String)
			{
				stringValue = AdvGame.PrepareStringForLoading (dataString);
			}
			else if (parameterType == ParameterType.GameObject)
			{
				gameObject = null;
				int constantID = 0;
				if (int.TryParse (dataString, out constantID))
				{
					ConstantID _constantID = Serializer.returnComponent <ConstantID> (constantID);
					if (_constantID != null)
					{
						gameObject = _constantID.gameObject;
					}
				}
			}
			else if (parameterType == ParameterType.UnityObject)
			{
				if (dataString == "")
				{
					objectValue = null;
				}
				else
				{
					Object[] objects = (Object[]) Resources.LoadAll ("");
					foreach (Object _object in objects)
					{
						if (_object.name == dataString)
						{
							objectValue = _object;
							return;
						}
					}
				}
			}
			else
			{
				intValue = 0;
				int.TryParse (dataString, out intValue);
			}
		}

	}

}