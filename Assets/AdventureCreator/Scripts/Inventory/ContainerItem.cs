﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ContainerItem.cs"
 * 
 *	This script is a container class for inventory items stored in a Container.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * A data container for an inventory item stored within a Container.
	 */
	[System.Serializable]
	public class ContainerItem
	{

		/** The ID number of the associated inventory item (InvItem) being stored */
		public int linkedID;
		/** How many instances of the item are being stored, if the InvItem's canCarryMultiple = True */
		public int count;
		/** A unique identifier */
		public int id;


		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_linkedID">The ID number of the associated inventory item (InvItem) being stored</param>
		 * <param name = "idArray">An array of already-used ID numbers, so that a unique one can be generated</param>
		 */
		public ContainerItem (int _linkedID, int[] idArray)
		{
			count = 1;
			linkedID = _linkedID;
			id = 0;
			
			// Update id based on array
			foreach (int _id in idArray)
			{
				if (id == _id)
					id ++;
			}
		}


		/**
		 * <summary>A Constructor.</summary>
		 * <param name = "_linkedID">The ID number of the associated inventory item (InvItem) being stored</param>
		 * <param name = "_count">How many instances of the item are being stored, if the InvItem's canCarryMultiple = True</param>
		 * <param name = "idArray">An array of already-used ID numbers, so that a unique one can be generated</param>
		 */
		public ContainerItem (int _linkedID, int _count, int[] idArray)
		{
			count = _count;
			linkedID = _linkedID;
			id = 0;
			
			// Update id based on array
			foreach (int _id in idArray)
			{
				if (id == _id)
					id ++;
			}
		}


		/**
		 * <summary>A Constructor.</summary>
		 * <param name = "_linkedID">The ID number of the associated inventory item (InvItem) being stored</param>
		 * <param name = "_count">How many instances of the item are being stored, if the InvItem's canCarryMultiple = True</param>
		 * <param name = "_id">A unique identifier</param>
		 */
		public ContainerItem (int _linkedID, int _count, int _id)
		{
			linkedID = _linkedID;
			count = _count;
			id = _id;
		}

	}

}