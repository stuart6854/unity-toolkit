using System;
using System.Collections.Generic;
using UnityEngine;
#if TOOLKIT_UGUI
using TMPro;
#endif

namespace StuMillman.Toolkit.Input
{
    /// <summary>
    /// Maps controls to icons per device family. Put one named "InputPromptIcons" in a Resources folder
    /// to use it by default (Tools > Toolkit > Input Prompts creates one for you).
    /// </summary>
    [CreateAssetMenu(menuName = "Toolkit/Input Prompt Icons", fileName = "InputPromptIcons")]
    public class InputPromptIcons : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public InputDeviceFamily family;

            [Tooltip("Device kind and control, e.g. Keyboard/space, Mouse/leftButton, Gamepad/buttonSouth, Gamepad/leftStick.")]
            public string control;

            [Tooltip("Icon for UI Toolkit and for building the TMP sprite asset.")]
            public Texture2D texture;

            [Tooltip("Sprite name in the TMP sprite asset. Filled in when the sprite asset is built.")]
            public string spriteName;
        }

#if TOOLKIT_UGUI
        [Tooltip("TMP sprite asset used by InputPromptText. Built from the entries' textures.")]
        public TMP_SpriteAsset spriteAsset;
#endif

        [Tooltip("Text used when there's no icon. {0} is the control's display name.")]
        public string fallbackFormat = "[{0}]";

        public List<Entry> entries = new();

        Dictionary<(InputDeviceFamily, string), Entry> _lookup;

        void OnEnable() => _lookup = null;
        void OnValidate() => _lookup = null;

        /// <summary>Finds the icon for a control. Generic gamepads use Xbox icons, and vice versa.</summary>
        public bool TryGet(InputDeviceFamily family, string control, out Entry entry)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<(InputDeviceFamily, string), Entry>();
                foreach (var e in entries)
                    if (e != null && !string.IsNullOrEmpty(e.control))
                        _lookup[(e.family, e.control.ToLowerInvariant())] = e;
            }

            var key = control.ToLowerInvariant();
            if (_lookup.TryGetValue((family, key), out entry))
                return true;

            var fallback = family switch
            {
                InputDeviceFamily.Gamepad => InputDeviceFamily.Xbox,
                InputDeviceFamily.Xbox => InputDeviceFamily.Gamepad,
                _ => family,
            };
            return fallback != family && _lookup.TryGetValue((fallback, key), out entry);
        }
    }
}
