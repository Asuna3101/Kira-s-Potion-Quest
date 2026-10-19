using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chocoflan.KiraPotionQuest.EditorTools
{
    /// <summary>
    /// Muestra los niveles generados por LevelBuilder en Scene y Game SIN pulsar Play.
    /// El preview es temporal: no se serializa en .unity y se elimina antes de Play.
    /// Al ejecutar el juego, LevelBuilder sigue generando su nivel normalmente.
    ///
    /// No modifica Level01Data, Level02EnzoData, LevelBuilder ni las escenas .unity.
    /// </summary>
    [InitializeOnLoad]
    public static class LevelEditPreview
    {
        const string Level01Scene = "Assets/Game/Scenes/Level01.unity";
        const string Level02Scene = "Assets/Game/Scenes/Level02_Enzo.unity";
        const HideFlags PreviewFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        static bool s_RefreshQueued;

        static LevelEditPreview()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            QueueRefresh(); // Cuando se importan scripts o se vuelve a abrir el proyecto.
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (IsTarget(scene)) QueueRefresh();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                RemoveTemporaryPreviews();
            else if (state == PlayModeStateChange.EnteredEditMode)
                QueueRefresh();
        }

        [MenuItem("Chocoflan/6 - Actualizar vista previa (sin Play)", false, 15)]
        public static void RefreshFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Chocoflan] Deten Play para actualizar la vista previa.");
                return;
            }

            RefreshAllLoadedScenes();
        }

        static void QueueRefresh()
        {
            if (s_RefreshQueued) return;
            s_RefreshQueued = true;
            EditorApplication.delayCall += () =>
            {
                s_RefreshQueued = false;
                RefreshAllLoadedScenes();
            };
        }

        static void RefreshAllLoadedScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueRefresh(); // Reintentar cuando Unity termine de importar.
                return;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (IsTarget(scene)) EnsurePreview(scene);
            }
        }

        static bool IsTarget(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded &&
                   (scene.path == Level01Scene || scene.path == Level02Scene);
        }

        static LevelBuilder FindBuilder(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var builder = root.GetComponentInChildren<LevelBuilder>(true);
                if (builder != null) return builder;
            }
            return null;
        }

        static Transform FindGeneratedRoot(LevelBuilder builder)
        {
            string rootName = builder.levelLayout == LevelBuilder.LevelLayout.Level02Enzo
                ? "Nivel02_Enzo" : "Nivel01";
            return builder.transform.Find(rootName);
        }

        static bool IsTemporary(Transform obj)
        {
            return obj != null && (obj.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0;
        }

        static void EnsurePreview(Scene scene)
        {
            LevelBuilder builder = FindBuilder(scene);
            if (builder == null) return;

            Transform existing = FindGeneratedRoot(builder);
            if (existing != null)
            {
                // Los sprites generados por PlaceholderArt no sobreviven a una
                // recompilacion: regeneramos el preview, pero nunca tocamos una
                // jerarquia permanente horneada manualmente por el equipo.
                if (!IsTemporary(existing)) return;
                Object.DestroyImmediate(existing.gameObject);
            }

            builder.Rebuild();
            Transform preview = FindGeneratedRoot(builder);
            if (preview == null) return;

            MarkTemporaryRecursive(preview);
            SceneView.RepaintAll();
        }

        static void MarkTemporaryRecursive(Transform transform)
        {
            transform.gameObject.hideFlags = PreviewFlags;
            for (int i = 0; i < transform.childCount; i++)
                MarkTemporaryRecursive(transform.GetChild(i));
        }

        static void RemoveTemporaryPreviews()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!IsTarget(scene)) continue;

                LevelBuilder builder = FindBuilder(scene);
                if (builder == null) continue;

                Transform preview = FindGeneratedRoot(builder);
                if (IsTemporary(preview))
                    Object.DestroyImmediate(preview.gameObject);
            }
        }
    }
}
