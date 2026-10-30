using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Preparar y compilar el juego para itch.io (WebGL, se juega en el navegador).
    ///
    /// El orden es: 1 (ajustes) -> 2 (texturas) -> 3 (compilar). El 2 es opcional
    /// pero marca la diferencia entre una build de 200 MB y una de 40 MB.
    /// </summary>
    public static class ItchBuild
    {
        const string CarpetaBuild = "Builds/WebGL";

        [MenuItem("Chocoflan/Itch/1 - Ajustar proyecto para WebGL", false, 100)]
        public static void Ajustar()
        {
            PlayerSettings.companyName = "Chocoflan";
            PlayerSettings.productName = "Kira's Potion Quest";
            PlayerSettings.runInBackground = true;

            // Tamano del lienzo en la pagina de itch. 960x540 es 16:9 y entra bien
            // en el ancho de una pagina; el boton de pantalla completa hace el resto.
            PlayerSettings.defaultWebScreenWidth = 960;
            PlayerSettings.defaultWebScreenHeight = 540;

            // CLAVE para itch: Gzip + Decompression Fallback.
            // itch sirve los archivos sin la cabecera Content-Encoding, asi que sin
            // el fallback el navegador no sabe descomprimirlos y el juego no carga.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            AssetDatabase.SaveAssets();

            Debug.Log("[Itch] Ajustes aplicados:\n" +
                      "  Lienzo 960x540\n" +
                      "  Compresion Gzip + Decompression Fallback (necesario en itch)\n" +
                      "  Run In Background activado\n" +
                      "Siguiente: Chocoflan > Itch > 2 (opcional) y luego 3.");
        }

        [MenuItem("Chocoflan/Itch/2 - Optimizar texturas para web (destructivo)", false, 101)]
        public static void OptimizarTexturas()
        {
            if (!EditorUtility.DisplayDialog("Optimizar texturas",
                    "Baja el Max Size de los dibujos grandes de 4096 a 2048.\n\n" +
                    "El arte se dibuja a 240 px por unidad y se ve a 120, o sea al 50%, " +
                    "asi que a 2048 no se pierde nitidez visible, pero la build pesa " +
                    "cerca de la cuarta parte.\n\nSe puede deshacer volviendo a poner 4096.",
                    "Optimizar", "Cancelar"))
                return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Game/Sprites" });
            int cambiados = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.maxTextureSize <= 2048) continue;

                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
                cambiados++;
            }
            Debug.Log("[Itch] Texturas optimizadas: " + cambiados +
                      ". Revisa que el arte se siga viendo bien antes de compilar.");
        }

        [MenuItem("Chocoflan/Itch/3 - Compilar WebGL", false, 102)]
        public static void Compilar()
        {
            List<string> escenas = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled && File.Exists(s.path)) escenas.Add(s.path);

            if (escenas.Count == 0)
            {
                Debug.LogError("[Itch] No hay escenas en Build Settings. " +
                               "Ejecuta Chocoflan > 1 - Aplicar ajustes de proyecto.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                bool cambiar = EditorUtility.DisplayDialog("Cambiar de plataforma",
                    "El proyecto no esta en WebGL. Hay que cambiar para compilar.\n\n" +
                    "Unity va a reimportar todo: la primera vez tarda bastante.",
                    "Cambiar a WebGL", "Cancelar");
                if (!cambiar) return;

                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                {
                    Debug.LogError("[Itch] No se pudo cambiar a WebGL. " +
                                   "Probablemente falte el modulo WebGL Build Support: " +
                                   "instalalo desde Unity Hub > Installs > los tres puntos > Add modules.");
                    return;
                }
            }

            Directory.CreateDirectory(CarpetaBuild);

            var opciones = new BuildPlayerOptions
            {
                scenes = escenas.ToArray(),
                locationPathName = CarpetaBuild,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log("[Itch] Compilando " + escenas.Count + " escenas en " + CarpetaBuild + "...");
            var informe = BuildPipeline.BuildPlayer(opciones);

            if (informe.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                float mb = informe.summary.totalSize / 1048576f;
                Debug.Log("[Itch] Build terminada: " + mb.ToString("0.0") + " MB en " + CarpetaBuild + "\n" +
                          "Comprime esa carpeta en .zip y subela a itch marcando " +
                          "\"This file will be played in the browser\".");
                EditorUtility.RevealInFinder(CarpetaBuild);
            }
            else
            {
                Debug.LogError("[Itch] La build fallo: " + informe.summary.result +
                               ". Revisa los errores de arriba en la consola.");
            }
        }

        [MenuItem("Chocoflan/Itch/4 - Abrir la carpeta de la build", false, 103)]
        public static void AbrirCarpeta()
        {
            if (!Directory.Exists(CarpetaBuild))
            {
                Debug.LogError("[Itch] Todavia no hay build. Ejecuta Chocoflan > Itch > 3.");
                return;
            }
            EditorUtility.RevealInFinder(CarpetaBuild);
        }
    }
}
