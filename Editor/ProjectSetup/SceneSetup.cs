using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#if TOOLKIT_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    public static class SceneSetup
    {
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        const string SampleSceneFolder = "Assets/Scenes";

        public static void CreateScenes(ProjectSetupSettings settings)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var folder = $"Assets/{settings.rootFolder}/{settings.scenesFolder}".Replace('\\', '/');
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();

            var paths = new List<string>();
            var created = 0;

            foreach (var entry in settings.scenes)
            {
                if (string.IsNullOrWhiteSpace(entry.name))
                    continue;

                var path = $"{folder}/{entry.name.Trim()}.unity";
                paths.Add(path);

                if (File.Exists(path))
                    continue; // Never overwrite existing scenes.

                var scene = EditorSceneManager.NewScene(
                    entry.cameraAndLight ? NewSceneSetup.DefaultGameObjects : NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

                var organisers = new Dictionary<string, Transform>();
                if (entry.organisers)
                {
                    foreach (var name in settings.organiserNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                        organisers[name] = new GameObject($"--- {name.Trim()} ---").transform;
                }

                if (entry.globalVolume)
                    AddGlobalVolume(settings, Find(organisers, "Environment"));

                if (entry.testGeometry)
                    AddTestGeometry(settings, Find(organisers, "Environment"));

                EditorSceneManager.SaveScene(scene, path);
                created++;
            }

            UpdateBuildSettings(paths, settings.deleteSampleScene);

            if (paths.Count > 0)
                EditorSceneManager.OpenScene(paths[0], OpenSceneMode.Single);

            if (settings.deleteSampleScene)
                DeleteSampleScene();

            Debug.Log($"[Toolkit] Scene setup done. Created {created} scene(s) in {folder}.");
        }

        static Transform Find(Dictionary<string, Transform> organisers, string name) =>
            organisers.TryGetValue(name, out var t) ? t : null;

        static void AddTestGeometry(ProjectSetupSettings settings, Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            ground.isStatic = true;
            ApplyMaterial(settings, ground, "M_Grey");

            var cubes = new (Vector3 pos, string mat)[]
            {
                (new Vector3(-2f, 0.5f, 2f), "M_White"),
                (new Vector3(2f, 0.5f, 3f), "M_Red"),
                (new Vector3(0f, 1f, 5f), "M_Blue"),
            };

            for (var i = 0; i < cubes.Length; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Cube_{i + 1}";
                cube.transform.SetParent(parent, false);
                cube.transform.localPosition = cubes[i].pos;
                if (i == 2) cube.transform.localScale = Vector3.one * 2f;
                ApplyMaterial(settings, cube, cubes[i].mat);
            }
        }

        static void ApplyMaterial(ProjectSetupSettings settings, GameObject go, string materialName)
        {
            // Uses the default materials if they've been created; otherwise keeps Unity's default.
            var path = $"Assets/{settings.rootFolder}/{settings.materialsFolder}/{materialName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static void AddGlobalVolume(ProjectSetupSettings settings, Transform parent)
        {
#if TOOLKIT_URP
            var go = new GameObject("Global Volume");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = GetOrCreateVolumeProfile(settings);

            var cam = Camera.main;
            if (cam != null)
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
#else
            Debug.LogWarning("[Toolkit] URP is not installed. Skipping Global Volume.");
#endif
        }

#if TOOLKIT_URP
        static VolumeProfile GetOrCreateVolumeProfile(ProjectSetupSettings settings)
        {
            var folder = $"Assets/{settings.rootFolder}/Settings";
            var path = $"{folder}/DefaultVolumeProfile.asset";

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null)
                return profile;

            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.ACES);

            var bloom = profile.Add<Bloom>();
            bloom.intensity.Override(0.3f);
            bloom.threshold.Override(0.9f);

            foreach (var component in profile.components)
            {
                component.name = component.GetType().Name;
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }
#endif

        static void UpdateBuildSettings(List<string> scenePaths, bool removeSample)
        {
            var ours = scenePaths.Select(p => new EditorBuildSettingsScene(p, true));
            var others = EditorBuildSettings.scenes
                .Where(s => !scenePaths.Contains(s.path))
                .Where(s => !(removeSample && s.path == SampleScenePath));

            EditorBuildSettings.scenes = ours.Concat(others).ToArray();
        }

        static void DeleteSampleScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath) != null)
                AssetDatabase.DeleteAsset(SampleScenePath);

            if (AssetDatabase.IsValidFolder(SampleSceneFolder) &&
                AssetDatabase.FindAssets("", new[] { SampleSceneFolder }).Length == 0)
                AssetDatabase.DeleteAsset(SampleSceneFolder);
        }
    }
}
