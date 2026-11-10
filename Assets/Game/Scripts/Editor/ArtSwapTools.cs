using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest.EditorTools
{
    /// <summary>
    /// Pone el arte nuevo (pinchos, caldero verde y libro de la guia) sobre una
    /// escena ya horneada, sin tocar el layout: cada pieza conserva su ancho en
    /// pantalla y su posicion, y los colliders se recalculan para el dibujo nuevo.
    ///
    /// Archivos que busca (importalos tal cual en estas rutas):
    ///   Assets/Game/Sprites/Tablero/Pinchos.png
    ///   Assets/Game/Sprites/Character/Caldero_verde.png
    ///   Assets/Game/Sprites/UI/Guia_libro.png
    /// </summary>
    public static class ArtSwapTools
    {
        const string RutaPinchos = "Assets/Game/Sprites/Tablero/Pinchos.png";
        const string RutaCaldero = "Assets/Game/Sprites/Character/Caldero_verde.png";
        const string RutaGuia = "Assets/Game/Sprites/UI/Guia_libro.png";

        [MenuItem("Chocoflan/8 - Aplicar arte nuevo (pinchos, caldero y libro)", false, 17)]
        public static void AplicarArte()
        {
            Sprite pinchos = CargarSprite(RutaPinchos);
            Sprite caldero = CargarSprite(RutaCaldero);
            Sprite guia = CargarSprite(RutaGuia);

            int nPinchos = 0, nCalderos = 0, nHud = 0;

            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!sr.gameObject.scene.IsValid() || EditorUtility.IsPersistent(sr)) continue;
                string n = sr.gameObject.name;
                if (pinchos != null && n.StartsWith("Pincho") && sr.GetComponent<Hazard>() != null)
                {
                    PonerPinchos(sr, pinchos); nPinchos++;
                }
                else if (caldero != null && n == "Caldero" && sr.GetComponent<Cauldron>() != null)
                {
                    PonerCaldero(sr, caldero); nCalderos++;
                }
            }

            if (guia != null)
            {
                foreach (var hud in Object.FindObjectsByType<HudController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    Undo.RecordObject(hud, "Libro de guia");
                    hud.spriteGuia = guia;
                    EditorUtility.SetDirty(hud);
                    nHud++;
                }
            }

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i));

            string faltan = "";
            if (pinchos == null) faltan += "\n - falta " + RutaPinchos;
            if (caldero == null) faltan += "\n - falta " + RutaCaldero;
            if (guia == null) faltan += "\n - falta " + RutaGuia;

            Debug.Log("[Chocoflan] Arte nuevo aplicado en esta escena: " + nPinchos + " pinchos, " +
                      nCalderos + " caldero(s), " + nHud + " HUD(s) con el libro. Guarda con Cmd+S." + faltan);
        }

        static Sprite CargarSprite(string ruta)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(ruta) == null) return null;
            var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }

        // Punto del dibujo (pixeles, origen arriba-izquierda) -> unidades locales del objeto.
        static Vector2 Local(Sprite s, float px, float pyAbajo)
        {
            float ppu = s.pixelsPerUnit;
            return new Vector2((px - s.pivot.x) / ppu, (s.rect.height - pyAbajo - s.pivot.y) / ppu);
        }

        static float U(Sprite s, float px) { return px / s.pixelsPerUnit; }

        // ------------------------------------------------------------------ pinchos
        static void PonerPinchos(SpriteRenderer sr, Sprite s)
        {
            var t = sr.transform;
            Undo.RecordObject(sr, "Pinchos"); Undo.RecordObject(t, "Pinchos");
            Bounds antes = sr.bounds;
            float anchoMundo = Mathf.Max(0.05f, antes.size.x);
            bool girado = Mathf.Abs(Mathf.DeltaAngle(t.eulerAngles.z, 0f)) > 1f;

            sr.drawMode = SpriteDrawMode.Simple;
            sr.sprite = s;
            sr.color = Color.white;

            float nativo = s.rect.width / s.pixelsPerUnit;
            float k = anchoMundo / nativo;
            t.localScale = new Vector3(k, k, 1f);

            // Conserva el apoyo: la base queda donde estaba (o el centro, si esta girado).
            Bounds despues = sr.bounds;
            Vector3 p = t.position;
            if (girado) p += antes.center - despues.center;
            else p.y += antes.min.y - despues.min.y;
            p.x += antes.center.x - despues.center.x;
            t.position = p;

            // Hitbox: la zona de las puntas, un poco mas chica que el dibujo (el borde
            // grueso del trazo no cuenta). Valores en pixeles de la imagen de 538 x 203.
            var box = t.GetComponent<BoxCollider2D>();
            if (box == null) box = Undo.AddComponent<BoxCollider2D>(t.gameObject);
            Undo.RecordObject(box, "Pinchos");
            float w = s.rect.width, h = s.rect.height;
            float x0 = w * 0.07f, x1 = w * 0.93f;        // 13..502 de 538, con margen
            float y0 = h * 0.27f, y1 = h * 0.86f;        // desde cerca de la punta hasta la base
            Vector2 c = Local(s, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
            box.isTrigger = true;
            box.size = new Vector2(U(s, x1 - x0), U(s, y1 - y0));
            box.offset = c;
            EditorUtility.SetDirty(sr); EditorUtility.SetDirty(t); EditorUtility.SetDirty(box);
        }

        // ------------------------------------------------------------------ caldero
        static void PonerCaldero(SpriteRenderer sr, Sprite s)
        {
            var t = sr.transform;
            Undo.RecordObject(sr, "Caldero"); Undo.RecordObject(t, "Caldero");
            Bounds antes = sr.bounds;
            float anchoMundo = Mathf.Max(0.5f, antes.size.x);

            sr.drawMode = SpriteDrawMode.Simple;
            sr.sprite = s;
            sr.color = Color.white;

            float nativo = s.rect.width / s.pixelsPerUnit;
            float k = anchoMundo / nativo;
            t.localScale = new Vector3(k, k, 1f);

            // Apoyado donde estaba: misma base y mismo centro horizontal.
            Bounds despues = sr.bounds;
            Vector3 p = t.position;
            p.x += antes.center.x - despues.center.x;
            p.y += antes.min.y - despues.min.y;
            t.position = p;

            // Medidas del dibujo de 590 x 518 px (origen arriba-izquierda):
            // la superficie verde esta en x 120..460, y 82..138; la panza va de y 138 hasta 500.
            float W = s.rect.width, H = s.rect.height;
            float sx = W / 590f, fy = H / 518f;
            Vector2 bocaC = Local(s, 290f * sx, 110f * fy);
            Vector2 bocaSz = new Vector2(U(s, 300f * sx), U(s, 56f * fy));
            Vector2 cuerpoC = Local(s, 290f * sx, 319f * fy);
            Vector2 cuerpoSz = new Vector2(U(s, 380f * sx), U(s, 362f * fy));

            foreach (var b in t.GetComponents<BoxCollider2D>())
            {
                if (!b.isTrigger) continue;
                Undo.RecordObject(b, "Caldero");
                b.size = bocaSz; b.offset = bocaC; EditorUtility.SetDirty(b);
            }
            foreach (var cap in t.GetComponents<CapsuleCollider2D>())
            {
                if (cap.isTrigger) continue;
                Undo.RecordObject(cap, "Caldero");
                cap.direction = CapsuleDirection2D.Horizontal;
                cap.size = cuerpoSz; cap.offset = cuerpoC; EditorUtility.SetDirty(cap);
            }
            foreach (var b in t.GetComponents<BoxCollider2D>())
            {
                if (b.isTrigger) continue;
                Undo.RecordObject(b, "Caldero");
                b.size = cuerpoSz; b.offset = cuerpoC; EditorUtility.SetDirty(b);
            }

            // Efectos (humo, brillo, burbujas): a la altura de la superficie verde.
            foreach (var follow in Object.FindObjectsByType<FxFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (follow.target != t) continue;
                Undo.RecordObject(follow, "Caldero fx"); Undo.RecordObject(follow.transform, "Caldero fx");
                follow.localOffset = bocaC;
                follow.referenceScale = t.localScale;
                follow.baseScale = 1f;
                follow.transform.position = t.TransformPoint(new Vector3(bocaC.x, bocaC.y, 0f));
                var fx = follow.GetComponent<CauldronFx>();
                if (fx != null)
                {
                    Undo.RecordObject(fx, "Caldero fx");
                    fx.mouthWidth = U(s, 300f * sx) * k;
                    fx.mouthHeight = U(s, 56f * fy) * k;
                    EditorUtility.SetDirty(fx);
                }
                EditorUtility.SetDirty(follow);
            }
            EditorUtility.SetDirty(sr); EditorUtility.SetDirty(t);
        }

    }
}
