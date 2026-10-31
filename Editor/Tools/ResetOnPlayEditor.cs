using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StuMillman.Toolkit.Editor.Tools
{
    /// <summary>
    /// Snapshots [ResetOnPlay] static fields when scripts load and restores them before each Play.
    /// </summary>
    [InitializeOnLoad]
    static class ResetOnPlayEditor
    {
        static readonly List<(FieldInfo field, object snapshot)> Snapshots = new();

        static readonly MethodInfo MemberwiseCloneMethod =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        static ResetOnPlayEditor()
        {
            foreach (var field in TypeCache.GetFieldsWithAttribute<ResetOnPlayAttribute>())
            {
                var where = $"{field.DeclaringType?.FullName}.{field.Name}";
                if (!field.IsStatic)
                {
                    Debug.LogWarning($"[Toolkit] [ResetOnPlay] only works on static fields: {where}");
                    continue;
                }

                if (field.DeclaringType == null || field.DeclaringType.ContainsGenericParameters)
                {
                    Debug.LogWarning($"[Toolkit] [ResetOnPlay] isn't supported on generic types: {where}");
                    continue;
                }

                try
                {
                    Snapshots.Add((field, Copy(field.GetValue(null))));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Toolkit] [ResetOnPlay] couldn't snapshot {where}: {e.Message}");
                }
            }

            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    RestoreAll();
            };
        }

        static void RestoreAll()
        {
            foreach (var (field, snapshot) in Snapshots)
            {
                try
                {
                    Restore(field, snapshot);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Toolkit] [ResetOnPlay] couldn't reset {field.DeclaringType?.FullName}.{field.Name}: {e.Message}");
                }
            }
        }

        static void Restore(FieldInfo field, object snapshot)
        {
            if (!field.IsInitOnly)
            {
                field.SetValue(null, Copy(snapshot));
                return;
            }

            // static readonly: the reference can't change, but collection contents can be refilled.
            switch (field.GetValue(null))
            {
                case IList list when snapshot is IList items && !list.IsFixedSize:
                    list.Clear();
                    foreach (var item in items) list.Add(item);
                    break;
                case IDictionary dict when snapshot is IDictionary entries:
                    dict.Clear();
                    foreach (DictionaryEntry e in entries) dict.Add(e.Key, e.Value);
                    break;
                default:
                    if (!Equals(field.GetValue(null), snapshot))
                        Debug.LogWarning($"[Toolkit] [ResetOnPlay] can't reset static readonly {field.DeclaringType?.FullName}.{field.Name}. Remove 'readonly'.");
                    break;
            }
        }

        /// <summary>Copies a value so later changes to the live field don't affect the snapshot (and vice versa).</summary>
        static object Copy(object value)
        {
            switch (value)
            {
                case null:
                case string:
                case UnityEngine.Object:
                case Delegate:
                case Type:
                    return value;
                case Array array:
                    return array.Clone();
            }

            var type = value.GetType();
            if (type.IsValueType)
                return value; // Boxed copy.

            // Collections: use a copy constructor, e.g. new List<T>(IEnumerable<T>) or new Dictionary<K,V>(IDictionary<K,V>).
            if (value is IEnumerable)
            {
                foreach (var ctor in type.GetConstructors())
                {
                    var parameters = ctor.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(value) &&
                        parameters[0].ParameterType != typeof(int))
                        return ctor.Invoke(new[] { value });
                }
            }

            // Anything else: shallow clone.
            return MemberwiseCloneMethod.Invoke(value, null);
        }
    }
}
