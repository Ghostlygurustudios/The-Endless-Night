/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"UISlot.cs"
 * 
 *	This is a class for Unity UI elements that contain both
 *	Image and Text components that must be linked to AC's Menu system.
 * 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A data container that links a Unity UI Button to AC's own Menu system.
	 */
	[System.Serializable]
	public class UISlot
	{

		/** The Unity UI Button this is linked to */
		public UnityEngine.UI.Button uiButton;
		/** The ConstantID number of the linked Unity UI Button */
		public int uiButtonID;
		/** The sprite to set in the Button's Image */
		public UnityEngine.Sprite sprite;
		
		private Text uiText;
		private Image uiImage;

		private Color originalColour;
		private UnityEngine.Sprite emptySprite;
		private Texture2D texture;


		/**
		 * The default Constructor.
		 */
		public UISlot ()
		{
			uiButton = null;
			uiButtonID = 0;
			uiText = null;
			uiImage = null;
			sprite = null;
		}


		#if UNITY_EDITOR

		public void LinkedUiGUI (int i, MenuSource source)
		{
			uiButton = (UnityEngine.UI.Button) EditorGUILayout.ObjectField ("Linked Button (" + (i+1).ToString () + "):", uiButton, typeof (UnityEngine.UI.Button), true);

			if (Application.isPlaying && source == MenuSource.UnityUiPrefab)
			{}
			else
			{
				uiButtonID = Menu.FieldToID <UnityEngine.UI.Button> (uiButton, uiButtonID);
				uiButton = Menu.IDToField <UnityEngine.UI.Button> (uiButton, uiButtonID, source);
			}
		}

		#endif


		/**
		 * <summary>Gets the boundary of the UI Button.</summary>
		 * <returns>The boundary Rect of the UI Button</returns>
		 */
		public RectTransform GetRectTransform ()
		{
			if (uiButton != null && uiButton.GetComponent <RectTransform>())
			{
				return uiButton.GetComponent <RectTransform>();
			}
			return null;
		}


		/**
		 * Links the UI GameObjects to the class, based on the supplied uiButtonID.
		 */
		public void LinkUIElements ()
		{
			uiButton = Serializer.returnComponent <UnityEngine.UI.Button> (uiButtonID);
			if (uiButton)
			{
				if (uiButton.GetComponentInChildren <Text>())
				{
					uiText = uiButton.GetComponentInChildren <Text>();
				}
				if (uiButton.GetComponentInChildren <Image>())
				{
					uiImage = uiButton.GetComponentInChildren <Image>();
				}

				originalColour = uiButton.colors.normalColor;
			}
		}


		/**
		 * <summary>Sets the text of the UI Button.</summary>
		 * <param title = "_text">The text to assign the Button</param>
		 */
		public void SetText (string _text)
		{
			if (uiText)
			{
				uiText.text = _text;
			}
		}


		/**
		 * <summary>Sets the image of the UI Button.</summary>
		 * <param title = "_texture">The texture to assign the Button</param>
		 */
		public void SetImage (Texture2D _texture)
		{
			if (uiImage)
			{
				if (_texture == null)
				{
					if (emptySprite == null)
					{
						emptySprite = Resources.Load <UnityEngine.Sprite> ("EmptySlot");
					}

					sprite = emptySprite;
				}
				else if (sprite == null || sprite == emptySprite || texture != _texture)
				{
					sprite = UnityEngine.Sprite.Create (_texture, new Rect (0f, 0f, _texture.width, _texture.height), new Vector2 (0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
				}

				if (_texture != null)
				{
					texture = _texture;
				}

				uiImage.sprite = sprite;
			}
		}


		/**
		 * <summary>Enables the visibility of the linked UI Button.</summary>
		 * <param name = "uiHideStyle">The method by which the UI element is hidden (DisableObject, ClearContent, DisableInteractibility) </param>
		 */
		public void ShowUIElement (UIHideStyle uiHideStyle)
		{
			if (Application.isPlaying && uiButton != null && uiButton.gameObject != null)
			{
				if (uiHideStyle == UIHideStyle.DisableObject && !uiButton.gameObject.activeSelf)
				{
					uiButton.gameObject.SetActive (true);
				}
			}
		}


		/**
		 * <summary>Disables the visibility of the linked UI Button.</summary>
		 * <param name = "uiHideStyle">The method by which the UI element is hidden (DisableObject, ClearContent, DisableInteractibility) </param>
		 */
		public void HideUIElement (UIHideStyle uiHideStyle)
		{
			if (Application.isPlaying && uiButton != null && uiButton.gameObject != null && uiButton.gameObject.activeSelf)
			{
				if (uiHideStyle == UIHideStyle.DisableObject)
				{
					uiButton.gameObject.SetActive (false);
				}
				else if (uiHideStyle == UIHideStyle.ClearContent)
				{
					SetImage (null);
					SetText ("");
				}
			}
		}


		/**
		 * <summary>Adds a UISlotClick component to the Button, which acts as a click-handler.</summary>
		 * <param title = "_menu">The Menu that the Button is linked to</param>
		 * <param title = "_element">The MenuElement within _menu that the Button is linked to</param>
		 * <param title = "_slot">The index number of the slot within _element that the Button is linked to</param>
		 */
		public void AddClickHandler (AC.Menu _menu, MenuElement _element, int _slot)
		{
			UISlotClick uiSlotClick = uiButton.gameObject.AddComponent <UISlotClick>();
			uiSlotClick.Setup (_menu, _element, _slot);
		}


		/**
		 * <summary>Changes the 'normal' colour of the linked UI Button.</summary>
		 * <param name = "newColour">The new 'normal' colour to set</param>
		 */
		public void SetColour (Color newColour)
		{
			if (uiButton != null)
			{
				ColorBlock colorBlock = uiButton.colors;
				colorBlock.normalColor = newColour;
				uiButton.colors = colorBlock;
			}
		}


		/**
		 * <summary>Reverts the 'normal' colour of the linked UI Button, if it was changed using SetColour.</summary>
		 */
		public void RestoreColour ()
		{
			if (uiButton != null)
			{
				ColorBlock colorBlock = uiButton.colors;
				colorBlock.normalColor = originalColour;
				uiButton.colors = colorBlock;
			}
		}

	}

}