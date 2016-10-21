﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"FollowTintMap.cs"
 * 
 *	This script causes any attached Sprite Renderer
 *	to change colour according to a TintMap.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Attach this script to a GameObject to affect the colour values of its SpriteRenderer component, according to a TintMap.
	 * This is intended for 2D character sprites, to provide lighting effects when moving around a scene.
	 */
	[AddComponentMenu("Adventure Creator/Characters/Follow TintMap")]
	[RequireComponent (typeof (SpriteRenderer))]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_follow_tint_map.html")]
	#endif
	public class FollowTintMap : MonoBehaviour
	{

		/** If True, then the tintMap defined in SceneSettings will be used as this sprite's colour tinter. */
		public bool useDefaultTintMap = true;
		/** The TintMap to make use of, if useDefaultTintMap = False */
		public TintMap tintMap;
		/** How intense the colour-tiniting effect is. 0 = no effect, 1 = fully tinted */
		public float intensity = 1f;
		/** If True, then SpriteRenderer components found elsewhere in the object's hierarchy will also be affected */
		public bool affectChildren = false;

		private TintMap actualTintMap;
		private SpriteRenderer _spriteRenderer;
		private SpriteRenderer[] _spriteRenderers;

		private float targetIntensity;
		private float initialIntensity;
		private float fadeStartTime;
		private float fadeTime;

		private float spriteAlpha = 1f;
		private float[] spriteAlphas;


		private void Awake ()
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			_spriteRenderer = GetComponent<SpriteRenderer>();
			_spriteRenderers = GetComponentsInChildren <SpriteRenderer>();

			if (_spriteRenderer != null)
			{
				spriteAlpha = _spriteRenderer.color.a;
			}
			if (_spriteRenderers != null)
			{
				List<float> spriteAlphasList = new List<float>();
				foreach (SpriteRenderer spriteRenderer in _spriteRenderers)
				{
					spriteAlphasList.Add (spriteRenderer.color.a);
				}
				spriteAlphas = spriteAlphasList.ToArray ();
			}

			targetIntensity = initialIntensity = intensity;

			ResetTintMap ();
		}


		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			ResetTintMap ();
		}


		/**
		 * Assigns the internal TintMap to follow based on the chosen public variables.
		 */
		public void ResetTintMap ()
		{
			actualTintMap = tintMap;

			if (useDefaultTintMap && KickStarter.sceneSettings)
			{
				if (KickStarter.sceneSettings.tintMap)
				{
					actualTintMap = KickStarter.sceneSettings.tintMap;
				}
				else
				{
					ACDebug.Log (this.gameObject.name + " cannot find Tint Map to follow!");
				}
			}

			if (affectChildren)
			{
				for (int i=0; i<_spriteRenderers.Length; i++)
				{
					if (spriteAlphas.Length > i)
					{
						_spriteRenderers[i].color = new Color (1f, 1f, 1f, spriteAlphas[i]);
					}
				}
			}
			else
			{
				_spriteRenderer.color = new Color (1f, 1f, 1f, spriteAlpha);
			}
		}


		/**
		 * <summary>Changes the intensity of a TintMap's effect.</summary>
		 * <param name = "_targetIntensity">The new intensity value</param>
		 * <param name = "_fadeTime">The duration, in seconds, to change the intensity over. If = 0, the change will be instantaneous.</param>
		 */
		public void SetIntensity (float _targetIntensity, float _fadeTime = 0f)
		{
			targetIntensity = _targetIntensity;
			initialIntensity = intensity;

			if (_fadeTime <= 0)
			{
				intensity = _targetIntensity;
				fadeStartTime = 0f;
				fadeTime = _fadeTime;
			}
			else
			{
				fadeStartTime = Time.time;
				fadeTime = _fadeTime;
			}
		}


		private void LateUpdate ()
		{
			if (actualTintMap)
			{
				if (fadeTime > 0f)
				{
					intensity = Mathf.Lerp (initialIntensity, targetIntensity, AdvGame.Interpolate (fadeStartTime, fadeTime, MoveMethod.Linear, null));
					if (Time.time > (fadeStartTime + fadeTime))
					{
						intensity = targetIntensity;
						fadeTime = fadeStartTime = 0f;
					}
				}

				if (affectChildren)
				{
					for (int i=0; i<_spriteRenderers.Length; i++)
					{
						if (spriteAlphas.Length > i)
						{
							_spriteRenderers[i].color = actualTintMap.GetColorData (transform.position, intensity, spriteAlphas[i]);
						}
						else
						{
							_spriteRenderers[i].color = actualTintMap.GetColorData (transform.position, intensity);
						}
					}
				}
				else
				{
					_spriteRenderer.color = actualTintMap.GetColorData (transform.position, intensity, spriteAlpha);
				}
			}
		}


		/**
		 * <summary>Updates a VisibilityData class with its own variables that need saving.</summary>
		 * <param name = "visibilityData">The original VisibilityData class</param>
		 * <returns>The updated VisibilityData class</returns>
		 */
		public VisibilityData SaveData (VisibilityData visibilityData)
		{
			visibilityData.useDefaultTintMap = useDefaultTintMap;
			visibilityData.tintIntensity = targetIntensity;

			visibilityData.tintMapID = 0;
			if (!useDefaultTintMap && tintMap != null && tintMap.gameObject != null)
			{
				visibilityData.tintMapID = Serializer.GetConstantID (tintMap.gameObject);
			}

			return visibilityData;
		}


		/**
		 * <summary>Updates its own variables from a VisibilityData class.</summary>
		 * <param name = "data">The VisibilityData class to load from</param>
		 */
		public void LoadData (VisibilityData data)
		{
			useDefaultTintMap = data.useDefaultTintMap;
			SetIntensity (data.tintIntensity, 0f);

			if (!useDefaultTintMap && data.tintMapID != 0)
			{
				tintMap = Serializer.returnComponent <TintMap> (data.tintMapID);
			}

			ResetTintMap ();
		}

	}

}