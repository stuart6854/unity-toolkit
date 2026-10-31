using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace StuMillman.Toolkit.Editor.VFX
{
    /// <summary>
    /// Starter Shuriken effects built from code. Each builder returns a new (unsaved) GameObject.
    /// One-shot effects point along +Z (align them to a hit normal); upward effects point along +Y.
    /// </summary>
    public static class VfxLibrary
    {
        public class Context
        {
            public ParticleMaterials.Set Mats;
            public bool UseTint;
            public Color Tint = Color.white;

            /// <summary>Applies the tint (if enabled) to a colour, keeping its alpha.</summary>
            public Color C(Color c) => UseTint ? new Color(c.r * Tint.r, c.g * Tint.g, c.b * Tint.b, c.a) : c;
        }

        public class Effect
        {
            public string Name;
            public string Category;
            public string Description;
            public Func<Context, GameObject> Build;
        }

        public static readonly Effect[] All =
        {
            new() { Name = "HitSparks", Category = "Combat", Description = "Short burst of stretched sparks along +Z.", Build = HitSparks },
            new() { Name = "MuzzleFlash", Category = "Combat", Description = "Bright flash with a few fast sparks along +Z.", Build = MuzzleFlash },
            new() { Name = "Explosion", Category = "Combat", Description = "Flash, fireball, smoke and debris sparks.", Build = Explosion },
            new() { Name = "DustPuff", Category = "Environment", Description = "Soft dust cloud, e.g. for landings or impacts.", Build = DustPuff },
            new() { Name = "FootstepDust", Category = "Environment", Description = "Small low dust puff for footsteps.", Build = FootstepDust },
            new() { Name = "SmokeColumn", Category = "Environment", Description = "Looping rising smoke.", Build = SmokeColumn },
            new() { Name = "Fire", Category = "Environment", Description = "Looping flames with drifting embers.", Build = Fire },
            new() { Name = "PickupSparkle", Category = "Pickups & Magic", Description = "Golden sparkle burst with an expanding ring.", Build = PickupSparkle },
            new() { Name = "MagicTrail", Category = "Pickups & Magic", Description = "Looping glowing trail; parent it to a moving object.", Build = MagicTrail },
            new() { Name = "HealBurst", Category = "Pickups & Magic", Description = "Green ground ring with rising motes.", Build = HealBurst },
            new() { Name = "LevelUpRing", Category = "Pickups & Magic", Description = "Expanding golden rings with a column of light.", Build = LevelUpRing },
        };

        // ---------------- Combat ----------------

        static GameObject HitSparks(Context c)
        {
            var ps = New("VFX_HitSparks", c.Mats.Spark, out var go);
            var main = Main(ps, duration: 0.5f, loop: false, life: (0.15f, 0.35f), speed: (4f, 9f), size: (0.04f, 0.09f));
            main.startColor = new MinMaxGradient(c.C(new Color(1f, 0.9f, 0.5f)), c.C(new Color(1f, 0.55f, 0.15f)));
            main.gravityModifier = 1f;
            Bursts(ps, 20, 30);
            Cone(ps, 35f, 0.05f);
            FadeOut(ps);
            Stretch(ps, 0.04f, 1.5f);
            Drag(ps, 1.5f);
            return go;
        }

        static GameObject MuzzleFlash(Context c)
        {
            var ps = New("VFX_MuzzleFlash", c.Mats.Glow, out var go);
            var main = Main(ps, duration: 0.2f, loop: false, life: (0.05f, 0.07f), speed: (0f, 0f), size: (0.45f, 0.65f));
            main.startColor = c.C(new Color(1f, 0.85f, 0.45f));
            main.startRotation = new MinMaxCurve(0f, Mathf.PI * 2f);
            Bursts(ps, 1, 1);
            NoShape(ps);
            FadeOut(ps);

            var sparks = Child(go, "Sparks", c.Mats.Spark);
            var sm = Main(sparks, duration: 0.2f, loop: false, life: (0.06f, 0.12f), speed: (8f, 14f), size: (0.03f, 0.05f));
            sm.startColor = c.C(new Color(1f, 0.8f, 0.4f));
            Bursts(sparks, 6, 10);
            Cone(sparks, 12f, 0.02f);
            Stretch(sparks, 0.03f, 1f);
            return go;
        }

        static GameObject Explosion(Context c)
        {
            // Root: initial flash.
            var flash = New("VFX_Explosion", c.Mats.Glow, out var go);
            var fm = Main(flash, duration: 1f, loop: false, life: (0.12f, 0.15f), speed: (0f, 0f), size: (3f, 3.5f));
            fm.startColor = c.C(new Color(1f, 0.85f, 0.5f));
            Bursts(flash, 1, 1);
            NoShape(flash);
            FadeOut(flash);

            var fire = Child(go, "Fireball", c.Mats.Glow);
            var fim = Main(fire, duration: 1f, loop: false, life: (0.35f, 0.6f), speed: (1f, 3f), size: (1f, 2f));
            fim.startRotation = new MinMaxCurve(0f, Mathf.PI * 2f);
            Bursts(fire, 12, 18);
            Sphere(fire, 0.3f);
            ColorOverLife(fire, Grad(
                new[] { (c.C(new Color(1f, 0.9f, 0.5f)), 0f), (c.C(new Color(1f, 0.45f, 0.1f)), 0.4f), (c.C(new Color(0.4f, 0.1f, 0.05f)), 1f) },
                new[] { (1f, 0f), (0.8f, 0.5f), (0f, 1f) }));
            SizeOverLife(fire, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.2f));
            Drag(fire, 3f);

            var smoke = Child(go, "Smoke", c.Mats.Smoke);
            var smm = Main(smoke, duration: 1f, loop: false, life: (1.5f, 2.5f), speed: (0.5f, 1.5f), size: (1.5f, 2.5f));
            smm.startColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
            smm.startRotation = new MinMaxCurve(0f, Mathf.PI * 2f);
            smm.gravityModifier = -0.05f;
            Bursts(smoke, 8, 12, time: 0.05f);
            Sphere(smoke, 0.5f);
            ColorOverLife(smoke, Grad(new[] { (Color.white, 0f), (Color.white, 1f) }, new[] { (0f, 0f), (0.7f, 0.1f), (0f, 1f) }));
            SizeOverLife(smoke, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.6f));
            Spin(smoke, 0.4f);
            Drag(smoke, 2f);

            var debris = Child(go, "Debris", c.Mats.Spark);
            var dm = Main(debris, duration: 1f, loop: false, life: (0.4f, 0.9f), speed: (6f, 12f), size: (0.05f, 0.1f));
            dm.startColor = c.C(new Color(1f, 0.7f, 0.3f));
            dm.gravityModifier = 1.2f;
            Bursts(debris, 25, 35);
            Sphere(debris, 0.2f);
            FadeOut(debris);
            Stretch(debris, 0.04f, 1.5f);
            return go;
        }

        // ---------------- Environment ----------------

        static GameObject DustPuff(Context c)
        {
            var ps = New("VFX_DustPuff", c.Mats.Smoke, out var go);
            var main = Main(ps, duration: 1f, loop: false, life: (0.6f, 1f), speed: (0.5f, 1.5f), size: (0.5f, 1f));
            main.startColor = c.C(new Color(0.75f, 0.68f, 0.55f, 0.6f));
            main.startRotation = new MinMaxCurve(0f, Mathf.PI * 2f);
            Bursts(ps, 8, 12);
            Hemisphere(ps, 0.3f, up: true);
            FadeOut(ps);
            SizeOverLife(ps, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));
            Drag(ps, 3f);
            return go;
        }

        static GameObject FootstepDust(Context c)
        {
            var ps = New("VFX_FootstepDust", c.Mats.Smoke, out var go);
            var main = Main(ps, duration: 0.6f, loop: false, life: (0.4f, 0.7f), speed: (0.3f, 0.8f), size: (0.2f, 0.4f));
            main.startColor = c.C(new Color(0.75f, 0.68f, 0.55f, 0.5f));
            main.startRotation = new MinMaxCurve(0f, Mathf.PI * 2f);
            Bursts(ps, 4, 6);
            GroundCircle(ps, 0.1f);
            FadeOut(ps);
            SizeOverLife(ps, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.3f));
            Drag(ps, 4f);
            return go;
        }

        static GameObject SmokeColumn(Context c)
        {
            var ps = New("VFX_SmokeColumn", c.Mats.Smoke, out var go);
            var main = Main(ps, duration: 5f, loop: true, life: (4f, 6f), speed: (0.5f, 1f), size: (0.8f, 1.2f));
            main.startColor = c.C(new Color(0.4f, 0.4f, 0.4f, 0.5f));
            main.startRotation = new MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            Rate(ps, 6f);
            Cone(ps, 10f, 0.3f);
            PointUp(ps);
            ColorOverLife(ps, Grad(new[] { (Color.white, 0f), (Color.white, 1f) }, new[] { (0f, 0f), (1f, 0.15f), (0f, 1f) }));
            SizeOverLife(ps, AnimationCurve.Linear(0f, 1f, 1f, 3f));
            Spin(ps, 0.3f);
            Noise(ps, 0.3f, 0.3f);
            return go;
        }

        static GameObject Fire(Context c)
        {
            var ps = New("VFX_Fire", c.Mats.Glow, out var go);
            var main = Main(ps, duration: 2f, loop: true, life: (0.6f, 1f), speed: (1f, 2f), size: (0.5f, 0.8f));
            main.startRotation = new MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            Rate(ps, 25f);
            Cone(ps, 8f, 0.2f);
            PointUp(ps);
            ColorOverLife(ps, Grad(
                new[] { (c.C(new Color(1f, 0.85f, 0.4f)), 0f), (c.C(new Color(1f, 0.4f, 0.1f)), 0.5f), (c.C(new Color(0.6f, 0.1f, 0.05f)), 1f) },
                new[] { (0f, 0f), (1f, 0.15f), (0f, 1f) }));
            SizeOverLife(ps, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            Spin(ps, 1f);

            var embers = Child(go, "Embers", c.Mats.Spark);
            var em = Main(embers, duration: 2f, loop: true, life: (1f, 2f), speed: (1f, 3f), size: (0.03f, 0.06f));
            em.startColor = c.C(new Color(1f, 0.6f, 0.2f));
            em.simulationSpace = ParticleSystemSimulationSpace.World;
            Rate(embers, 5f);
            Cone(embers, 15f, 0.25f);
            PointUp(embers);
            FadeOut(embers);
            Noise(embers, 0.6f, 0.8f);
            return go;
        }

        // ---------------- Pickups & Magic ----------------

        static GameObject PickupSparkle(Context c)
        {
            var ps = New("VFX_PickupSparkle", c.Mats.Glow, out var go);
            var main = Main(ps, duration: 1f, loop: false, life: (0.5f, 0.8f), speed: (0.5f, 2f), size: (0.08f, 0.15f));
            main.startColor = c.C(new Color(1f, 0.85f, 0.35f));
            Bursts(ps, 20, 25);
            Sphere(ps, 0.3f);
            SizeOverLife(ps, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            Drag(ps, 2f);

            var ring = Child(go, "Ring", c.Mats.Ring);
            var rm = Main(ring, duration: 1f, loop: false, life: (0.4f, 0.4f), speed: (0f, 0f), size: (1.5f, 1.5f));
            rm.startColor = c.C(new Color(1f, 0.85f, 0.35f));
            Bursts(ring, 1, 1);
            NoShape(ring);
            FadeOut(ring);
            SizeOverLife(ring, AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f));
            return go;
        }

        static GameObject MagicTrail(Context c)
        {
            var ps = New("VFX_MagicTrail", c.Mats.Glow, out var go);
            var main = Main(ps, duration: 1f, loop: true, life: (0.4f, 0.8f), speed: (0f, 0.2f), size: (0.1f, 0.2f));
            main.startColor = new MinMaxGradient(c.C(new Color(0.6f, 0.4f, 1f)), c.C(new Color(0.3f, 0.7f, 1f)));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 5f;
            emission.rateOverDistance = 20f;
            Sphere(ps, 0.05f);
            SizeOverLife(ps, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            Noise(ps, 0.2f, 1f);
            return go;
        }

        static GameObject HealBurst(Context c)
        {
            var ps = New("VFX_HealBurst", c.Mats.Glow, out var go);
            var main = Main(ps, duration: 1.5f, loop: false, life: (0.8f, 1.2f), speed: (0f, 0f), size: (0.1f, 0.2f));
            main.startColor = c.C(new Color(0.4f, 1f, 0.5f));
            Bursts(ps, 25, 30);
            GroundCircle(ps, 0.6f);
            Velocity(ps, new Vector3(0f, 1f, 0f), new Vector3(0f, 2f, 0f));
            FadeOut(ps);

            var ring = Child(go, "GroundRing", c.Mats.Ring);
            var rm = Main(ring, duration: 1.5f, loop: false, life: (0.6f, 0.6f), speed: (0f, 0f), size: (2f, 2f));
            rm.startColor = c.C(new Color(0.4f, 1f, 0.5f));
            Bursts(ring, 1, 1);
            NoShape(ring);
            FadeOut(ring);
            SizeOverLife(ring, AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f));
            Flat(ring);
            return go;
        }

        static GameObject LevelUpRing(Context c)
        {
            var ring = New("VFX_LevelUpRing", c.Mats.Ring, out var go);
            var rm = Main(ring, duration: 1.5f, loop: false, life: (0.8f, 0.8f), speed: (0f, 0f), size: (3f, 3f));
            rm.startColor = c.C(new Color(1f, 0.85f, 0.35f));
            var em = ring.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new Burst(0f, 1, 1), new Burst(0.2f, 1, 1), new Burst(0.4f, 1, 1) });
            NoShape(ring);
            FadeOut(ring);
            SizeOverLife(ring, AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f));
            Flat(ring);

            var column = Child(go, "Column", c.Mats.Glow);
            var cm = Main(column, duration: 1.5f, loop: false, life: (0.8f, 1.2f), speed: (0f, 0f), size: (0.1f, 0.25f));
            cm.startColor = c.C(new Color(1f, 0.9f, 0.5f));
            Bursts(column, 30, 40);
            GroundCircle(column, 0.5f);
            Velocity(column, new Vector3(0f, 2f, 0f), new Vector3(0f, 4f, 0f));
            FadeOut(column);
            return go;
        }

        // ---------------- Helpers ----------------

        static ParticleSystem New(string name, Material material, out GameObject go)
        {
            go = new GameObject(name);
            return Setup(go, material);
        }

        static ParticleSystem Child(GameObject parent, string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return Setup(go, material);
        }

        static ParticleSystem Setup(GameObject go, Material material)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        static MainModule Main(ParticleSystem ps, float duration, bool loop, (float, float) life, (float, float) speed, (float, float) size)
        {
            var main = ps.main;
            main.duration = duration;
            main.loop = loop;
            main.playOnAwake = true;
            main.startLifetime = new MinMaxCurve(life.Item1, life.Item2);
            main.startSpeed = new MinMaxCurve(speed.Item1, speed.Item2);
            main.startSize = new MinMaxCurve(size.Item1, size.Item2);
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.maxParticles = 200;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            return main;
        }

        static void Bursts(ParticleSystem ps, int min, int max, float time = 0f)
        {
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new Burst(time, (short)min, (short)max) });
        }

        static void Rate(ParticleSystem ps, float rate)
        {
            var em = ps.emission;
            em.rateOverTime = rate;
        }

        static void Cone(ParticleSystem ps, float angle, float radius)
        {
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = angle;
            sh.radius = radius;
        }

        static void Sphere(ParticleSystem ps, float radius)
        {
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = radius;
        }

        static void Hemisphere(ParticleSystem ps, float radius, bool up)
        {
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius = radius;
            if (up) sh.rotation = new Vector3(-90f, 0f, 0f);
        }

        /// <summary>Flat circle on the ground (XZ plane), emitting outwards.</summary>
        static void GroundCircle(ParticleSystem ps, float radius)
        {
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = radius;
            sh.rotation = new Vector3(90f, 0f, 0f);
        }

        static void NoShape(ParticleSystem ps)
        {
            var sh = ps.shape;
            sh.enabled = false;
        }

        /// <summary>Rotates the shape so a cone emits along +Y instead of +Z.</summary>
        static void PointUp(ParticleSystem ps)
        {
            var sh = ps.shape;
            sh.rotation = new Vector3(-90f, 0f, 0f);
        }

        static void FadeOut(ParticleSystem ps) =>
            ColorOverLife(ps, Grad(new[] { (Color.white, 0f), (Color.white, 1f) }, new[] { (1f, 0f), (1f, 0.6f), (0f, 1f) }));

        static void ColorOverLife(ParticleSystem ps, Gradient gradient)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new MinMaxGradient(gradient);
        }

        static void SizeOverLife(ParticleSystem ps, AnimationCurve curve)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new MinMaxCurve(1f, curve);
        }

        static void Spin(ParticleSystem ps, float radiansPerSecond)
        {
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new MinMaxCurve(-radiansPerSecond, radiansPerSecond);
        }

        static void Drag(ParticleSystem ps, float drag)
        {
            var lv = ps.limitVelocityOverLifetime;
            lv.enabled = true;
            lv.drag = drag;
            lv.limit = 1000f;
        }

        static void Noise(ParticleSystem ps, float strength, float frequency)
        {
            var n = ps.noise;
            n.enabled = true;
            n.strength = strength;
            n.frequency = frequency;
        }

        static void Velocity(ParticleSystem ps, Vector3 min, Vector3 max)
        {
            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = new MinMaxCurve(min.x, max.x);
            v.y = new MinMaxCurve(min.y, max.y);
            v.z = new MinMaxCurve(min.z, max.z);
        }

        static void Stretch(ParticleSystem ps, float velocityScale, float lengthScale)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = velocityScale;
            r.lengthScale = lengthScale;
        }

        /// <summary>Renders flat on the ground (horizontal billboard).</summary>
        static void Flat(ParticleSystem ps)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        }

        static Gradient Grad((Color color, float time)[] colors, (float alpha, float time)[] alphas)
        {
            var g = new Gradient();
            var ck = new GradientColorKey[colors.Length];
            for (var i = 0; i < colors.Length; i++) ck[i] = new GradientColorKey(colors[i].color, colors[i].time);
            var ak = new GradientAlphaKey[alphas.Length];
            for (var i = 0; i < alphas.Length; i++) ak[i] = new GradientAlphaKey(alphas[i].alpha, alphas[i].time);
            g.SetKeys(ck, ak);
            return g;
        }
    }
}
