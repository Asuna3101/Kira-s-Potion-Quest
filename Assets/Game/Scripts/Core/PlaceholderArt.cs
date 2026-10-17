using System.Collections.Generic;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Tipos de sprite placeholder. El nombre de cada entrada es el nombre que debe
    /// tener el asset final para reemplazarlo sin tocar codigo:
    /// Assets/Game/Sprites/&lt;carpeta&gt;/&lt;Kind&gt;.png
    /// </summary>
    public enum ArtKind
    {
        Wall,
        Shelf,
        Spikes,
        Fire,
        AirCurrent,
        PortalA,
        PortalB,
        Button,
        Lever,
        Gate,
        Cauldron,
        IngredientEgg,
        IngredientMushroom,
        Kira,
        SelectionRing,
        AimDot,
        Background
    }

    /// <summary>
    /// Genera sprites placeholder por codigo (formas y colores legibles, filtro Point,
    /// 32 px por unidad) para no depender de arte final. Si Melanie entrega el PNG,
    /// basta asignarlo en el inspector de LevelBuilder: LevelBuilder usa el sprite
    /// asignado y escala el objeto al tamano del tile automaticamente.
    /// </summary>
    public static class PlaceholderArt
    {
        // Paleta "cute & cozy" del GDD.
        public static readonly Color WallDark = new Color32(0x4A, 0x3C, 0x5C, 0xFF);
        public static readonly Color WallLight = new Color32(0x63, 0x52, 0x7A, 0xFF);
        public static readonly Color ShelfDark = new Color32(0x6B, 0x4A, 0x33, 0xFF);
        public static readonly Color ShelfLight = new Color32(0x96, 0x6B, 0x4A, 0xFF);
        public static readonly Color Danger = new Color32(0xC9, 0x4F, 0x4F, 0xFF);
        public static readonly Color DangerDark = new Color32(0x6E, 0x27, 0x30, 0xFF);
        public static readonly Color FireA = new Color32(0xFF, 0xA8, 0x3C, 0xFF);
        public static readonly Color FireB = new Color32(0xFF, 0x5C, 0x2E, 0xFF);
        public static readonly Color Air = new Color32(0x9B, 0xE8, 0xFF, 0x55);
        public static readonly Color AirLine = new Color32(0xD8, 0xF8, 0xFF, 0xB0);
        public static readonly Color PortalACol = new Color32(0x4F, 0xC3, 0xE8, 0xFF);
        public static readonly Color PortalBCol = new Color32(0xE8, 0x5F, 0xC3, 0xFF);
        public static readonly Color ButtonIdle = new Color32(0xE8, 0xC2, 0x4F, 0xFF);
        public static readonly Color ButtonDone = new Color32(0x7A, 0xE0, 0x5A, 0xFF);
        public static readonly Color GateCol = new Color32(0x84, 0x63, 0xE0, 0xFF);
        public static readonly Color PotCol = new Color32(0x33, 0x33, 0x44, 0xFF);
        public static readonly Color BrewCol = new Color32(0x7A, 0xE0, 0x5A, 0xFF);
        public static readonly Color EggCol = new Color32(0xFF, 0xF2, 0xD8, 0xFF);
        public static readonly Color EggShade = new Color32(0xE0, 0xCE, 0xAC, 0xFF);
        public static readonly Color CapCol = new Color32(0xE0, 0x6A, 0x6A, 0xFF);
        public static readonly Color KiraCol = new Color32(0x3E, 0x33, 0x52, 0xFF);
        public static readonly Color KiraBelly = new Color32(0xE8, 0xDD, 0xF0, 0xFF);
        public static readonly Color Highlight = new Color32(0xFF, 0xD8, 0x6A, 0xFF);
        public static readonly Color BgCol = new Color32(0x24, 0x1E, 0x33, 0xFF);

        static readonly Dictionary<string, Sprite> s_Cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Devuelve (y cachea) un sprite placeholder del tipo pedido, dimensionado para
        /// cubrir widthTiles x heightTiles tiles de 16 px.
        /// </summary>
        public static Sprite Get(ArtKind kind, int widthTiles = 1, int heightTiles = 1)
        {
            widthTiles = Mathf.Max(1, widthTiles);
            heightTiles = Mathf.Max(1, heightTiles);

            string key = kind + "_" + widthTiles + "x" + heightTiles;
            if (s_Cache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            // OJO: se genera a PlaceholderPixelsPerTile (32), no a los 120 px de
            // arte reales que mide un tile a 240 PPU. A tamano real, el fondo de
            // pantalla completa seria una textura de 3840x2160 creada en runtime.
            // El tamano en el mundo sale igual porque el PPU del sprite se ajusta
            // en la misma proporcion (ver Sprite.Create mas abajo).
            int w = widthTiles * GridSettings.PlaceholderPixelsPerTile;
            int h = heightTiles * GridSettings.PlaceholderPixelsPerTile;
            Color[] px = new Color[w * h];

            switch (kind)
            {
                case ArtKind.Wall: PaintBricks(px, w, h, WallDark, WallLight); break;
                case ArtKind.Shelf: PaintPlanks(px, w, h, ShelfDark, ShelfLight); break;
                case ArtKind.Spikes: PaintSpikes(px, w, h); break;
                case ArtKind.Fire: PaintFire(px, w, h); break;
                case ArtKind.AirCurrent: PaintAir(px, w, h); break;
                case ArtKind.PortalA: PaintRing(px, w, h, PortalACol); break;
                case ArtKind.PortalB: PaintRing(px, w, h, PortalBCol); break;
                case ArtKind.Button: PaintButton(px, w, h); break;
                case ArtKind.Lever: PaintLever(px, w, h); break;
                case ArtKind.Gate: PaintGate(px, w, h); break;
                case ArtKind.Cauldron: PaintCauldron(px, w, h); break;
                case ArtKind.IngredientEgg: PaintEgg(px, w, h); break;
                case ArtKind.IngredientMushroom: PaintMushroom(px, w, h); break;
                case ArtKind.Kira: PaintKira(px, w, h); break;
                case ArtKind.SelectionRing: PaintRing(px, w, h, Highlight); break;
                case ArtKind.AimDot: PaintDisc(px, w, h, Highlight); break;
                case ArtKind.Background: PaintBackground(px, w, h); break;
                default: Fill(px, Color.magenta); break;
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "PH_" + key,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            tex.SetPixels(px);
            tex.Apply();

            var sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, w, h),
                new Vector2(0.5f, 0.5f),
                GridSettings.PlaceholderPixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "PH_" + key;
            sprite.hideFlags = HideFlags.DontSave;

            s_Cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Material unlit para que los placeholders se vean aunque la escena no
        /// tenga Light2D. Si el shader no se encuentra devuelve null (material por defecto).
        /// </summary>
        public static Material UnlitSpriteMaterial()
        {
            if (s_Unlit != null) return s_Unlit;
            Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) return null;
            s_Unlit = new Material(sh) { name = "PH_SpriteUnlit", hideFlags = HideFlags.DontSave };
            return s_Unlit;
        }

        static Material s_Unlit;

        // ---------------- pintores -----------------------------------------

        static void Fill(Color[] px, Color c)
        {
            for (int i = 0; i < px.Length; i++) px[i] = c;
        }

        static void Set(Color[] px, int w, int h, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            px[y * w + x] = c;
        }

        static void PaintBricks(Color[] px, int w, int h, Color dark, Color light)
        {
            Fill(px, light);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool mortarH = (y % 8) == 0;
                    int offset = ((y / 8) % 2 == 0) ? 0 : 8;
                    bool mortarV = ((x + offset) % 16) == 0;
                    if (mortarH || mortarV) Set(px, w, h, x, y, dark);
                }
            }
            Outline(px, w, h, dark);
        }

        static void PaintPlanks(Color[] px, int w, int h, Color dark, Color light)
        {
            Fill(px, light);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (y == 0 || y == h - 1 || (x % 16) == 0)
                        Set(px, w, h, x, y, dark);
            Outline(px, w, h, dark);
        }

        static void PaintSpikes(Color[] px, int w, int h)
        {
            // Base oscura abajo, triangulos hacia arriba cada 8 px.
            int baseH = Mathf.Max(2, h / 4);
            for (int y = 0; y < baseH; y++)
                for (int x = 0; x < w; x++)
                    Set(px, w, h, x, y, DangerDark);

            int spikeW = 8;
            int spikeH = h - baseH;
            for (int sx = 0; sx < w; sx += spikeW)
            {
                for (int y = 0; y < spikeH; y++)
                {
                    float t = 1f - (float)y / Mathf.Max(1, spikeH);  // 1 abajo, 0 arriba
                    int half = Mathf.Max(0, Mathf.RoundToInt(t * spikeW * 0.5f));
                    int cx = sx + spikeW / 2;
                    for (int x = cx - half; x <= cx + half; x++)
                        Set(px, w, h, x, baseH + y, Danger);
                }
            }
        }

        static void PaintFire(Color[] px, int w, int h)
        {
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / Mathf.Max(1, h - 1);
                int half = Mathf.RoundToInt(Mathf.Lerp(w * 0.45f, w * 0.12f, t));
                int cx = w / 2;
                Color c = Color.Lerp(FireB, FireA, t);
                for (int x = cx - half; x < cx + half; x++)
                    Set(px, w, h, x, y, c);
            }
        }

        static void PaintAir(Color[] px, int w, int h)
        {
            Fill(px, Air);
            // flechas hacia arriba cada 16 px
            for (int by = 0; by < h; by += 16)
            {
                for (int bx = 0; bx < w; bx += 16)
                {
                    int cx = bx + 8;
                    for (int y = 3; y < 13; y++) Set(px, w, h, cx, by + y, AirLine);
                    for (int k = 0; k < 4; k++)
                    {
                        Set(px, w, h, cx - k, by + 12 - k, AirLine);
                        Set(px, w, h, cx + k, by + 12 - k, AirLine);
                    }
                }
            }
        }

        static void PaintRing(Color[] px, int w, int h, Color c)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float rOut = Mathf.Min(w, h) * 0.48f;
            float rIn = rOut - Mathf.Max(2f, Mathf.Min(w, h) * 0.18f);
            Color inner = new Color(c.r, c.g, c.b, 0.35f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d <= rOut && d >= rIn) Set(px, w, h, x, y, c);
                    else if (d < rIn) Set(px, w, h, x, y, inner);
                }
        }

        static void PaintDisc(Color[] px, int w, int h, Color c)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float r = Mathf.Min(w, h) * 0.3f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d <= r) Set(px, w, h, x, y, c);
                }
        }

        static void PaintButton(Color[] px, int w, int h)
        {
            int padTop = Mathf.Max(2, h / 2);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (y >= padTop) Set(px, w, h, x, y, ButtonIdle);
                    else if (x > 1 && x < w - 2) Set(px, w, h, x, y, DangerDark);
                }
        }

        static void PaintLever(Color[] px, int w, int h)
        {
            int baseH = Mathf.Max(2, h / 5);
            for (int y = 0; y < baseH; y++)
                for (int x = 0; x < w; x++) Set(px, w, h, x, y, WallDark);
            int cx = w / 2;
            for (int y = baseH; y < h - 2; y++)
            {
                Set(px, w, h, cx, y, ShelfLight);
                Set(px, w, h, cx + 1, y, ShelfLight);
            }
            PaintDiscAt(px, w, h, cx, h - 3, 2.5f, ButtonIdle);
        }

        static void PaintDiscAt(Color[] px, int w, int h, int cx, int cy, float r, Color c)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d <= r) Set(px, w, h, x, y, c);
                }
        }

        static void PaintGate(Color[] px, int w, int h)
        {
            Fill(px, GateCol);
            Color dark = new Color(GateCol.r * 0.55f, GateCol.g * 0.55f, GateCol.b * 0.55f, 1f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if ((y % 8) == 0 || x == 0 || x == w - 1) Set(px, w, h, x, y, dark);
        }

        static void PaintCauldron(Color[] px, int w, int h)
        {
            int rimH = Mathf.Max(2, h / 8);
            int brewH = Mathf.Max(2, h / 6);
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / Mathf.Max(1, h - 1);
                // cuerpo: mas ancho arriba
                int half = Mathf.RoundToInt(Mathf.Lerp(w * 0.28f, w * 0.48f, Mathf.Sqrt(t)));
                int cx = w / 2;
                for (int x = cx - half; x < cx + half; x++)
                {
                    if (y >= h - rimH) Set(px, w, h, x, y, WallLight);
                    else if (y >= h - rimH - brewH) Set(px, w, h, x, y, BrewCol);
                    else Set(px, w, h, x, y, PotCol);
                }
            }
        }

        static void PaintEgg(Color[] px, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.45f;
            float rx = w * 0.32f, ry = h * 0.42f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    float d = dx * dx + dy * dy;
                    if (d <= 1f) Set(px, w, h, x, y, d > 0.55f && x < cx ? EggShade : EggCol);
                }
        }

        static void PaintMushroom(Color[] px, int w, int h)
        {
            int stemW = Mathf.Max(2, w / 4);
            int stemH = Mathf.Max(2, h / 2);
            int cx = w / 2;
            for (int y = 0; y < stemH; y++)
                for (int x = cx - stemW / 2; x < cx + stemW / 2; x++)
                    Set(px, w, h, x, y, EggCol);

            float capCy = stemH;
            float rx = w * 0.42f, ry = h * 0.34f;
            for (int y = stemH - 1; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - capCy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(px, w, h, x, y, CapCol);
                }
            // lunares
            PaintDiscAt(px, w, h, cx - w / 5, stemH + h / 8, Mathf.Max(1f, w * 0.07f), EggCol);
            PaintDiscAt(px, w, h, cx + w / 6, stemH + h / 5, Mathf.Max(1f, w * 0.06f), EggCol);
        }

        static void PaintKira(Color[] px, int w, int h)
        {
            float cx = (w - 1) * 0.5f;
            // cuerpo
            float bodyCy = h * 0.32f, brx = w * 0.34f, bry = h * 0.3f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / brx, dy = (y - bodyCy) / bry;
                    if (dx * dx + dy * dy <= 1f) Set(px, w, h, x, y, KiraCol);
                }
            // cabeza
            float headCy = h * 0.72f, hr = Mathf.Min(w, h) * 0.22f;
            PaintDiscAt(px, w, h, Mathf.RoundToInt(cx), Mathf.RoundToInt(headCy), hr, KiraCol);
            // orejas
            for (int k = 0; k < 5; k++)
            {
                for (int x = 0; x <= k; x++)
                {
                    Set(px, w, h, Mathf.RoundToInt(cx - hr * 0.8f) + x, Mathf.RoundToInt(headCy + hr * 0.6f) + (4 - k), KiraCol);
                    Set(px, w, h, Mathf.RoundToInt(cx + hr * 0.8f) - x, Mathf.RoundToInt(headCy + hr * 0.6f) + (4 - k), KiraCol);
                }
            }
            // ojos
            Set(px, w, h, Mathf.RoundToInt(cx - hr * 0.4f), Mathf.RoundToInt(headCy), KiraBelly);
            Set(px, w, h, Mathf.RoundToInt(cx + hr * 0.4f), Mathf.RoundToInt(headCy), KiraBelly);
            // pechera
            PaintDiscAt(px, w, h, Mathf.RoundToInt(cx), Mathf.RoundToInt(h * 0.26f), Mathf.Min(w, h) * 0.1f, KiraBelly);
        }

        static void PaintBackground(Color[] px, int w, int h)
        {
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / Mathf.Max(1, h - 1);
                Color c = Color.Lerp(BgCol, new Color32(0x3A, 0x2C, 0x4F, 0xFF), t);
                for (int x = 0; x < w; x++) Set(px, w, h, x, y, c);
            }
        }

        static void Outline(Color[] px, int w, int h, Color c)
        {
            for (int x = 0; x < w; x++) { Set(px, w, h, x, 0, c); Set(px, w, h, x, h - 1, c); }
            for (int y = 0; y < h; y++) { Set(px, w, h, 0, y, c); Set(px, w, h, w - 1, y, c); }
        }
    }
}
