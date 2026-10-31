namespace StuMillman.Toolkit.Input
{
    /// <summary>Which set of prompts to show.</summary>
    public enum InputDeviceFamily
    {
        KeyboardMouse,
        Xbox,
        PlayStation,
        Switch,
        /// <summary>Any other gamepad. Falls back to Xbox icons.</summary>
        Gamepad,
    }
}
