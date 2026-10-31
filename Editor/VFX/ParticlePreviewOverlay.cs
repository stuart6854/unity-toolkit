using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace StuMillman.Toolkit.Editor.VFX
{
    /// <summary>
    /// Scene view overlay for the selected particle effect: loop one-shots, restart, play/pause,
    /// simulation speed and live stats. Shown when a GameObject with a ParticleSystem is selected.
    /// </summary>
    [Overlay(typeof(SceneView), "stumillman-toolkit-particle-preview", "Particle Preview", true)]
    public class ParticlePreviewOverlay : Overlay
    {
        const string LoopKey = "StuMillman.Toolkit.ParticlePreview.Loop";
        const float LoopDelay = 0.4f;

        Toggle _loop;
        Button _playPause;
        Slider _speed;
        Label _stats;
        double _deadSince = -1;

        // Unity's own preview speed (internal). Used if available so it matches the built-in controls.
        static readonly PropertyInfo SpeedProperty = typeof(UnityEditor.Editor).Assembly
            .GetType("UnityEditor.ParticleSystemEditorUtils")
            ?.GetProperty("simulationSpeed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        static ParticleSystem Root
        {
            get
            {
                var go = Selection.activeGameObject;
                if (go == null) return null;
                var ps = go.GetComponentInParent<ParticleSystem>();
                if (ps == null) return go.GetComponentInChildren<ParticleSystem>();
                // Walk up to the top-most particle system so the whole effect is controlled.
                var parent = ps.transform.parent != null ? ps.transform.parent.GetComponentInParent<ParticleSystem>() : null;
                while (parent != null)
                {
                    ps = parent;
                    parent = ps.transform.parent != null ? ps.transform.parent.GetComponentInParent<ParticleSystem>() : null;
                }
                return ps;
            }
        }

        public override void OnCreated()
        {
            Selection.selectionChanged += UpdateVisibility;
            EditorApplication.update += Tick;
            UpdateVisibility();
        }

        public override void OnWillBeDestroyed()
        {
            Selection.selectionChanged -= UpdateVisibility;
            EditorApplication.update -= Tick;
        }

        void UpdateVisibility() => displayed = Root != null;

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement { style = { minWidth = 220, paddingLeft = 4, paddingRight = 4, paddingBottom = 4 } };

            _loop = new Toggle("Loop") { value = EditorPrefs.GetBool(LoopKey, true) };
            _loop.RegisterValueChangedCallback(e => EditorPrefs.SetBool(LoopKey, e.newValue));
            root.Add(_loop);

            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var restart = new Button(Restart) { text = "Restart" };
            restart.style.flexGrow = 1;
            _playPause = new Button(TogglePlay) { text = "Pause" };
            _playPause.style.flexGrow = 1;
            buttons.Add(restart);
            buttons.Add(_playPause);
            root.Add(buttons);

            if (SpeedProperty != null)
            {
                _speed = new Slider("Speed", 0.05f, 3f) { value = GetSpeed(), showInputField = true };
                _speed.RegisterValueChangedCallback(e => SetSpeed(e.newValue));
                root.Add(_speed);
            }

            _stats = new Label { style = { unityFontStyleAndWeight = FontStyle.Normal, marginTop = 4, whiteSpace = WhiteSpace.Normal } };
            root.Add(_stats);
            return root;
        }

        void Tick()
        {
            if (!displayed) return;
            var ps = Root;
            if (ps == null) return;

            if (_stats != null)
            {
                var systems = ps.GetComponentsInChildren<ParticleSystem>();
                var count = systems.Sum(s => s.particleCount);
                var duration = systems.Max(s => s.main.duration + s.main.startLifetime.constantMax);
                _stats.text = $"Particles: {count}\nSystems: {systems.Length}\nDuration: ~{duration:0.00}s{(ps.main.loop ? " (looping)" : "")}";
            }

            if (_playPause != null)
                _playPause.text = ps.isPaused ? "Play" : "Pause";

            if (_speed != null && Mathf.Abs(_speed.value - GetSpeed()) > 0.001f)
                _speed.SetValueWithoutNotify(GetSpeed());

            // Loop one-shot effects: restart shortly after everything has died.
            if (_loop != null && _loop.value && !ps.isPaused && !ps.IsAlive(true))
            {
                if (_deadSince < 0) _deadSince = EditorApplication.timeSinceStartup;
                else if (EditorApplication.timeSinceStartup - _deadSince > LoopDelay)
                {
                    _deadSince = -1;
                    Restart();
                }
            }
            else
            {
                _deadSince = -1;
            }

            SceneView.RepaintAll();
        }

        static void Restart()
        {
            var ps = Root;
            if (ps == null) return;
            ps.Simulate(0f, true, true);
            ps.Play(true);
        }

        void TogglePlay()
        {
            var ps = Root;
            if (ps == null) return;
            if (ps.isPaused) ps.Play(true);
            else ps.Pause(true);
        }

        static float GetSpeed() => SpeedProperty != null ? (float)SpeedProperty.GetValue(null) : 1f;

        static void SetSpeed(float value) => SpeedProperty?.SetValue(null, value);
    }
}
