/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"RememberConversation.cs"
 * 
 *	This script is attached to conversation objects in the scene
 *	with DialogOption states we wish to save.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Attach this script to Conversation objects in the scene with DialogOption states you wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Conversation")]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_conversation.html")]
	#endif
	public class RememberConversation : Remember
	{

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			ConversationData conversationData = new ConversationData();
			conversationData.objectID = constantID;

			if (GetComponent <Conversation>())
			{
				Conversation conversation = GetComponent <Conversation>();

				bool[] optionStates = conversation.GetOptionStates ();
				conversationData._optionStates = ArrayToString <bool> (optionStates);

				bool[] optionLocks = conversation.GetOptionLocks ();
				conversationData._optionLocks = ArrayToString <bool> (optionLocks);

				bool[] optionChosens = conversation.GetOptionChosens ();
				conversationData._optionChosens = ArrayToString <bool> (optionChosens);

				conversationData.lastOption = conversation.lastOption;
			}

			return Serializer.SaveScriptData <ConversationData> (conversationData);
		}


		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			ConversationData data = Serializer.LoadScriptData <ConversationData> (stringData);
			if (data == null) return;

			if (GetComponent <Conversation>())
			{
				Conversation conversation = GetComponent <Conversation>();

				bool[] optionStates = StringToBoolArray (data._optionStates);
				conversation.SetOptionStates (optionStates);

				bool[] optionLocks = StringToBoolArray (data._optionLocks);
				conversation.SetOptionLocks (optionLocks);

				bool[] optionChosens = StringToBoolArray (data._optionChosens);
				conversation.SetOptionChosens (optionChosens);

				conversation.lastOption = data.lastOption;
			}
		}

	}


	/**
	 * A data container used by the RememberConversation script.
	 */
	[System.Serializable]
	public class ConversationData : RememberData
	{

		/** The enabled state of each DialogOption */
		public string _optionStates;
		/** The locked state of each DialogOption */
		public string _optionLocks;
		/** The 'already chosen' state of each DialogOption */
		public string _optionChosens;
		/** The index of the last-chosen option */
		public int lastOption;

		/**
		 * The default Constructor.
		 */
		public ConversationData () { }
	}

}