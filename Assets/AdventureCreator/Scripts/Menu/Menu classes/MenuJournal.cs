/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"MenuJournal.cs"
 * 
 *	This MenuElement provides an array of labels, used to make a book.
 * 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that provides an array of labels, each one representing a page, that collectively form a bork.
	 * "Pages" can be added to the journal mid-game, and changes made to it will be saved in save games.
	 */
	public class MenuJournal : MenuElement
	{

		/** The Unity UI Text this is linked to (Unity UI Menus only) */
		public Text uiText;
		/** A List of JournalPage instances that make up the pages within */
		public List<JournalPage> pages = new List<JournalPage>();
		/** The initial number of pages when the game begins */
		public int numPages = 1;
		/** The index number of the current page being shown */
		public int showPage = 1;
		/** If True, then the "Preview page" set in the Editor will be the first page open when the game begins */
		public bool startFromPage = false;
		/** The text alignment */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** An ActionList to run whenever a new page is added */
		public ActionListAsset actionListOnAddPage;
		/** What type of journal this is (NewJournal, DisplayExistingJournal) */
		public JournalType journalType = JournalType.NewJournal;
		/** The page offset, if journalType = JournalType.DisplayExistingJournal) */
		public int pageOffset;
		/** The name of the Journal element within the same Menu that is used as reference, if journalType = JournalType.DisplayExistingJournal) */
		public string otherJournalTitle;

		private string fullText;
		private MenuJournal otherJournal;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiText = null;

			pages = new List<JournalPage>();
			pages.Add (new JournalPage ());
			numPages = 1;
			showPage = 1;
			isVisible = true;
			isClickable = false;
			numSlots = 1;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (10f, 5f));
			textEffects = TextEffects.None;
			outlineSize = 2f;
			fullText = "";
			actionListOnAddPage = null;
			journalType = JournalType.NewJournal;
			pageOffset = 0;
			otherJournalTitle = "";

			base.Declare ();
		}


		/**
		 * <summary>Creates and returns a new MenuJournal that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <returns>A new MenuJournal with the same values as itself</returns>
		 */
		public override MenuElement DuplicateSelf (bool fromEditor)
		{
			MenuJournal newElement = CreateInstance <MenuJournal>();
			newElement.Declare ();
			newElement.CopyJournal (this, fromEditor);
			return newElement;
		}
		
		
		private void CopyJournal (MenuJournal _element, bool fromEditor)
		{
			uiText = _element.uiText;
			pages = new List<JournalPage>();
			foreach (JournalPage page in _element.pages)
			{
				JournalPage newPage = new JournalPage (page);
				if (fromEditor)
				{
					newPage.lineID = -1;
				}

				pages.Add (newPage);
			}

			numPages = _element.numPages;
			startFromPage = _element.startFromPage;
			if (startFromPage)
			{
				showPage = _element.showPage;
			}
			else
			{
				showPage = 1;
			}
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			fullText = "";
			actionListOnAddPage = _element.actionListOnAddPage;
			journalType = _element.journalType;
			pageOffset = _element.pageOffset;
			otherJournalTitle = _element.otherJournalTitle;;

			base.Copy (_element);
		}


		public override void Initialise (AC.Menu _menu)
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				MenuElement sharedElement = _menu.GetElementWithName (otherJournalTitle);
				if (sharedElement != null && sharedElement is MenuJournal)
				{
					otherJournal = (MenuJournal) sharedElement;
				}
			}
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu)
		{
			uiText = LinkUIElement <Text>();
		}
		

		/**
		 * <summary>Gets the boundary of the element</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <returns>The boundary Rect of the element</returns>
		 */
		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiText)
			{
				return uiText.rectTransform;
			}
			return null;
		}


		/**
		 * <summary>Gets the currently-viewed page number.</summary>
		 * <returns>The currently-viewed page number</returms>
		 */
		public int GetCurrentPageNumber ()
		{
			return showPage;
		}


		/**
		 * <summary>Gets the total number of pages.</summary>
		 * <returns>The total number of pages</returns>
		 */
		public int GetTotalNumberOfPages ()
		{
			if (pages != null)
			{
				return pages.Count;
			}
			return 0;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			MenuSource source = menu.menuSource;
			EditorGUILayout.BeginVertical ("Button");

			journalType = (JournalType) EditorGUILayout.EnumPopup ("Journal type:", journalType);
			if (journalType == JournalType.DisplayExistingJournal)
			{
				EditorGUILayout.HelpBox ("This Journal will share pages from another Journal element in the same Menu.", MessageType.Info);
				otherJournalTitle = EditorGUILayout.TextField ("Existing element name:", otherJournalTitle);
				pageOffset = EditorGUILayout.IntField ("Page offset #:", pageOffset);

				if (pages == null || pages.Count != 1)
				{
					pages.Clear ();
					pages.Add (new JournalPage ());
				}

				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Placeholder text:", GUILayout.Width (146f));
				pages[0].text = EditorGUILayout.TextArea (pages[0].text);
				showPage = 1;
				EditorGUILayout.EndHorizontal ();

				if (source == MenuSource.AdventureCreator)
				{
					anchor = (TextAnchor) EditorGUILayout.EnumPopup ("Text alignment:", anchor);
					textEffects = (TextEffects) EditorGUILayout.EnumPopup ("Text effect:", textEffects);
					if (textEffects != TextEffects.None)
					{
						outlineSize = EditorGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f);
					}
				}
				else
				{
					EditorGUILayout.EndVertical ();
					EditorGUILayout.BeginVertical ("Button");
					uiText = LinkedUiGUI <Text> (uiText, "Linked Text:", source);
				}
			}
			else
			{
				if (pages == null || pages.Count == 0)
				{
					pages.Clear ();
					pages.Add (new JournalPage ());
				}
				numPages = pages.Count;

				for (int i=0; i<pages.Count; i++)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Page #" + (i+1).ToString () + ":");
					if (GUILayout.Button ("-", GUILayout.Width (20f)))
					{
						Undo.RecordObject (this, "Delete journal page");
						pages.RemoveAt (i);
						break;
					}
					EditorGUILayout.EndHorizontal ();

					pages[i].text = EditorGUILayout.TextArea (pages[i].text);
					GUILayout.Box ("", GUILayout.ExpandWidth (true), GUILayout.Height(1));
				}

				if (GUILayout.Button ("Create new page", EditorStyles.miniButton))
				{
					Undo.RecordObject (this, "Create journal page");
					pages.Add (new JournalPage ());
				}

				numPages = pages.Count;

				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");

				if (numPages > 1)
				{
					showPage = EditorGUILayout.IntSlider ("Preview page #:", showPage, 1, numPages);
					startFromPage = EditorGUILayout.Toggle ("Start from this page?", startFromPage);
				}
				else
				{
					showPage = 1;
				}

				if (source == MenuSource.AdventureCreator)
				{
					anchor = (TextAnchor) EditorGUILayout.EnumPopup ("Text alignment:", anchor);
					textEffects = (TextEffects) EditorGUILayout.EnumPopup ("Text effect:", textEffects);
				}
				else
				{
					EditorGUILayout.EndVertical ();
					EditorGUILayout.BeginVertical ("Button");
					uiText = LinkedUiGUI <Text> (uiText, "Linked Text:", source);
				}

				actionListOnAddPage = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList on add page:", actionListOnAddPage, typeof (ActionListAsset), false);
			}
			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}
		
		#endif


		/**
		 * <summary>Performs all calculations necessary to display the element.</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language to display text in</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (Application.isPlaying && journalType == JournalType.DisplayExistingJournal)
			{
				if (otherJournal != null)
				{
					int index = otherJournal.showPage + pageOffset - 1;
					if (otherJournal.pages.Count > index)
					{
						fullText = TranslatePage (otherJournal.pages[index], languageNumber);
					}
					else
					{
						fullText = "";
					}
					fullText = AdvGame.ConvertTokens (fullText, languageNumber);
				}
			}
			else
			{
				if (pages.Count == 0)
				{
					fullText = "";
				}
				else if (pages.Count >= showPage && showPage > 0)
				{
					fullText = TranslatePage (pages[showPage - 1], languageNumber);
					fullText = AdvGame.ConvertTokens (fullText, languageNumber);
				}
			}

			if (uiText != null)
			{
				UpdateUIElement (uiText);
				uiText.text = fullText;
			}
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
			
			_style.wordWrap = true;
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}

			if (pages.Count >= showPage)
			{
				if (textEffects != TextEffects.None)
				{
					AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), fullText, _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
				}
				else
				{
					GUI.Label (ZoomRect (relativeRect, zoom), fullText, _style);
				}
			}
		}


		/**
		 * <summary>Gets the display text of the current page</summary>
		 * <param name = "slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the current page</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				if (otherJournal != null)
				{
					int index = otherJournal.showPage + pageOffset - 1;
					if (index >= 0 && otherJournal.pages.Count > index)
					{
						return TranslatePage (otherJournal.pages [index], languageNumber);
					}
				}
				return "";
			}

			int i = showPage - 1;
			if (i >= 0 && pages.Count > i)
			{
				return TranslatePage (pages [i], languageNumber);
			}
			return "";
		}


		/**
		 * <summary>Shifts which slots are on display, if the number of slots the element has exceeds the number of slots it can show at once.</summary>
		 * <param name = "shiftType">The direction to shift pages in (Left, Right)</param>
		 * <param name = "doLoop">If True, then shifting right beyond the last page will display the first page, and vice-versa</param>
		 * <param name = "amount">The amount to shift pages by</param>
		 */
		public void Shift (AC_ShiftInventory shiftType, bool doLoop, int amount)
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				ACDebug.LogWarning ("The journal '" + title + "' cannot be shifted - instead its linked journal (" + otherJournalTitle + ") must be shifted instead.");
				return;
			}

			if (shiftType == AC_ShiftInventory.ShiftRight)
			{
				showPage += amount;
			}
			else if (shiftType == AC_ShiftInventory.ShiftLeft)
			{
				showPage -= amount;
			}

			if (showPage < 1)
			{
				if (doLoop)
				{
					showPage = pages.Count;
				}
				else
				{
					showPage = 1;
				}
			}
			else if (showPage > pages.Count)
			{
				if (doLoop)
				{
					showPage = 1;
				}
				else
				{
					showPage = pages.Count;
				}
			}
		}


		private string TranslatePage (JournalPage page, int languageNumber)
		{
			if (languageNumber > 0)
			{
				return (KickStarter.runtimeLanguages.GetTranslation (page.text, page.lineID, languageNumber));
			}
			return page.text;
		}

		
		protected override void AutoSize ()
		{
			string pageText = "";
			if (Application.isPlaying && journalType == JournalType.DisplayExistingJournal)
			{
				if (otherJournal != null)
				{
					int index = otherJournal.showPage + pageOffset - 1;
					if (index >= 0 && otherJournal.pages.Count > index)
					{
						pageText = otherJournal.pages [index].text;
					}
				}
			}
			else
			{
				int index = showPage - 1;
				if (index >= 0 && pages.Count > index)
				{
					pageText = pages [index].text;
				}
			}

			if (pageText == "" && backgroundTexture != null)
			{
				GUIContent content = new GUIContent (backgroundTexture);
				AutoSize (content);
			}
			else
			{
				GUIContent content = new GUIContent (pageText);
				AutoSize (content);
			}
		}


		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (journalType == JournalType.DisplayExistingJournal || pages.Count == 0)
			{
				return false;
			}

			if (shiftType == AC_ShiftInventory.ShiftLeft)
			{
				if (showPage == 1)
				{
					return false;
				}
			}
			else
			{
				if (pages.Count <= showPage)
				{
					return false;
				}
			}
			return true;
		}


		/**
		 * <summary>Adds a page to the journal.</summary>
		 * <param name = "newPage">The page to add</param>
		 * <param name = "onlyAddNew">If True, then the page will not be added if its lineID number matches that of any page already in the journal</param>
		 * <param name = "index">The index number to insert the page into. A value of -1 will cause it to be added at the end.<param>
		 */
		public void AddPage (JournalPage newPage, bool onlyAddNew, int index = -1)
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				ACDebug.LogWarning ("The journal '" + title + "' cannot be added to - instead its linked journal (" + otherJournalTitle + ") must be modified instead.");
				return;
			}

			if (onlyAddNew && newPage.lineID >= 0 && pages != null && pages.Count > 0)
			{
				// Check for existing to avoid duplicates
				foreach (JournalPage page in pages)
				{
					if (page.lineID == newPage.lineID)
					{
						return;
					}
				}
			}

			if (index == -1)
			{
				index = pages.Count;
			}

			if (index < 0 || index >= pages.Count)
			{
				pages.Add (newPage);
			}
			else
			{
				pages.Insert (index, newPage);
			}

			if (showPage > index || showPage == 0)
			{
				showPage ++;
			}

			AdvGame.RunActionListAsset (actionListOnAddPage);
		}


		/**
		 * <summary>Removes a page from the journal.</summary>
		 * <param name = "index">The page number to remove. A value of -1 will cause the last page to be removed.<param>
		 */
		public void RemovePage (int index = -1)
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				ACDebug.LogWarning ("The journal '" + title + "' cannot be modified - instead its linked journal (" + otherJournalTitle + ") must be modified instead.");
				return;
			}

			if (pages.Count == 0)
			{
				return;
			}

			if (index == -1)
			{
				index = pages.Count - 1;
			}

			if (index < 0)
			{
				pages.RemoveAt (pages.Count-1);
			}
			else if (index < pages.Count)
			{
				pages.RemoveAt (index);
			}
			else
			{
				ACDebug.LogWarning ("The journal '" + title + "' cannot have it's " + index + " page removed, as it only has " + pages.Count + " pages!");
			}

			if (showPage > index)// && showPage > 1)
			{
				showPage --;
			}
		}

	}


	/**
	 * A data container for the contents of each page in a MenuJournal.
	 */
	[System.Serializable]
	public class JournalPage
	{

		/** The translation ID, as set by SpeechManager */
		public int lineID = -1;
		/** The page text, in its original language */
		public string text = "";


		/**
		 * The default Constructor.
		 */
		public JournalPage ()
		{ }


		public JournalPage (JournalPage journalPage)
		{
			lineID = journalPage.lineID;
			text = journalPage.text;
		}


		public JournalPage (int _lineID, string _text)
		{
			lineID = _lineID;
			text = _text;
		}

	}

}