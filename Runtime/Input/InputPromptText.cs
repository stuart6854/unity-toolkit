#if TOOLKIT_INPUTSYSTEM && TOOLKIT_UGUI
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StuMillman.Toolkit.Input
{
    /// <summary>
    /// Fills a TextMeshPro text from a template like "Press {Jump} to jump", using icons for the current device.
    /// Updates when the player switches device or bindings change.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Toolkit/Input Prompt Text")]
    public class InputPromptText : MonoBehaviour
    {
        [TextArea]
        [Tooltip("Use {Action} or {Map/Action} for controls.")]
        [SerializeField] string template = "Press {Jump} to jump";

        [Tooltip("Optional. Defaults to the project-wide actions.")]
        [SerializeField] InputActionAsset actions;

        [Tooltip("Optional. Defaults to Resources/InputPromptIcons.")]
        [SerializeField] InputPromptIcons icons;

        TMP_Text _text;

        public string Template
        {
            get => template;
            set
            {
                template = value;
                Refresh();
            }
        }

        void OnEnable()
        {
            _text = GetComponent<TMP_Text>();
            InputDeviceTracker.Changed += OnDeviceChanged;
            InputSystem.onActionChange += OnActionChange;
            Refresh();
        }

        void OnDisable()
        {
            InputDeviceTracker.Changed -= OnDeviceChanged;
            InputSystem.onActionChange -= OnActionChange;
        }

        void OnDeviceChanged(InputDeviceFamily _) => Refresh();

        void OnActionChange(object _, InputActionChange change)
        {
            if (change == InputActionChange.BoundControlsChanged)
                Refresh();
        }

        public void Refresh()
        {
            if (_text == null) return;

            var iconSet = icons != null ? icons : InputPrompts.Icons;
            if (iconSet != null && iconSet.spriteAsset != null)
                _text.spriteAsset = iconSet.spriteAsset;

            _text.text = InputPrompts.FormatForTMP(template, InputDeviceTracker.Current, actions, iconSet);
        }
    }
}
#endif
