/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"Container.cs"
 * 
 *	This script is used to store a set of
 *	Inventory items in the scene, to be
 *	either taken or added to by the player.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component that is used to store a local set of inventory items within a scene.
	 * The items stored here are separate to those held by the player, who can retrieve or place items in here for safe-keeping.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_container.html")]
	#endif
	public class Container : MonoBehaviour
	{

		/** The list of inventory items held by the Container */
		public List<ContainerItem> items = new List<ContainerItem>();


		/**
		 * Activates the Container.  If a Menu with an appearType = AppearType.OnContainer, it will be enabled and show the Container's contents.
		 */
		public void Interact ()
		{
			KickStarter.playerInput.activeContainer = this;
		}


		/**
		 * <summary>Adds an inventory item to the Container's contents.</summary>
		 * <param name = "_id">The ID number of the InvItem to add</param>
		 * <param name = "amount">How many instances of the inventory item to add</param>
		 */
		public void Add (int _id, int amount)
		{
			// Raise "count" by 1 for appropriate ID
			foreach (ContainerItem containerItem in items)
			{
				if (containerItem.linkedID == _id)
				{
					if (KickStarter.inventoryManager.CanCarryMultiple (containerItem.linkedID))
					{
						containerItem.count += amount;
					}
					return;
				}
			}

			// Not already carrying the item
			foreach (InvItem assetItem in KickStarter.inventoryManager.items)
			{
				if (assetItem.id == _id)
				{
					if (!KickStarter.inventoryManager.CanCarryMultiple (_id))
					{
						amount = 1;
					}

					items.Add (new ContainerItem (_id, amount, GetIDArray ()));
				}
			}
		}
		

		/**
		 * <summary>Removes an inventory item from the Container's contents.</summary>
		 * <param name = "_id">The ID number of the InvItem to remove</param>
		 * <param name = "amount">How many instances of the inventory item to remove</param>
		 */
		public void Remove (int _id, int amount)
		{
			// Reduce "count" by 1 for appropriate ID
			
			foreach (ContainerItem item in items)
			{
				if (item.linkedID == _id)
				{
					if (item.count > 0)
					{
						item.count -= amount;
					}
					if (item.count < 1)
					{
						items.Remove (item);
					}
					return;
				}
			}
		}


		/**
		 * <summary>Gets the number of instances of a particular inventory item stored within the Container.</summary>
		 * <param name = "_id">The ID number of the InvItem to search for</param>
		 * <returns>The number of instances of the inventory item stored within the Container</returns>
		 */
		public int GetCount (int _id)
		{
			foreach (ContainerItem item in items)
			{
				if (item.linkedID == _id)
				{
					return (item.count);
				}
			}
			
			return 0;
		}


		/**
		 * <summary>Adds an inventory item to the Container's contents, at a particular index.</summary>
		 * <param name = "_item">The InvItem to place within the Container</param>
		 * <param name = "_index">The index number within the Container's current contents to insert the new item</param>
		 */
		public void InsertAt (InvItem _item, int _index)
		{
			ContainerItem newContainerItem = new ContainerItem (_item.id, GetIDArray ());
			newContainerItem.count = _item.count;

			if (items.Count <= _index)
			{
				items.Add (newContainerItem);
			}
			else
			{
				items.Insert (_index, newContainerItem);
			}
		}


		/**
		 * <summmary>Gets an array of ID numbers of existing ContainerItem classes, so that a unique number can be generated.</summary>
		 * <returns>Gets an array of ID numbers of existing ContainerItem classes</returns>
		 */
		public int[] GetIDArray ()
		{
			List<int> idArray = new List<int>();
			
			foreach (ContainerItem item in items)
			{
				idArray.Add (item.id);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}

	}

}