using System;
using System.Collections.Generic;
using RPGFramework.Core.Audio;
using RPGFramework.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// What the room shows, in the layout its scene's document gives it: a button for each page and one for the size of
    /// a step along the top, reached by moving up from the first row; the open lists' path; the rows of the one on top;
    /// what the controls do; and a text field for a name, opened when one is asked for. It needs only the moves, submit
    /// and cancel every menu has, and a keyboard to type a name. It reports the player's choices to the room, which
    /// decides what follows and plays its sounds; only the cursor's move sounds here, as in the menus.
    /// </summary>
    internal sealed class DebugRoomUI
    {
        private const string CURRENT_PAGE_CLASS    = "debug-room__page--current";
        private const string OPEN_NAME_ENTRY_CLASS = "debug-room__name-entry--open";

        internal event Action<int> RowChosen;
        internal event Action<int> PageChosen;
        internal event Action      StepChosen;

        /// <summary>Enter was pressed in the name entry, with the name typed.</summary>
        internal event Action<string> NameEntered;

        /// <summary>A row was stepped left, -1, or right, 1.</summary>
        internal event Action<int, int> RowStepped;

        private readonly ScrollView        m_Rows;
        private readonly Label             m_PathLabel;
        private readonly Label             m_HintLabel;
        private readonly VisualElement     m_NameEntry;
        private readonly Label             m_NamePrompt;
        private readonly TextField         m_NameField;
        private readonly RPGUIButton[]     m_PageButtons;
        private readonly RPGUIButton       m_StepButton;
        private readonly RPGUIButton[]     m_TopButtons;
        private readonly SelectionScope    m_Selection;
        private readonly List<RPGUIButton> m_Buttons;

        private bool          m_InputEnabled;
        private int           m_Page;
        private VisualElement m_QuietFocus;

        internal DebugRoomUI(VisualElement root, IAudioIntentPlayer audioIntentPlayer, string[] pageNames)
        {
            VisualElement room  = root.Q("DebugRoom");
            VisualElement pages = room.Q("Pages");

            m_Rows         = room.Q<ScrollView>("Rows");
            m_PathLabel    = room.Q<Label>("PathLabel");
            m_HintLabel    = room.Q<Label>("HintLabel");
            m_NameEntry    = room.Q("NameEntry");
            m_NamePrompt   = room.Q<Label>("NamePrompt");
            m_NameField    = room.Q<TextField>("NameField");
            m_PageButtons  = new RPGUIButton[pageNames.Length];
            m_StepButton   = new RPGUIButton();
            m_TopButtons   = new RPGUIButton[pageNames.Length + 1];
            m_Selection    = new SelectionScope(room, audioIntentPlayer, AudioContext.Menu);
            m_Buttons      = new List<RPGUIButton>();
            m_InputEnabled = false;
            m_Page         = 0;
            m_QuietFocus   = null;

            root.RegisterCallback<FocusInEvent>(OnFocusInBeforeScope, TrickleDown.TrickleDown);
            root.RegisterCallback<FocusInEvent>(OnFocusInAfterScope);

            // Keys bound to navigation or submit are letters too, so as a name is typed they move nothing and choose
            // nothing; Enter alone gives the name.
            m_NameField.RegisterCallback<KeyDownEvent>(OnNameKeyDown, TrickleDown.TrickleDown);
            m_NameField.RegisterCallback<NavigationMoveEvent>(StopInNameField, TrickleDown.TrickleDown);
            m_NameField.RegisterCallback<NavigationSubmitEvent>(StopInNameField, TrickleDown.TrickleDown);

            for (int i = 0; i < pageNames.Length; i++)
            {
                int         page   = i;
                RPGUIButton button = new RPGUIButton();

                button.text = pageNames[i];
                button.AddToClassList("debug-room__page");
                button.Chosen += () => PageChosen?.Invoke(page);

                pages.Add(button);
                m_PageButtons[i] = button;
                m_TopButtons[i]  = button;
            }

            m_StepButton.AddToClassList("debug-room__step");
            m_StepButton.Chosen += () => StepChosen?.Invoke();

            pages.Add(m_StepButton);
            m_TopButtons[pageNames.Length] = m_StepButton;

            RPGUIButton.LinkRow(m_TopButtons, true);
        }

        internal void SetInputEnabled(bool enabled)
        {
            m_InputEnabled = enabled;
            m_Selection.SetInputEnabled(enabled);
        }

        internal void SetPage(int page)
        {
            m_Page = page;

            for (int i = 0; i < m_PageButtons.Length; i++)
            {
                m_PageButtons[i].EnableInClassList(CURRENT_PAGE_CLASS, i == page);
            }
        }

        internal void SetStep(string step)
        {
            m_StepButton.text = step;
        }

        internal void SetPath(string path)
        {
            m_PathLabel.text = path;
        }

        internal void SetHint(string hint)
        {
            m_HintLabel.text = hint;
        }

        /// <summary>
        /// Shows a row for each text, the cursor on <paramref name="focusedRow" />, or on the page's button when there
        /// are none, without its sound.
        /// </summary>
        internal void SetRows(string[] texts, int focusedRow)
        {
            while (m_Buttons.Count < texts.Length)
            {
                AddButton();
            }

            while (m_Buttons.Count > texts.Length)
            {
                m_Buttons[m_Buttons.Count - 1].RemoveFromHierarchy();
                m_Buttons.RemoveAt(m_Buttons.Count - 1);
            }

            for (int i = 0; i < texts.Length; i++)
            {
                m_Buttons[i].text = texts[i];
            }

            LinkRowsToTop();

            RPGUIButton focused = texts.Length == 0 ? m_PageButtons[m_Page] : m_Buttons[focusedRow];

            m_QuietFocus = focused;
            focused.Focus();

            // Rows just added have no layout to scroll to until the next one.
            m_Rows.schedule.Execute(() => m_Rows.ScrollTo(focused));
        }

        internal void SetRowText(int row, string text)
        {
            m_Buttons[row].text = text;
        }

        /// <summary>Opens the name entry, empty, with the cursor in it and without its sound.</summary>
        internal void AskForName(string prompt)
        {
            m_NamePrompt.text = prompt;
            m_NameField.SetValueWithoutNotify(string.Empty);
            m_NameEntry.AddToClassList(OPEN_NAME_ENTRY_CLASS);

            m_QuietFocus = m_NameField;
            m_NameField.Focus();
        }

        internal void CloseNameEntry()
        {
            m_NameEntry.RemoveFromClassList(OPEN_NAME_ENTRY_CLASS);
        }

        private void OnNameKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            evt.StopPropagation();
            NameEntered?.Invoke(m_NameField.value);
        }

        private void StopInNameField(EventBase evt)
        {
            evt.StopPropagation();
        }

        // The rows run down from the top's buttons, the first row's up leading back to the page's own.
        private void LinkRowsToTop()
        {
            RPGUIButton[] rows = m_Buttons.ToArray();

            RPGUIButton.LinkColumn(rows, false);

            RPGUIButton first = rows.Length > 0 ? rows[0] : null;

            for (int i = 0; i < m_TopButtons.Length; i++)
            {
                m_TopButtons[i].Down = first;
            }

            if (first != null)
            {
                first.Up = m_PageButtons[m_Page];
            }
        }

        // The selection scope sounds every focus move while input is on, and UI Toolkit may deliver the focus after the
        // call that asked for it has returned, so a focus the room makes itself, after a sound of its own, is kept quiet
        // by turning the scope's input off around its delivery, above and below the scope's own handler.
        private void OnFocusInBeforeScope(FocusInEvent evt)
        {
            if (!IsQuiet(evt))
            {
                m_QuietFocus = null;
                return;
            }

            m_Selection.SetInputEnabled(false);
        }

        private void OnFocusInAfterScope(FocusInEvent evt)
        {
            if (!IsQuiet(evt))
            {
                return;
            }

            m_QuietFocus = null;
            m_Selection.SetInputEnabled(m_InputEnabled);
        }

        // A text field passes its focus to the element inside it that takes the text.
        private bool IsQuiet(FocusInEvent evt)
        {
            VisualElement target = evt.target as VisualElement;

            return m_QuietFocus != null && target != null && (target == m_QuietFocus || m_QuietFocus.Contains(target));
        }

        private void AddButton()
        {
            int         row    = m_Buttons.Count;
            RPGUIButton button = new RPGUIButton();

            button.Chosen += () => RowChosen?.Invoke(row);
            button.RegisterCallback<NavigationMoveEvent>(evt => OnMove(evt, row));
            button.RegisterCallback<FocusInEvent>(_ => m_Rows.ScrollTo(button));

            m_Rows.Add(button);
            m_Buttons.Add(button);
        }

        // Up and down move the cursor, which the selection scope does before this; left and right step the row.
        private void OnMove(NavigationMoveEvent evt, int row)
        {
            int direction = evt.direction switch
                            {
                                NavigationMoveEvent.Direction.Left  => -1,
                                NavigationMoveEvent.Direction.Right => 1,
                                _                                   => 0
                            };

            if (direction == 0)
            {
                return;
            }

            evt.StopPropagation();
            RowStepped?.Invoke(row, direction);
        }
    }
}
