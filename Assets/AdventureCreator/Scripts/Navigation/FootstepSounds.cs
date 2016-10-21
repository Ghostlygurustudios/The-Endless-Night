/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"FootstepSounds.cs"
 * 
 *	A component that can play footstep sounds whenever a Mecanim-animated Character moves.
 * The component stores an array of AudioClips, one of which is played at random whenever the PlayFootstep method is called.
 * This method should be invoked as part of a Unity AnimationEvent: http://docs.unity3d.com/Manual/animeditor-AnimationEvents.html
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * A component that can play footstep sounds whenever a Mecanim-animated Character moves.
	 * The component stores an array of AudioClips, one of which is played at random whenever the PlayFootstep method is called.
	 * This method should be invoked as part of a Unity AnimationEvent: http://docs.unity3d.com/Manual/animeditor-AnimationEvents.html
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_footstep_sounds.html")]
	#endif
	[AddComponentMenu("Adventure Creator/Characters/Footstep sounds")]
	public class FootstepSounds: MonoBehaviour
	{

		/** An array of footstep AudioClips to play at random */
		public AudioClip[] footstepSounds;
		/** The Sound object to play from */
		public Sound soundToPlayFrom;
		/** The Player or NPC that this component is for */
		public Char character;
		
		private int lastIndex;
		private AudioSource audioSource;
		
		
		private void Awake ()
		{
			if (soundToPlayFrom != null)
			{
				audioSource = soundToPlayFrom.GetComponent <AudioSource>();
			}
			character = GetComponent <Char>();
		}
		

		/**
		 * Plays one of the footstepSounds at random on the assigned Sound object.
		 */
		public void PlayFootstep ()
		{
			if (audioSource != null && footstepSounds.Length > 0 &&
			    (character == null || character.charState == CharState.Move))
			{
				int newIndex = Random.Range (0, footstepSounds.Length - 1);
				if (newIndex == lastIndex)
				{
					newIndex ++;
				}

				if (footstepSounds[newIndex] != null)
				{
					audioSource.clip = footstepSounds [newIndex];
					soundToPlayFrom.Play (false);
				}

				lastIndex = newIndex;
			}
		}

	}

}