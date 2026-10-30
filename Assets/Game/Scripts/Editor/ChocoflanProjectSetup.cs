using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest.EditorTools
{
    /// <summary>
    /// Ajustes de proyecto para las medidas del documento de arte:
    /// 1920 x 1080, PPU 240, camara ortografica 4.5.
    /// Se aplican desde el menu (y no editando ProjectSettings a mano) para que Unity
    /// los serialice bien aunque el Editor este abierto.
    /// </summary>
    public static class ChocoflanProjectSetup
    {
        const string Level01ScenePath = "Assets/Game/Scenes/Level01.unity";
        const string MainMenuScenePath = "Assets/Game/Scenes/MainMenu.unity";

        [MenuItem("Chocoflan/1 - Aplicar ajustes de proyecto", false, 10)]
        public static void ApplyProjectSettings()
        {
            // Resolucion por defecto del Player.
            PlayerSettings.defaultScreenWidth = GridSettings.ReferenceWidthPx;
            PlayerSettings.defaultScreenHeight = GridSettings.ReferenceHeightPx;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            // Anti aliasing fuera: desenfoca los bordes de pixel.
            int levels = QualitySettings.names.Length;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < levels; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.antiAliasing = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            }
            QualitySettings.SetQualityLevel(current, false);

            // Snap del editor a 1 / PPU para colocar cosas a mano sin medios pixeles.
            float step = 1f / GridSettings.AssetsPixelsPerUnit; // 0.03125
            EditorSnapSettings.move = new Vector3(step, step, step);
            EditorSnapSettings.gridSnapEnabled = true;

            // El menu va primero: es la escena con la que arranca el juego.
            // Todos los niveles tienen que estar en Build Settings para que el
            // boton MENU del HUD pueda volver, y el menu pueda cargarlos.
            AddSceneToBuildSettings("Assets/Game/Scenes/Level02.unity", 0);
            AddSceneToBuildSettings(Level01ScenePath, 0);
            AddSceneToBuildSettings("Assets/Game/Scenes/LevelSelect.unity", 0);
            AddSceneToBuildSettings(MainMenuScenePath, 0);

            Debug.Log("[Chocoflan] Ajustes aplicados: " +
                      GridSettings.ReferenceWidthPx + "x" + GridSettings.ReferenceHeightPx +
                      ", PPU " + GridSettings.AssetsPixelsPerUnit +
                      ", tile " + GridSettings.TilePixels + " px (" + GridSettings.TileSize + " u)" +
                      ", grilla " + GridSettings.Columns + "x" + GridSettings.Rows +
                      ", orthographicSize " + GridSettings.CameraOrthographicSize +
                      ", AA 0, snap " + step + ".\n" +
                      "Recuerda poner la Game view en 16:9 (o la resolucion fija " +
                      GridSettings.ReferenceWidthPx + "x" + GridSettings.ReferenceHeightPx + ").");
        }

        static void AddSceneToBuildSettings(string path, int index)
        {
            if (!System.IO.File.Exists(path)) return;

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == path) return;

            scenes.Insert(Mathf.Clamp(index, 0, scenes.Count), new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Chocoflan/2b - Abrir menu de inicio", false, 12)]
        public static void OpenMainMenu()
        {
            if (!System.IO.File.Exists(MainMenuScenePath))
            {
                Debug.LogError("[Chocoflan] No se encontro " + MainMenuScenePath);
                return;
            }
            EditorSceneManager.OpenScene(MainMenuScenePath);
        }

        [MenuItem("Chocoflan/2 - Abrir escena Nivel 1", false, 11)]
        public static void OpenLevel01()
        {
            if (!System.IO.File.Exists(Level01ScenePath))
            {
                Debug.LogError("[Chocoflan] No se encontro " + Level01ScenePath);
                return;
            }
            EditorSceneManager.OpenScene(Level01ScenePath);
        }

        /// <summary>
        /// OPCIONAL y destructivo: deja los PNG con el import que pide el documento
        /// de arte: 240 PPU, filtro Bilinear y mipmaps.
        ///
        /// Ojo, es lo CONTRARIO de lo que se usaria en pixel art. El arte se dibuja
        /// a 240 px por unidad y en pantalla se ve a 120 px por unidad (al 50%), asi
        /// que necesita filtrado e mipmaps para no quedar con bordes dentados.
        ///
        /// Afecta tambien a SashasScene, por eso no se ejecuta solo.
        /// </summary>
        [MenuItem("Chocoflan/Opcional - Reimportar sprites a 240 PPU", false, 40)]
        public static void FixPlaceholderSpriteImport()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reimportar sprites",
                    "Se cambiara el import de los PNG de Assets/Game/Sprites a " +
                    GridSettings.AssetsPixelsPerUnit + " PPU, filtro Bilinear y mipmaps. " +
                    "Esto afecta tambien a SashasScene. Continuar?",
                    "Si, cambiar", "Cancelar"))
                return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Game/Sprites" });
            int changed = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = GridSettings.AssetsPixelsPerUnit;
                importer.SaveAndReimport();
                changed++;
            }
            Debug.Log("[Chocoflan] Sprites preparados: " + changed);
        }
    }
}
