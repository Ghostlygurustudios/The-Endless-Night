using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * An interface that is called when saving and loading save files, allowing custom global save data to be injected.
	 */
	public interface ISave
	{

		void PreSave ();
		void PostLoad ();

	}


	/**
	 * An interface that is called when saving and loading options data, allowing custom PlayerPrefs to be injected.
	 */
	public interface ISaveOptions
	{
		
		void PreSaveOptions ();
		void PostLoadOptions ();
		
	}

}