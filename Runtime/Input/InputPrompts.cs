#if TOOLKIT_INPUTSYSTEM
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StuMillman.Toolkit.Input
{
    /// <summary>
    /// Turns templates like "Press {Jump} to jump" into prompts for the current device.
    /// Tokens are action names ({Jump}) or map/action ({Player/Jump}).
    /// </summary>
    public static class InputPrompts
    {
        /// <summary>
        /// One piece of a parsed template. For controls, Icon is set (its texture/spriteName may be empty when
        /// there's no icon) and Text is the control's display name. For plain text, Icon is null.
        /// </summary>
        public readonly struct Part
        {
            public readonly string Text;
            public readonly InputPromptIcons.Entry Icon;
            public bool IsIcon => Icon != null;
            public bool HasTexture => Icon != null && Icon.texture != null;

            public Part(string text, InputPromptIcons.Entry icon)
            {
                Text = text;
                Icon = icon;
            }
        }

        static readonly Regex Token = new(@"\{([^{}]+)\}");

        static InputPromptIcons _icons;
        static bool _iconsLoaded;
        static InputActionAsset _actions;

        /// <summary>Default icons. Loaded from Resources/InputPromptIcons unless set.</summary>
        public static InputPromptIcons Icons
        {
            get
            {
                if (_icons == null && !_iconsLoaded)
                {
                    _icons = Resources.Load<InputPromptIcons>("InputPromptIcons");
                    _iconsLoaded = true;
                }
                return _icons;
            }
            set
            {
                _icons = value;
                _iconsLoaded = true;
            }
        }

        /// <summary>Default actions. Uses the project-wide actions (Project Settings > Input System) unless set.</summary>
        public static InputActionAsset Actions
        {
            get => _actions != null ? _actions : InputSystem.actions;
            set => _actions = value;
        }

        /// <summary>Splits a template into text and control parts for a device family.</summary>
        public static List<Part> Parse(string template, InputDeviceFamily family, InputActionAsset actions = null, InputPromptIcons icons = null)
        {
            actions ??= Actions;
            icons ??= Icons;
            var parts = new List<Part>();
            if (string.IsNullOrEmpty(template))
                return parts;

            var last = 0;
            foreach (Match m in Token.Matches(template))
            {
                if (m.Index > last)
                    parts.Add(new Part(template.Substring(last, m.Index - last), null));

                var action = actions != null ? actions.FindAction(m.Groups[1].Value) : null;
                if (action == null)
                    parts.Add(new Part($"{{?{m.Groups[1].Value}}}", null));
                else
                    AddControlParts(parts, action, family, icons);

                last = m.Index + m.Length;
            }

            if (last < template.Length)
                parts.Add(new Part(template.Substring(last), null));

            return parts;
        }

        /// <summary>Builds TextMeshPro rich text: icons as &lt;sprite name="..."&gt;, others as fallback text.</summary>
        public static string FormatForTMP(string template, InputDeviceFamily family, InputActionAsset actions = null, InputPromptIcons icons = null)
        {
            icons ??= Icons;
            var fallback = icons != null ? icons.fallbackFormat : "[{0}]";
            var sb = new StringBuilder();
            foreach (var part in Parse(template, family, actions, icons))
            {
                if (part.IsIcon && !string.IsNullOrEmpty(part.Icon.spriteName))
                    sb.Append("<sprite name=\"").Append(part.Icon.spriteName).Append("\">");
                else if (part.IsIcon)
                    sb.Append(string.Format(fallback, part.Text));
                else
                    sb.Append(part.Text);
            }
            return sb.ToString();
        }

        /// <summary>Plain text version, e.g. "Press [Space] to jump".</summary>
        public static string FormatPlain(string template, InputDeviceFamily family, InputActionAsset actions = null, InputPromptIcons icons = null)
        {
            icons ??= Icons;
            var fallback = icons != null ? icons.fallbackFormat : "[{0}]";
            var sb = new StringBuilder();
            foreach (var part in Parse(template, family, actions, icons))
                sb.Append(part.IsIcon ? string.Format(fallback, part.Text) : part.Text);
            return sb.ToString();
        }

        static void AddControlParts(List<Part> parts, InputAction action, InputDeviceFamily family, InputPromptIcons icons)
        {
            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isPartOfComposite)
                    continue;

                if (b.isComposite)
                {
                    var added = false;
                    for (var j = i + 1; j < bindings.Count && bindings[j].isPartOfComposite; j++)
                    {
                        if (!Matches(bindings[j].effectivePath, family)) continue;
                        parts.Add(ControlPart(action, j, family, icons));
                        added = true;
                    }
                    if (added) return;
                    continue;
                }

                if (Matches(b.effectivePath, family))
                {
                    parts.Add(ControlPart(action, i, family, icons));
                    return;
                }
            }

            parts.Add(new Part("?", new InputPromptIcons.Entry())); // No binding for this device.
        }

        static Part ControlPart(InputAction action, int bindingIndex, InputDeviceFamily family, InputPromptIcons icons)
        {
            var path = action.bindings[bindingIndex].effectivePath;
            var display = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            if (string.IsNullOrEmpty(display)) display = path;

            var key = ControlKey(path);
            if (icons != null && key != null && icons.TryGet(family, key, out var entry))
                return new Part(display, entry);

            // No icon: an Entry with no sprite marks this as a control (so it gets the fallback format).
            return new Part(display, new InputPromptIcons.Entry());
        }

        /// <summary>"&lt;XInputController&gt;/buttonSouth" -> "Gamepad/buttonSouth"; "&lt;Keyboard&gt;/space" -> "Keyboard/space".</summary>
        public static string ControlKey(string path)
        {
            var layout = InputControlPath.TryGetDeviceLayout(path);
            var slash = path.IndexOf('/');
            if (layout == null || slash < 0) return null;

            var control = path.Substring(slash + 1);
            if (IsBasedOn(layout, "Keyboard")) return "Keyboard/" + control;
            if (IsBasedOn(layout, "Mouse")) return "Mouse/" + control;
            if (IsBasedOn(layout, "Gamepad")) return "Gamepad/" + control;
            return null;
        }

        static bool Matches(string path, InputDeviceFamily family)
        {
            var layout = InputControlPath.TryGetDeviceLayout(path);
            if (string.IsNullOrEmpty(layout)) return false;

            if (family == InputDeviceFamily.KeyboardMouse)
                return IsBasedOn(layout, "Keyboard") || IsBasedOn(layout, "Mouse") || IsBasedOn(layout, "Pointer");

            if (!IsBasedOn(layout, "Gamepad")) return false;
            if (layout == "Gamepad") return true; // Generic gamepad bindings match every gamepad.

            return family switch
            {
                InputDeviceFamily.PlayStation => IsBasedOn(layout, "DualShockGamepad"),
                InputDeviceFamily.Xbox => IsBasedOn(layout, "XInputController"),
                InputDeviceFamily.Switch => IsBasedOn(layout, "SwitchProControllerHID"),
                _ => true,
            };
        }

        static bool IsBasedOn(string layout, string baseLayout)
        {
            if (layout == baseLayout) return true;
            try
            {
                return InputSystem.IsFirstLayoutBasedOnSecond(layout, baseLayout);
            }
            catch
            {
                return false; // Layout not registered on this platform.
            }
        }
    }
}
#endif
