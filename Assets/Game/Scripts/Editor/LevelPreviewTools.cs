using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chocoflan.KiraPotionQuest.EditorTools
{
    /// <summary>
    /// Vuelve a hacer las fotos de los niveles que se ven en la pantalla de
    /// seleccion (Sprites/UI/Preview_Level01.png y Preview_Level02.png).
    ///
    /// Abre cada escena, le hace una foto a la camara tal cual esta, y
    /// sobrescribe el PNG. Asi la vista previa siempre coincide con el nivel de
    /// verdad y no hay que recortar capturas a mano.
    ///
    /// Se puede usar cada vez que cambie el arte o el diseno de un nivel.
    /// </summary>
    public static class LevelPreviewTools
    {
        const string CarpetaEscenas = "Assets/Game/Scenes/";
        const string CarpetaUI = "Assets/Game/Sprites/UI/";

        // 16:9, igual que el mundo del juego (16 x 9 unidades).
        const int Ancho = 960;
        const int Alto = 540;

        static readonly (string escena, string png)[] Niveles =
        {
            ("Level01", "Preview_Level01.png"),
            ("Level02", "Preview_Level02.png"),
        };

        [MenuItem("Chocoflan/9 - Actualizar vistas previas de los niveles", false, 18)]
        public static void Actualizar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string escenaOriginal = SceneManager.GetActiveScene().path;
            int hechas = 0;

            try
            {
                for (int i = 0; i < Niveles.Length; i++)
                {
                    var (escena, png) = Niveles[i];
                    string rutaEscena = CarpetaEscenas + escena + ".unity";

                    if (!File.Exists(rutaEscena))
                    {
                        Debug.LogWarning("[Vistas previas] No existe " + rutaEscena + ", se salta.");
                        continue;
                    }

                    EditorUtility.DisplayProgressBar("Vistas previas",
                        "Fotografiando " + escena + "...", i / (float)Niveles.Length);

                    EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);

                    Camera cam = BuscarCamara();
                    if (cam == null)
                    {
                        Debug.LogError("[Vistas previas] " + escena + " no tiene camara.");
                        continue;
                    }

                    byte[] datos = Fotografiar(cam);
                    if (datos == null) continue;

                    File.WriteAllBytes(CarpetaUI + png, datos);
                    hechas++;
                    Debug.Log("[Vistas previas] " + png + " actualizada desde " + escena + ".");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(escenaOriginal))
                    EditorSceneManager.OpenScene(escenaOriginal, OpenSceneMode.Single);
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Vistas previas",
                hechas + " de " + Niveles.Length + " vistas previas actualizadas.\n\n" +
                "Ya se ven en la escena LevelSelect.",
                "Listo");
        }

        static Camera BuscarCamara()
        {
            if (Camera.main != null) return Camera.main;

            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c.enabled && c.targetTexture == null) return c;

            return null;
        }

        /// <summary>Renderiza la camara a una textura y devuelve el PNG.</summary>
        static byte[] Fotografiar(Camera cam)
        {
            var rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };

            RenderTexture anterior = RenderTexture.active;
            RenderTexture camAnterior = cam.targetTexture;
            Texture2D tex = null;

            try
            {
                // Render a la textura. Funciona igual con URP que con el pipeline
                // clasico mientras se haga desde el editor y fuera de Play.
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, Ancho, Alto), 0, 0);
                tex.Apply();

                return tex.EncodeToPNG();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Vistas previas] No se pudo fotografiar: " + e.Message);
                return null;
            }
            finally
            {
                RenderTexture.active = anterior;
                cam.targetTexture = camAnterior;
                if (tex != null) Object.DestroyImmediate(tex);
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
    }
}
