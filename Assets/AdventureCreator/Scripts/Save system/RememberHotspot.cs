/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"RememberHotspot.cs"
 * 
 *	This script is attached to hotspot objects in the scene
 *	whose on/off state we wish to save. 
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * Attach this script to Hotspot objects in the scene whose state you wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Hotspot")]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_hotspot.html")]
	#endif
	public class RememberHotspot : Remember
	{

		/** Determines whether the Hotspot is on or off when the game begins */
		public AC_OnOff startState = AC_OnOff.On;

		private bool loadedData = false;


		private void Awake ()
		{
			if (loadedData) return;

			if (KickStarter.settingsManager && GameIsPlaying () && GetComponent <Hotspot>())
			{
				if (startState == AC_OnOff.On)
				{
					GetComponent <Hotspot>().TurnOn ();
				}
				else
				{
					GetComponent <Hotspot>().TurnOff ();
				}
			}
		}


		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			HotspotData hotspotData = new HotspotData ();
			hotspotData.objectID = constantID;


			if (GetComponent <Hotspot>())
			{
				Hotspot _hotspot = GetComponent <Hotspot>();
				hotspotData.isOn = _hotspot.IsOn ();
				hotspotData.buttonStates = ButtonStatesToString (_hotspot);

				hotspotData.hotspotName = _hotspot.GetName (0);
				hotspotData.displayLineID = _hotspot.displayLineID;
			}
			
			return Serializer.SaveScriptData <HotspotData> (hotspotData);
		}


		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			HotspotData data = Serializer.LoadScriptData <HotspotData> (stringData);
			if (data == null)
			{
				loadedData = false;
				return;
			}

			if (data.isOn)
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
			}
			else
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);
			}

			if (GetComponent <Hotspot>())
			{
				Hotspot _hotspot = GetComponent <Hotspot>();

				if (data.isOn)
				{
					_hotspot.TurnOn ();
				}
				else
				{
					_hotspot.TurnOff ();
				}

				StringToButtonStates (_hotspot, data.buttonStates);

				if (data.hotspotName != "")
				{
					_hotspot.SetName (data.hotspotName, data.displayLineID);
				}
				_hotspot.ResetMainIcon ();
			}

			loadedData = true;
		}


		private void StringToButtonStates (Hotspot hotspot, string stateString)
		{
			if (stateString.Length == 0)
			{
				return;
			}

			string[] typesArray = stateString.Split (SaveSystem.pipe[0]);
			
			if (KickStarter.settingsManager == null || KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
			{
				// Look interactions
				if (hotspot.provideLookInteraction && hotspot.lookButton != null)
				{
					hotspot.lookButton.isDisabled = SetButtonDisabledValue (typesArray [0]);
				}
			}

			if (hotspot.provideUseInteraction)
			{
				string[] usesArray = typesArray[1].Split (","[0]);
				
				for (int i=0; i<usesArray.Length; i++)
				{
					if (hotspot.useButtons.Count < i+1)
					{
						break;
					}
					hotspot.useButtons[i].isDisabled = SetButtonDisabledValue (usesArray [i]);
				}
			}

			// Inventory interactions
			if (hotspot.provideInvInteraction && typesArray.Length > 2)
			{
				string[] invArray = typesArray[2].Split (","[0]);
				
				for (int i=0; i<invArray.Length; i++)
				{
					if (hotspot.invButtons.Count < i+1)
					{
						break;
					}
					
					hotspot.invButtons[i].isDisabled = SetButtonDisabledValue (invArray [i]);
				}
			}
		}
		
		
		private string ButtonStatesToString (Hotspot hotspot)
		{
			System.Text.StringBuilder stateString = new System.Text.StringBuilder ();
			
			if (KickStarter.settingsManager == null || KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
			{
				// Single-use and Look interaction
				if (hotspot.provideLookInteraction)
				{
					stateString.Append (GetButtonDisabledValue (hotspot.lookButton));
				}
				else
				{
					stateString.Append ("0");
				}
			}

			stateString.Append (SaveSystem.pipe);

			// Multi-use interactions
			if (hotspot.provideUseInteraction)
			{
				foreach (AC.Button button in hotspot.useButtons)
				{
					stateString.Append (GetButtonDisabledValue (button));
					
					if (hotspot.useButtons.IndexOf (button) < hotspot.useButtons.Count-1)
					{
						stateString.Append (",");
					}
				}
			}
				
			stateString.Append (SaveSystem.pipe);

			// Inventory interactions
			if (hotspot.provideInvInteraction)
			{
				foreach (AC.Button button in hotspot.invButtons)
				{
					stateString.Append (GetButtonDisabledValue (button));
					
					if (hotspot.invButtons.IndexOf (button) < hotspot.invButtons.Count-1)
					{
						stateString.Append (",");
					}
				}
			}
			
			return stateString.ToString ();
		}


		private string GetButtonDisabledValue (AC.Button button)
		{
			if (button != null && !button.isDisabled)
			{
				return ("1");
			}
			
			return ("0");
		}
		
		
		private bool SetButtonDisabledValue (string text)
		{
			if (text == "1")
			{
				return false;
			}
			
			return true;
		}

	}


	/**
	 * A data container used by the RememberHotspot script.
	 */
	[System.Serializable]
	public class HotspotData : RememberData
	{

		/** True if the Hotspot is enabled */
		public bool isOn;
		/** The enabled state of each Interaction */
		public string buttonStates;
		/** The ID number that references the Hotspot's name, as generated by the Speech Manager */
		public int displayLineID;
		/** The Hotspot's display name */
		public string hotspotName;

		/**
		 * The default Constructor.
		 */
		public HotspotData () { }
	}

}