using System;

namespace StuMillman.Toolkit
{
    /// <summary>
    /// Restores a static field to its initial value every time you enter Play Mode in the editor.
    /// Use it when "Enter Play Mode Options" has domain reload turned off, so statics don't leak between plays.
    /// </summary>
    /// <remarks>
    /// Value types and strings are restored exactly. Arrays and collections get a fresh shallow copy.
    /// Other objects get a shallow clone (UnityEngine.Objects and delegates keep the same reference).
    /// Has no effect in builds.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ResetOnPlayAttribute : Attribute
    {
    }
}
