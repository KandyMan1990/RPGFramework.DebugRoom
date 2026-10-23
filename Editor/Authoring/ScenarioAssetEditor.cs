using System;
using System.Collections.Generic;
using RPGFramework.Core.Editor;
using RPGFramework.Core.Memory;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.DebugRoom.Authoring
{
    /// <summary>
    /// A scenario's values beside the variable map's persistent variables, each in its own type with the map's own
    /// pickers — a field by name, a module from a list — arrays and records opened out. Only the values set apart from
    /// the map's defaults are kept, shown in bold, each with a reset.
    /// </summary>
    [CustomEditor(typeof(ScenarioAsset))]
    internal sealed class ScenarioAssetEditor : UnityEditor.Editor
    {
        private ScenarioValues m_Values;
        private VisualElement  m_List;
        private VisualElement  m_Orphans;
        private TextField      m_Filter;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            PropertyField map  = new PropertyField(serializedObject.FindProperty(ScenarioAsset.VARIABLE_MAP));

            m_Filter  = new TextField("Filter");
            m_Orphans = new VisualElement();
            m_List    = new VisualElement();

            root.Add(map);
            root.Add(new HelpBox("Every persistent variable starts at the map's default but those set here, shown in bold. The debug room loads " +
                                 "the scenario in the editor and in debug builds, going straight to the field and spawn point it names.",
                                 HelpBoxMessageType.Info));
            root.Add(m_Filter);
            root.Add(m_Orphans);
            root.Add(m_List);

            Rebuild();

            map.RegisterValueChangeCallback(_ => Rebuild());
            m_Filter.RegisterValueChangedCallback(_ => ApplyFilter());

            root.RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += Rebuild);
            root.RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= Rebuild);

            return root;
        }

        private void Rebuild()
        {
            serializedObject.Update();

            m_List.Clear();
            m_Orphans.Clear();

            VariableMapAsset map = ((ScenarioAsset)target).VariableMap;

            if (map == null)
            {
                m_List.Add(new HelpBox("Choose the game's variable map.", HelpBoxMessageType.Warning));
                return;
            }

            IPersistentValues              persistent = new PersistentValues(map, map);
            IReadOnlyList<PersistentValue> values     = persistent.Values;
            IVariableMap                   variables  = map;

            m_Values = new ScenarioValues(serializedObject, values);

            // A variable's values lie together, in the map's order.
            for (int start = 0; start < values.Count;)
            {
                int end = start + 1;

                while (end < values.Count && values[end].VariableId == values[start].VariableId)
                {
                    end++;
                }

                VisualElement entry = values[start].IsWholeVariable ? MakeRow(values[start]) : MakeFoldout(variables, values, start, end);

                entry.userData = values[start].VariableName;
                m_List.Add(entry);

                start = end;
            }

            ShowOrphans();
            ApplyFilter();
        }

        // An array's or a record's values are made only as it is first opened, as there can be hundreds.
        private VisualElement MakeFoldout(IVariableMap variables, IReadOnlyList<PersistentValue> values, int start, int end)
        {
            string name  = values[start].VariableName;
            int    count = variables.TryGetVariable(name, out VariableDefinition variable) ? variable.Count : 1;

            Foldout foldout = new Foldout { text = count == 1 ? name : $"{name} [{count}]", value = false };
            bool    anySet  = false;

            for (int i = start; i < end; i++)
            {
                anySet |= m_Values.IsSet(values[i]);
            }

            foldout.Q<Label>().style.unityFontStyleAndWeight = anySet ? FontStyle.Bold : FontStyle.Normal;

            foldout.RegisterValueChangedCallback(e =>
                                                 {
                                                     if (e.target != foldout || !e.newValue || foldout.contentContainer.childCount > 0)
                                                     {
                                                         return;
                                                     }

                                                     for (int i = start; i < end; i++)
                                                     {
                                                         foldout.Add(MakeRow(values[i]));
                                                     }
                                                 });

            return foldout;
        }

        private VisualElement MakeRow(PersistentValue value)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            bool   set          = m_Values.IsSet(value);
            string variableName = value.IsWholeVariable ? value.VariableName : null;

            VariableValueField field = new VariableValueField(value.Label, value.Width, m_Values.Get(value), bits => Write(row, value, bits), variableName, m_Values);

            VisualElement shown = field;

            // A picker carries its own label, so the variable's name goes above it.
            if (field.IsPicker)
            {
                shown = new VisualElement();
                shown.Add(new Label(value.Label));
                shown.Add(field);
            }

            Label label = shown.Q<Label>();

            if (label != null)
            {
                label.style.unityFontStyleAndWeight = set ? FontStyle.Bold : FontStyle.Normal;
            }

            shown.style.flexGrow = 1;

            Button reset = new Button(() => ResetValue(row, value)) { text = "Reset", tooltip = "Start this value at the map's default" };
            reset.SetEnabled(set);

            row.Add(shown);
            row.Add(reset);

            return row;
        }

        private void Write(VisualElement row, PersistentValue value, ulong bits)
        {
            bool wasSet = m_Values.IsSet(value);

            m_Values.Set(value, bits);

            if (wasSet != m_Values.IsSet(value))
            {
                Replace(row, value);
            }
        }

        private void ResetValue(VisualElement row, PersistentValue value)
        {
            m_Values.Clear(value);
            Replace(row, value);
        }

        // The row made again, in its place, so it shows what is now set and its reset.
        private void Replace(VisualElement row, PersistentValue value)
        {
            VisualElement parent = row.parent;

            if (parent == null)
            {
                return;
            }

            VisualElement made = MakeRow(value);

            made.userData = row.userData;
            parent.Insert(parent.IndexOf(row), made);
            row.RemoveFromHierarchy();
        }

        private void ShowOrphans()
        {
            int orphans = m_Values.CountOrphans();

            if (orphans == 0)
            {
                return;
            }

            m_Orphans.Add(new HelpBox($"{orphans} value(s) are for variables, fields or elements the map no longer has, and are not used.", HelpBoxMessageType.Warning));
            m_Orphans.Add(new Button(() =>
                                     {
                                         m_Values.RemoveOrphans();
                                         Rebuild();
                                     })
                          {
                              text = "Remove them"
                          });
        }

        private void ApplyFilter()
        {
            string filter = m_Filter.value ?? string.Empty;

            for (int i = 0; i < m_List.childCount; i++)
            {
                VisualElement entry = m_List[i];
                string        name  = entry.userData as string;
                bool          shown = name == null || filter.Length == 0 || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                entry.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// The scenario's values as its serialized object holds them, a value equal to the map's default never kept, so
        /// the map's default can change under it; and, to the map's pickers, the values around the one they set.
        /// </summary>
        private sealed class ScenarioValues : IVariableValueContext
        {
            private readonly SerializedObject               m_Scenario;
            private readonly IReadOnlyList<PersistentValue> m_Persistent;

            internal ScenarioValues(SerializedObject scenario, IReadOnlyList<PersistentValue> persistent)
            {
                m_Scenario   = scenario;
                m_Persistent = persistent;
            }

            ulong IVariableValueContext.ValueOf(string variableName)
            {
                for (int i = 0; i < m_Persistent.Count; i++)
                {
                    if (m_Persistent[i].IsWholeVariable && m_Persistent[i].VariableName == variableName)
                    {
                        return Get(m_Persistent[i]);
                    }
                }

                return 0UL;
            }

            void IVariableValueContext.Watch(VisualElement owner, string variableName, Action changed)
            {
                owner.TrackSerializedObjectValue(m_Scenario, _ => changed());
            }

            internal bool IsSet(PersistentValue value)
            {
                return Find(value) != null;
            }

            internal ulong Get(PersistentValue value)
            {
                SerializedProperty entry = Find(value);

                return entry == null ? value.Default : entry.FindPropertyRelative(ScenarioValue.VALUE).ulongValue;
            }

            internal void Set(PersistentValue value, ulong bits)
            {
                if (bits == value.Default)
                {
                    Clear(value);
                    return;
                }

                SerializedProperty entry = Find(value);

                if (entry == null)
                {
                    SerializedProperty values = Values();

                    values.arraySize++;
                    entry = values.GetArrayElementAtIndex(values.arraySize - 1);

                    entry.FindPropertyRelative(ScenarioValue.VARIABLE_ID).uintValue = value.VariableId;
                    entry.FindPropertyRelative(ScenarioValue.INDEX).intValue        = value.Index;
                    entry.FindPropertyRelative(ScenarioValue.FIELD_ID).uintValue    = value.FieldId;
                    entry.FindPropertyRelative(ScenarioValue.FIELD_INDEX).intValue  = value.FieldIndex;
                }

                entry.FindPropertyRelative(ScenarioValue.VALUE).ulongValue = bits;

                m_Scenario.ApplyModifiedProperties();
            }

            internal void Clear(PersistentValue value)
            {
                SerializedProperty values = Values();

                for (int i = values.arraySize - 1; i >= 0; i--)
                {
                    if (Matches(values.GetArrayElementAtIndex(i), value))
                    {
                        values.DeleteArrayElementAtIndex(i);
                    }
                }

                m_Scenario.ApplyModifiedProperties();
            }

            internal int CountOrphans()
            {
                SerializedProperty values  = Values();
                int                orphans = 0;

                for (int i = 0; i < values.arraySize; i++)
                {
                    if (!IsKnown(values.GetArrayElementAtIndex(i)))
                    {
                        orphans++;
                    }
                }

                return orphans;
            }

            internal void RemoveOrphans()
            {
                SerializedProperty values = Values();

                for (int i = values.arraySize - 1; i >= 0; i--)
                {
                    if (!IsKnown(values.GetArrayElementAtIndex(i)))
                    {
                        values.DeleteArrayElementAtIndex(i);
                    }
                }

                m_Scenario.ApplyModifiedProperties();
            }

            private SerializedProperty Values()
            {
                return m_Scenario.FindProperty(ScenarioAsset.VALUES);
            }

            private SerializedProperty Find(PersistentValue value)
            {
                SerializedProperty values = Values();

                for (int i = 0; i < values.arraySize; i++)
                {
                    SerializedProperty entry = values.GetArrayElementAtIndex(i);

                    if (Matches(entry, value))
                    {
                        return entry;
                    }
                }

                return null;
            }

            private bool IsKnown(SerializedProperty entry)
            {
                for (int i = 0; i < m_Persistent.Count; i++)
                {
                    if (Matches(entry, m_Persistent[i]))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool Matches(SerializedProperty entry, PersistentValue value)
            {
                return entry.FindPropertyRelative(ScenarioValue.VARIABLE_ID).uintValue == value.VariableId &&
                       entry.FindPropertyRelative(ScenarioValue.INDEX).intValue        == value.Index      &&
                       entry.FindPropertyRelative(ScenarioValue.FIELD_ID).uintValue    == value.FieldId    &&
                       entry.FindPropertyRelative(ScenarioValue.FIELD_INDEX).intValue  == value.FieldIndex;
            }
        }
    }
}
