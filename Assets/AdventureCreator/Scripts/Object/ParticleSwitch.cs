/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ParticleSwitch.cs"
 * 
 *	This can be used, via the Object: Send Message Action,
 *	to turn its attached particle systems on and off.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * This script provides functions to enable and disable the ParticleSystem component on the GameObject it is attached to.
	 * These functions can be called either through script, or with the "Object: Send message" Action.
	 */
	[AddComponentMenu("Adventure Creator/Misc/Particle switch")]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_particle_switch.html")]
	#endif
	public class ParticleSwitch : MonoBehaviour
	{

		/** If True, then the Light component will be enabled when the game begins. */
		public bool enableOnStart = false;
		
		
		private void Awake ()
		{
			Switch (enableOnStart);
		}
		

		/**
		 * Enables the ParticleSystem component on the GameObject this script is attached to.
		 */
		public void TurnOn ()
		{
			Switch (true);
		}
		

		/**
		 * Disables the ParticleSystem component on the GameObject this script is attached to.
		 */
		public void TurnOff ()
		{
			Switch (false);
		}


		/**
		 * Causes the ParticleSystem component on the GameObject to emit its maximum number of particles in one go.
		 */
		public void Interact ()
		{
			if (GetComponent <ParticleSystem>())
			{
				GetComponent <ParticleSystem>().Emit (GetComponent <ParticleSystem>().maxParticles);
			}
		}
		
		
		private void Switch (bool turnOn)
		{
			if (GetComponent <ParticleSystem>())
			{
				if (turnOn)
				{
					GetComponent <ParticleSystem>().Play ();
				}
				else
				{
					GetComponent <ParticleSystem>().Stop ();
				}
			}
		}
		
	}

}