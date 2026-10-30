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
            Undo.postprocessModifications += OnModificacionManual;
            QueueRefresh(); // Cuando se importan scripts o se vuelve a abrir el proyecto.
        }

        /// <summary>
        /// En cuanto el equipo toca a mano un objeto del preview (lo arrastra, le
        /// cambia un sprite, lo que sea), el nivel deja de ser temporal y pasa a ser
        /// parte de la escena.
        ///
        /// Sin esto, el preview esta marcado DontSaveInEditor: Unity no lo guarda y
        /// al entrar en Play se borra y el builder lo regenera, asi que cualquier
        /// cambio hecho a mano se pierde. Que es justo lo que pasaba.
        /// </summary>
        static UndoPropertyModification[] OnModificacionManual(UndoPropertyModification[] mods)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return mods;

            foreach (var mod in mods)
            {
                Object target = mod.currentValue != null ? mod.currentValue.target : null;
                if (target == null) continue;

                Transform t = target as Transform;
                if (t == null)
                {
                    var comp = target as Component;
                    if (comp != null) t = comp.transform;
                    else
                    {
                        var go = target as GameObject;
                        if (go != null) t = go.transform;
                    }
                }
                if (t == null) continue;

                if (HacerPermanente(t)) break;   // una vez por tanda basta
            }
            return mods;
        }

        /// <summary>
        /// Si el transform esta dentro de un preview temporal, convierte TODO el
        /// nivel en permanente y apaga la reconstruccion automatica.
        /// Devuelve true si hizo el cambio.
        /// </summary>
        static bool HacerPermanente(Transform t)
        {
            LevelBuilder builder = FindBuilder(t.gameObject.scene);
            if (builder == null) return false;

            Transform raiz = FindGeneratedRoot(builder);
            if (raiz == null || !IsTemporary(raiz)) return false;
            if (!t.IsChildOf(raiz)) return false;

            MarcarPermanenteRecursivo(raiz);
            builder.buildOnAwake = false;

            EditorUtility.SetDirty(builder);
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);

            Debug.Log("[Chocoflan] Tocaste el nivel a mano, asi que deja de ser una vista previa " +
                      "y pasa a formar parte de la escena. 'Build On Awake' se apago para que no " +
                      "se regenere al darle Play. Guarda con Cmd+S.", builder);
            return true;
        }

        static void MarcarPermanenteRecursivo(Transform t)
        {
            t.gameObject.hideFlags = HideFlags.None;
            for (int i = 0; i < t.childCount; i++)
                MarcarPermanenteRecursivo(t.GetChild(i));
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
