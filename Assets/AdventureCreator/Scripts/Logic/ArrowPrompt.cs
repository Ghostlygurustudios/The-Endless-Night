/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ArrowPrompt.cs"
 * 
 *	This script allows for "Walking Dead"-style on-screen arrows,
 *	which respond to player input.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * This component provides the ability to display up to four arrows on-screen.
	 * Each arrow responds to player input, and can run an ActionList when the relevant input is detected.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_arrow_prompt.html")]
	#endif
	public class ArrowPrompt : MonoBehaviour
	{

		/** What kind of input the arrows respond to (KeyOnly, ClickOnly, KeyAndClick) */
		public ArrowPromptType arrowPromptType = ArrowPromptType.KeyAndClick;
		/** The "up" Arrow */
		public Arrow upArrow;
		/** The "down" Arrow */
		public Arrow downArrow;
		/** The "left" Arrow */
		public Arrow leftArrow;
		/** The "right" Arrow */
		public Arrow rightArrow;
		/** If True, then Hotspots will be disabled when the arrows are on screen */
		public bool disableHotspots = true;
		
		private bool isOn = false;
		
		private AC_Direction directionToAnimate;
		private float alpha = 0f;
		private float arrowSize = 0.05f;


		/**
		 * Draws the arrow(s) on screen, if appropriate.
		 * This function is called every frame by StateHandler.
		 */
		public void DrawArrows ()
		{
			if (alpha > 0f)
			{
				if (directionToAnimate != AC_Direction.None)
				{
					SetGUIAlpha (alpha);
					
					if (directionToAnimate == AC_Direction.Up)
					{
						upArrow.rect = KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.5f, 0.1f, arrowSize*2, arrowSize));
					}
					
					else if (directionToAnimate == AC_Direction.Down)
					{
						downArrow.rect = KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.5f, 0.9f, arrowSize*2, arrowSize));
					}
					
					else if (directionToAnimate == AC_Direction.Left)
					{
						leftArrow.rect = KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.05f, 0.5f, arrowSize, arrowSize*2));
					}
					
					else if (directionToAnimate == AC_Direction.Right)
					{
						rightArrow.rect = KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.95f, 0.5f, arrowSize, arrowSize*2));
					}
				}
				
				else
				{
					SetGUIAlpha (alpha);
					
					if (upArrow.isPresent)
					{
						upArrow.rect = KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.5f, 0.1f, 0.1f, 0.05f));
					}
		
					if (downArrow.isPresent)
					{
						downArrow.rect = KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.5f, 0.9f, 0.1f, 0.05f));
					}
				
					if (leftArrow.isPresent)
					{
						leftArrow.rect = KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.05f, 0.5f, 0.05f, 0.1f));
					}
					
					if (rightArrow.isPresent)
					{
						rightArrow.rect = KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.95f, 0.5f, 0.05f, 0.1f));
					}
				}
			
				upArrow.Draw ();
				downArrow.Draw ();
				leftArrow.Draw ();
				rightArrow.Draw ();
			}
		}


		/**
		 * <summary>Enables the ArrowPrompt.</summary>
		 */
		public void TurnOn ()
		{
			if (upArrow.isPresent || downArrow.isPresent || leftArrow.isPresent || rightArrow.isPresent)
			{
				if (KickStarter.playerInput)
				{
					KickStarter.playerInput.activeArrows = this;
				}
				
				StartCoroutine ("FadeIn");
				directionToAnimate = AC_Direction.None;
				arrowSize = 0.05f;
			}
		}
		
		
		private void Disable ()
		{
			if (KickStarter.playerInput)
			{
				KickStarter.playerInput.activeArrows = null;
			}
			
			isOn = false;
		}
		

		/**
		 * <summary>Disables the ArrowPrompt.</summary>
		 */
		public void TurnOff ()
		{
			Disable ();
			StopCoroutine ("FadeIn");
			alpha = 0f;
		}
		

		/**
		 * Triggers the "up" arrow.
		 */
		public void DoUp ()
		{
			if (upArrow.isPresent && isOn && directionToAnimate == AC_Direction.None)
			{
				StartCoroutine (FadeOut (AC_Direction.Up));
				Disable ();
				upArrow.Run ();
			}
		}
		

		/**
		 * Triggers the "down" arrow.
		 */
		public void DoDown ()
		{
			if (downArrow.isPresent && isOn && directionToAnimate == AC_Direction.None)
			{
				StartCoroutine (FadeOut (AC_Direction.Down));
				Disable ();
				downArrow.Run ();
			}
		}
		

		/**
		 * Triggers the "left" arrow.
		 */
		public void DoLeft ()
		{
			if (leftArrow.isPresent && isOn && directionToAnimate == AC_Direction.None)
			{
				StartCoroutine (FadeOut (AC_Direction.Left));
				Disable ();
				leftArrow.Run ();
			}
		}
		

		/**
		 * Triggers the "right" arrow.
		 */
		public void DoRight ()
		{
			if (rightArrow.isPresent && isOn && directionToAnimate == AC_Direction.None)
			{
				StartCoroutine (FadeOut (AC_Direction.Right));
				Disable ();
				rightArrow.Run ();
			}
		}
		
		
		private IEnumerator FadeIn ()
		{
			alpha = 0f;
			
			if (alpha < 1f)
			{
				while (alpha < 0.95f)
				{
					alpha += 0.05f;
					alpha = Mathf.Clamp01 (alpha);
					yield return new WaitForFixedUpdate();
				}
				
				alpha = 1f;
				isOn = true;
			}
		}
		
		
		private IEnumerator FadeOut (AC_Direction direction)
		{
			arrowSize = 0.05f;
			alpha = 1f;
			directionToAnimate = direction;
			
			if (alpha > 0f)
			{
				while (alpha > 0.05f)
				{
					arrowSize += 0.005f;
					
					alpha -= 0.05f;
					alpha = Mathf.Clamp01 (alpha);
					yield return new WaitForFixedUpdate();
				}
				alpha = 0f;

			}
		}
		
		
		private void SetGUIAlpha (float alpha)
		{
			Color tempColor = GUI.color;
			tempColor.a = alpha;
			GUI.color = tempColor;
		}

	}


	/**
	 * A data container for an arrow that is used in an ArrowPrompt.
	 */
	[System.Serializable]
	public class Arrow
	{
			
		/** If True, the Arrow is defined and used in the ArrowPrompt */
		public bool isPresent;
		/** The Cutscene to run when the Arrow is triggered */
		public Cutscene linkedCutscene;
		/** The texture to draw on-screen */
		public Texture2D texture;
		/** The OnGUI Rect that defines the screen boundary */
		public Rect rect;
		

		/**
		 * The default Constructor.
		 */
		public Arrow ()
		{
			isPresent = false;
		}
		

		/**
		 * Runs the Arrow's linkedCutscene.
		 */
		public void Run ()
		{
			if (linkedCutscene)
			{
				linkedCutscene.SendMessage ("Interact");
			}
		}
		

		/**
		 * Draws the Arrow on screen.
		 * This is called every OnGUI call by StateHandler.
		 */
		public void Draw ()
		{
			if (texture)
			{
				GUI.DrawTexture (rect, texture, ScaleMode.StretchToFill, true);
			}
		}

	}

}