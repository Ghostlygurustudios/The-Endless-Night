/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"MenuGraphic.cs"
 * 
 *	This MenuElement provides a space for
 *	animated and static textures
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that provides a space for animated or still images.
	 */
	public class MenuGraphic : MenuElement
	{

		/** The Unity UI Image this is linked to (Unity UI Menus only) */
		public Image uiImage;
		/** The type of graphic that is shown (Normal, DialogPortrait) */
		public AC_GraphicType graphicType = AC_GraphicType.Normal;
		/** The CursorIconBase that stores the graphic and animation data */
		public CursorIconBase graphic;
		
		private Sprite sprite;
		private Speech speech;
		private bool speechIsAnimating;
		private Texture2D speechTex;
		private Rect speechRect;
		private bool isDuppingSpeech;
		

		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiImage = null;
			
			graphicType = AC_GraphicType.Normal;
			isVisible = true;
			isClickable = false;
			graphic = new CursorIconBase ();
			numSlots = 1;
			SetSize (new Vector2 (10f, 5f));
			
			base.Declare ();
		}
		

		/**
		 * <summary>Creates and returns a new MenuGraphic that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <returns>A new MenuGraphic with the same values as itself</returns>
		 */
		public override MenuElement DuplicateSelf (bool fromEditor)
		{
			MenuGraphic newElement = CreateInstance <MenuGraphic>();
			newElement.Declare ();
			newElement.CopyGraphic (this);
			return newElement;
		}
		
		
		private void CopyGraphic (MenuGraphic _element)
		{
			uiImage = _element.uiImage;
			
			graphicType = _element.graphicType;
			graphic = new CursorIconBase ();
			graphic.Copy (_element.graphic);
			base.Copy (_element);
		}
		

		/**
		 * <summary>Initialises the linked Unity UI GameObjects.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu)
		{
			uiImage = LinkUIElement <Image>();
		}
		

		/**
		 * <summary>Gets the boundary of a slot</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <returns>The boundary Rect of the slot</returns>
		 */
		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiImage)
			{
				return uiImage.rectTransform;
			}
			return null;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");
			
			if (source != MenuSource.AdventureCreator)
			{
				uiImage = LinkedUiGUI <Image> (uiImage, "Linked Image:", source);
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
			}
			
			graphicType = (AC_GraphicType) EditorGUILayout.EnumPopup ("Graphic type:", graphicType);
			if (graphicType == AC_GraphicType.Normal)
			{
				graphic.ShowGUI (false);
			}
			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}
		
		#endif


		private void UpdateSpeechLink ()
		{
			if (!isDuppingSpeech && KickStarter.dialog.GetLatestSpeech () != null)
			{
				speech = KickStarter.dialog.GetLatestSpeech ();
			}
		}
		

		/**
		 * <summary>Assigns the element to a specific Speech line.</summary>
		 * <param name = "_speech">The Speech line to assign the element to</param>
		 */
		public override void SetSpeech (Speech _speech)
		{
			isDuppingSpeech = true;
			speech = _speech;
		}
		

		/**
		 * Clears any speech text on display.
		 */
		public override void ClearSpeech ()
		{
			if (graphicType == AC_GraphicType.DialoguePortrait)
			{
				speechTex = null;
			}
		}


		/**
		 * <summary>Performs all calculations necessary to display the element.</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language to display text in</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (graphicType == AC_GraphicType.DialoguePortrait)
			{
				UpdateSpeechLink ();
				if (speech != null)
				{
					speechTex = speech.GetPortrait ();
					speechIsAnimating = speech.IsAnimating ();
				}
			}
			
			SetUIGraphic ();
		}


		/**
		 * <summary>Draws the element using OnGUI</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			if (graphicType == AC_GraphicType.Normal)
			{
				if (graphic != null)
				{
					graphic.DrawAsInteraction (ZoomRect (relativeRect, zoom), true);
				}
			}
			else
			{
				if (speechTex != null)
				{
					if (speechIsAnimating)
					{
						if (speech != null)
						{
							speechRect = speech.GetAnimatedRect ();
						}
						GUI.DrawTextureWithTexCoords (ZoomRect (relativeRect, zoom), speechTex, speechRect);
					}
					else
					{
						GUI.DrawTexture (ZoomRect (relativeRect, zoom), speechTex, ScaleMode.StretchToFill, true, 0f);
					}
				}
			}
		}
		

		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void RecalculateSize (MenuSource source)
		{
			graphic.Reset ();
			SetUIGraphic ();
			base.RecalculateSize (source);
		}


		private void SetUIGraphic ()
		{
			if (uiImage != null)
			{
				if (graphicType == AC_GraphicType.Normal)
				{
					uiImage.sprite = graphic.GetAnimatedSprite (true);
				}
				else if (speech != null)
				{
					uiImage.sprite = speech.GetPortraitSprite ();
				}
				UpdateUIElement (uiImage);
			}
		}
		
		
		protected override void AutoSize ()
		{
			if (graphicType == AC_GraphicType.Normal && graphic.texture != null)
			{
				GUIContent content = new GUIContent (graphic.texture);
				AutoSize (content);
			}
		}
		
	}
	
}