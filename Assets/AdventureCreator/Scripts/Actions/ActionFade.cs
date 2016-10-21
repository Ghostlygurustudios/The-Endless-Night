/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionFade.cs"
 * 
 *	This action controls the MainCamera's fading.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionFade : Action
	{
		
		public FadeType fadeType;
		public bool isInstant;
		public float fadeSpeed = 0.5f;
		public bool setTexture;
		public Texture2D tempTexture;
		public int tempTextureParameterID = -1;
		public bool forceCompleteTransition = true;
		
		
		public ActionFade ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Camera;
			title = "Fade";
			description = "Fades the camera in or out. The fade speed can be adjusted, as can the overlay texture – this is black by default.";
		}


		override public void AssignValues (List<ActionParameter> parameters)
		{
			tempTexture = (Texture2D) AssignObject <Texture2D> (parameters, tempTextureParameterID, tempTexture);
		}
		
		
		override public float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;
				
				MainCamera mainCam = KickStarter.mainCamera;
				RunSelf (mainCam, fadeSpeed);
					
				if (willWait && !isInstant)
				{
					return (fadeSpeed);
				}

				return 0f;
			}

			else
			{
				isRunning = false;
				return 0f;
			}
		}


		override public void Skip ()
		{
			RunSelf (KickStarter.mainCamera, 0f);
		}


		private void RunSelf (MainCamera mainCam, float _time)
		{
			if (mainCam == null)
			{
				return;
			}

			mainCam.StopCrossfade ();

			if (fadeType == FadeType.fadeIn)
			{
				if (isInstant)
				{
					mainCam.FadeIn (0f);
				}
				else
				{
					mainCam.FadeIn (_time, forceCompleteTransition);
				}
			}
			else
			{
				Texture2D texToUse = tempTexture;
				if (!setTexture)
				{
					texToUse = null;
				}

				float timeToFade = _time;
				if (isInstant)
				{
					timeToFade = 0f;
				}

				mainCam.FadeOut (timeToFade, texToUse, forceCompleteTransition);
			}
		}

		
		#if UNITY_EDITOR

		override public void ShowGUI (List<ActionParameter> parameters)
		{
			fadeType = (FadeType) EditorGUILayout.EnumPopup ("Type:", fadeType);

			if (fadeType == FadeType.fadeOut)
			{
				setTexture = EditorGUILayout.Toggle ("Custom fade texture?", setTexture);
				if (setTexture)
				{
					tempTextureParameterID = Action.ChooseParameterGUI ("Fade texture:", parameters, tempTextureParameterID, ParameterType.UnityObject);
					if (tempTextureParameterID < 0)
					{
						tempTexture = (Texture2D) EditorGUILayout.ObjectField ("Fade texture:", tempTexture, typeof (Texture2D), false);
					}
				}
			}

			isInstant = EditorGUILayout.Toggle ("Instant?", isInstant);
			if (!isInstant)
			{
				fadeSpeed = EditorGUILayout.Slider ("Time to fade:", fadeSpeed, 0, 3);
				forceCompleteTransition = EditorGUILayout.Toggle ("Force complete transition?", forceCompleteTransition);
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			}

			AfterRunningOption ();
		}
		
		
		override public string SetLabel ()
		{
			string labelAdd = "";
			
			if (fadeType == FadeType.fadeIn)
			{
				labelAdd = " (In)";
			}
			else
			{
				labelAdd = " (Out)";
			}
			
			return labelAdd;
		}

		#endif

	}

}