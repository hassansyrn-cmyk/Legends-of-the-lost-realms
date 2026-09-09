using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LostRealms.EditorTools
{
    /// One-shot integration: configures Mixamo FBX importers as humanoid, renames clips,
    /// builds the hero + stone-brute AnimatorControllers, and wires LevelBuilder references.
    public static class RigIntegration
    {
        private const string HeroModel = "Assets/Models/Hero/Aster_Mixamo.fbx";
        private const string HeroClips = "Assets/Models/Hero/Clips";
        private const string BruteClips = "Assets/Models/StoneBrute/Clips";
        private const string HeroController = "Assets/Resources/Hero.controller";
        private const string BruteController = "Assets/Resources/StoneBrute.controller";

        [MenuItem("LostRealms/Integrate Mixamo Rigs")]
        public static void Integrate() => Run(heroPrefab: true);

        public static void Run(bool heroPrefab)
        {
            Directory.CreateDirectory("Assets/Resources");

            ConfigureClipFiles(HeroClips, new[]
            {
                ("sword and shield idle.fbx", "HeroIdle", true),
                ("sword and shield run.fbx", "HeroRun", true),
                ("sword and shield walk.fbx", "HeroWalk", true),
                ("sword and shield jump.fbx", "HeroJump", false),
                ("sword and shield attack.fbx", "HeroAttack1", false),
                ("sword and shield attack (2).fbx", "HeroAttack2", false),
                ("sword and shield attack (3).fbx", "HeroAttack3", false),
                ("sword and shield attack (4).fbx", "HeroAttack4", false),
                ("sword and shield impact.fbx", "HeroHurt", false),
                ("sword and shield death.fbx", "HeroDeath", false),
            });

            ConfigureClipFiles(BruteClips, new[]
            {
                ("Idle_CrossSource.fbx", "BruteIdle", true),
                ("Walk_CrossSource.fbx", "BruteWalk", true),
                ("Golem_Punch.fbx", "BruteAttack", false),
                ("Golem_Death.fbx", "BruteDeath", false),
            });

            if (heroPrefab)
            {
                var heroImporter = ModelImporter.GetAtPath(HeroModel) as ModelImporter;
                if (heroImporter != null)
                {
                    heroImporter.animationType = ModelImporterAnimationType.Human;
                    heroImporter.SaveAndReimport();
                }
            }

            var hero = BuildHeroController();
            var brute = BuildBruteController();

            File.WriteAllText("Assets/Resources/wired.txt", $"hero={hero.name}\nbrute={brute.name}\n");
            AssetDatabase.SaveAssets();
            Debug.Log("RigIntegration: controllers built into Assets/Resources. Run SceneBootstrap to wire prefabs.");
        }

        private static void ConfigureClipFiles(string folder, (string file, string clipName, bool loop)[] clips)
        {
            foreach (var (file, clipName, loop) in clips)
            {
                string path = $"{folder}/{file}";
                var importer = ModelImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) { Debug.LogWarning($"RigIntegration: missing {path}"); continue; }
                importer.animationType = ModelImporterAnimationType.Human;
                importer.importAnimation = true;

                var so = new SerializedObject(importer);
                var clipAnimations = so.FindProperty("m_ClipAnimations");
                importer.clipAnimations = importer.defaultClipAnimations;
                var arr = importer.clipAnimations;
                if (arr.Length == 0) { Debug.LogWarning($"RigIntegration: no clips in {path}"); continue; }
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i].name = arr.Length == 1 ? clipName : $"{clipName}_{i}";
                    arr[i].loopTime = loop;
                }
                importer.clipAnimations = arr;
                importer.SaveAndReimport();
            }
        }

        private static AnimatorStateMachine AddState(AnimatorController controller, string name, string clipName, AnimatorControllerLayer layer)
        {
            var clip = LoadClip(clipName);
            var state = layer.stateMachine.AddState(name);
            if (clip != null) state.motion = clip;
            state.writeDefaultValuesOnEnter = true;
            return state;
        }

        private static Motion LoadClip(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Models" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null && clip.name == name) return clip;
            }
            Debug.LogWarning($"RigIntegration: clip '{name}' not found");
            return null;
        }

        private static void Param(AnimatorController c, string name, AnimatorControllerParameterType t)
        {
            if (!System.Array.Exists(c.parameters, p => p.name == name))
                c.AddParameter(name, t);
        }

        private static AnimatorStateTransition Link(AnimatorState from, AnimatorState to, string cond, AnimatorControllerParameterType t, float threshold, bool value, float exitTime, float duration)
        {
            var tr = from.AddTransition(to);
            tr.hasExitTime = string.IsNullOrEmpty(cond);
            tr.exitTime = exitTime;
            tr.duration = duration;
            tr.hasFixedDuration = true;
            if (!string.IsNullOrEmpty(cond))
            {
                if (t == AnimatorControllerParameterType.Float) tr.AddCondition(AnimatorConditionMode.Greater, threshold, cond);
                else if (t == AnimatorControllerParameterType.Bool) tr.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, cond);
                else tr.AddCondition(AnimatorConditionMode.If, 0, cond);
            }
            return tr;
        }

        private static AnimatorController BuildHeroController()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(HeroController) != null)
                AssetDatabase.DeleteAsset(HeroController);
            var c = AnimatorController.CreateAnimatorControllerAtPath(HeroController);
            var layer = c.layers[0];

            Param(c, "Speed", AnimatorControllerParameterType.Float);
            Param(c, "Grounded", AnimatorControllerParameterType.Bool);
            Param(c, "Attack", AnimatorControllerParameterType.Trigger);
            Param(c, "Hurt", AnimatorControllerParameterType.Trigger);
            Param(c, "Death", AnimatorControllerParameterType.Trigger);

            var idle = AddState(c, "Idle", "HeroIdle", layer);
            var run = AddState(c, "Run", "HeroRun", layer);
            var jump = AddState(c, "Jump", "HeroJump", layer);
            var atk = AddState(c, "Attack", "HeroAttack1", layer);
            var hurt = AddState(c, "Hurt", "HeroHurt", layer);
            var death = AddState(c, "Death", "HeroDeath", layer);

            // locomotion
            Link(idle, run, "Speed", AnimatorControllerParameterType.Float, 0.2f, false, 0f, 0.15f);
            Link(run, idle, "Speed", AnimatorControllerParameterType.Float, -1f, false, 0f, 0.15f).AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
            Link(idle, jump, "Grounded", AnimatorControllerParameterType.Bool, 0f, false, 0f, 0.08f);
            Link(run, jump, "Grounded", AnimatorControllerParameterType.Bool, 0f, false, 0f, 0.08f);
            Link(jump, idle, "Grounded", AnimatorControllerParameterType.Bool, 0f, true, 0f, 0.1f);

            // one-shot states from any state
            var atkTr = layer.stateMachine.AddAnyStateTransition(atk);
            atkTr.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            atkTr.hasExitTime = false; atkTr.duration = 0.05f;
            Link(atk, idle, null, 0, 0, false, 0.98f, 0.1f);

            var hurtTr = layer.stateMachine.AddAnyStateTransition(hurt);
            hurtTr.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
            hurtTr.hasExitTime = false; hurtTr.duration = 0.05f;
            Link(hurt, idle, null, 0, 0, false, 0.98f, 0.1f);

            var deathTr = layer.stateMachine.AddAnyStateTransition(death);
            deathTr.AddCondition(AnimatorConditionMode.If, 0, "Death");
            deathTr.hasExitTime = false; deathTr.duration = 0.05f;

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            Debug.Log($"RigIntegration: hero controller at {HeroController}");
            return c;
        }

        private static AnimatorController BuildBruteController()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(BruteController) != null)
                AssetDatabase.DeleteAsset(BruteController);
            var c = AnimatorController.CreateAnimatorControllerAtPath(BruteController);
            var layer = c.layers[0];

            Param(c, "Speed", AnimatorControllerParameterType.Float);
            Param(c, "Attack", AnimatorControllerParameterType.Trigger);
            Param(c, "Death", AnimatorControllerParameterType.Trigger);

            var idle = AddState(c, "Idle", "BruteIdle", layer);
            var walk = AddState(c, "Walk", "BruteWalk", layer);
            var atk = AddState(c, "Attack", "BruteAttack", layer);
            var death = AddState(c, "Death", "BruteDeath", layer);

            Link(idle, walk, "Speed", AnimatorControllerParameterType.Float, 0.2f, false, 0f, 0.2f);
            Link(walk, idle, "Speed", AnimatorControllerParameterType.Float, -1f, false, 0f, 0.2f).AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");

            var atkTr = layer.stateMachine.AddAnyStateTransition(atk);
            atkTr.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            atkTr.hasExitTime = false; atkTr.duration = 0.08f;
            Link(atk, idle, null, 0, 0, false, 0.98f, 0.15f);

            var deathTr = layer.stateMachine.AddAnyStateTransition(death);
            deathTr.AddCondition(AnimatorConditionMode.If, 0, "Death");
            deathTr.hasExitTime = false; deathTr.duration = 0.08f;

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            Debug.Log($"RigIntegration: brute controller at {BruteController}");
            return c;
        }
    }
}
