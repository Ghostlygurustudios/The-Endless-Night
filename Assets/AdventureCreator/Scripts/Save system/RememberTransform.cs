/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"RememberTransform.cs"
 * 
 *	This script is attached to gameObjects in the scene
 *	with transform data we wish to save.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * Attach this to GameObject whose position, parentage, or scene presence you wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Transform")]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_transform.html")]
	#endif
	public class RememberTransform : ConstantID
	{

		/** True if the GameObject's change in parent should be recorded */
		public bool saveParent;
		/** True if the GameObject's change in scene presence should be recorded */
		public bool saveScenePresence;


		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public TransformData SaveTransformData ()
		{
			TransformData transformData = new TransformData();
			
			transformData.objectID = constantID;
			
			transformData.LocX = transform.position.x;
			transformData.LocY = transform.position.y;
			transformData.LocZ = transform.position.z;
			
			transformData.RotX = transform.eulerAngles.x;
			transformData.RotY = transform.eulerAngles.y;
			transformData.RotZ = transform.eulerAngles.z;
			
			transformData.ScaleX = transform.localScale.x;
			transformData.ScaleY = transform.localScale.y;
			transformData.ScaleZ = transform.localScale.z;

			transformData.bringBack = saveScenePresence;

			if (saveParent)
			{
				// Attempt to find the "hand" bone of a character
				Transform t = transform.parent;

				if (t == null)
				{
					transformData.parentID = 0;
					return transformData;
				}

				while (t.parent != null)
				{
					t = t.parent;

					if (t.GetComponent <AC.Char>())
					{
						AC.Char parentCharacter = t.GetComponent <AC.Char>();
						
						if (parentCharacter is Player || (parentCharacter.GetComponent <ConstantID>() && parentCharacter.GetComponent <ConstantID>().constantID != 0))
						{
							if (transform.parent == parentCharacter.leftHandBone || transform.parent == parentCharacter.rightHandBone)
							{
								if (parentCharacter is Player)
								{
									transformData.parentIsPlayer = true;
									transformData.parentIsNPC = false;
									transformData.parentID = 0;
								}
								else
								{
									transformData.parentIsPlayer = false;
									transformData.parentIsNPC = true;
									transformData.parentID = parentCharacter.GetComponent <ConstantID>().constantID;
								}
								
								if (transform.parent == parentCharacter.leftHandBone)
								{
									transformData.heldHand = Hand.Left;
								}
								else
								{
									transformData.heldHand = Hand.Right;
								}
								
								return transformData;
							}
						}
						
						break;
					}
				}

				if (transform.parent.GetComponent <ConstantID>() && transform.parent.GetComponent <ConstantID>().constantID != 0)
				{
					transformData.parentID = transform.parent.GetComponent <ConstantID>().constantID;
				}
				else
				{
					transformData.parentID = 0;
					ACDebug.LogWarning ("Could not save " + this.name + "'s parent since it has no Constant ID");
				}
			}

			return transformData;
		}


		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public void LoadTransformData (TransformData data)
		{
			if (data == null) return;

			if (data.parentIsPlayer)
			{
				if (KickStarter.player)
				{
					if (data.heldHand == Hand.Left)
					{
						transform.parent = KickStarter.player.leftHandBone;
					}
					else
					{
						transform.parent = KickStarter.player.rightHandBone;
					}
				}
			}
			else if (data.parentID != 0)
			{
				ConstantID parentObject = Serializer.returnComponent <ConstantID> (data.parentID);

				if (parentObject != null)
				{
					if (data.parentIsNPC && parentObject.GetComponent <NPC>())
					{
						if (data.heldHand == Hand.Left)
						{
							transform.parent = parentObject.GetComponent <NPC>().leftHandBone;
						}
						else
						{
							transform.parent = parentObject.GetComponent <NPC>().rightHandBone;
						}
					}
					else
					{
						transform.parent = parentObject.gameObject.transform;
					}
				}
			}
			else if (data.parentID == 0 && saveParent)
			{
				transform.parent = null;
			}

			transform.position = new Vector3 (data.LocX, data.LocY, data.LocZ);
			transform.eulerAngles = new Vector3 (data.RotX, data.RotY, data.RotZ);
			transform.localScale = new Vector3 (data.ScaleX, data.ScaleY, data.ScaleZ);
		}

	}


	/**
	 * A data container used by the RememberTransform script.
	 */
	[System.Serializable]
	public class TransformData
	{

		/** The ConstantID number of the object being saved */
		public int objectID;

		/** The X position */
		public float LocX;
		/** The Y position */
		public float LocY;
		/** The Z position */
		public float LocZ;

		/** The X rotation */
		public float RotX;
		/** The Y rotation */
		public float RotY;
		/** The Z rotation */
		public float RotZ;

		/** The X scale */
		public float ScaleX;
		/** The Y scale */
		public float ScaleY;
		/** The Z scale */
		public float ScaleZ;

		/** True if the GameObject should be re-instantiated if removed from the scene */
		public bool bringBack;

		/** The Constant ID number of the GameObject's parent */
		public int parentID;
		/** True if the GameObject's parent is an NPC */
		public bool parentIsNPC = false;
		/** True if the GameObject's parent is the Player */
		public bool parentIsPlayer = false;
		/** If the GameObject's parent is a Character, which hand is it held in? (Left, Right) */
		public Hand heldHand;


		/**
		 * The default Constructor.
		 */
		public TransformData () { }
		
	}

}