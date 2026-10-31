#if TOOLKIT_INPUTSYSTEM && TOOLKIT_UITOOLKIT
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace StuMillman.Toolkit.Input
{
    /// <summary>
    /// UI Toolkit element showing a prompt like "Press {Jump} to jump" with inline icons.
    /// Use in UXML: &lt;StuMillman.Toolkit.Input.InputPromptElement template="Press {Jump} to jump" /&gt;
    /// </summary>
    [UxmlElement]
    public partial class InputPromptElement : VisualElement
    {
        public const string UssClass = "input-prompt";
        public const string TextUssClass = UssClass + "__text";
        public const string IconUssClass = UssClass + "__icon";

        string _template = "Press {Jump} to jump";
        float _iconSize = 24f;

        [UxmlAttribute]
        public string Template
        {
            get => _template;
            set
            {
                _template = value;
                Refresh();
            }
        }

        [UxmlAttribute]
        public float IconSize
        {
            get => _iconSize;
            set
            {
                _iconSize = value;
                Refresh();
            }
        }

        /// <summary>Optional. Defaults to the project-wide actions.</summary>
        public InputActionAsset Actions { get; set; }

        /// <summary>Optional. Defaults to Resources/InputPromptIcons.</summary>
        public InputPromptIcons Icons { get; set; }

        public InputPromptElement()
        {
            AddToClassList(UssClass);
            style.flexDirection = FlexDirection.Row;
            style.flexWrap = Wrap.Wrap;
            style.alignItems = Align.Center;

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                InputDeviceTracker.Changed += OnDeviceChanged;
                InputSystem.onActionChange += OnActionChange;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                InputDeviceTracker.Changed -= OnDeviceChanged;
                InputSystem.onActionChange -= OnActionChange;
            });

            Refresh();
        }

        void OnDeviceChanged(InputDeviceFamily _) => Refresh();

        void OnActionChange(object _, InputActionChange change)
        {
            if (change == InputActionChange.BoundControlsChanged)
                Refresh();
        }

        public void Refresh()
        {
            Clear();
            var icons = Icons ?? InputPrompts.Icons;
            var fallback = icons != null ? icons.fallbackFormat : "[{0}]";

            foreach (var part in InputPrompts.Parse(_template, InputDeviceTracker.Current, Actions, icons))
            {
                if (part.HasTexture)
                {
                    var icon = new VisualElement();
                    icon.AddToClassList(IconUssClass);
                    icon.style.width = _iconSize;
                    icon.style.height = _iconSize;
                    icon.style.backgroundImage = new StyleBackground(part.Icon.texture);
                    icon.tooltip = part.Text;
                    Add(icon);
                }
                else
                {
                    var label = new Label(part.IsIcon ? string.Format(fallback, part.Text) : part.Text);
                    label.AddToClassList(TextUssClass);
                    label.style.paddingLeft = 0;
                    label.style.paddingRight = 0;
                    label.style.marginLeft = 0;
                    label.style.marginRight = 0;
                    label.style.whiteSpace = WhiteSpace.Pre; // Keep spaces between words and icons.
                    Add(label);
                }
            }
        }
    }
}
#endif
