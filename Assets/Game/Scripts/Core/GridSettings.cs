using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Sistema de medidas del juego. TODO el nivel se describe en coordenadas de tile
    /// y esta clase es la unica que traduce a unidades de mundo.
    ///
    /// FUENTE: "KIRAS POTION CRAFT MEDIDAS" (documento de arte). Melanie dibuja
    /// contra estas medidas, asi que son las que mandan.
    ///
    /// Resolucion de juego       : 1920 x 1080 (16:9), estandar PC
    /// Camara ortografica        : orthographicSize = 4.5
    /// -> mundo visible          : 16 u de ancho x 9 u de alto  (x -8..8, y -4.5..4.5)
    /// Pixels Per Unit (import)  : 240
    /// -> el arte se dibuja a 240 px por unidad, en lienzo 3840 x 2160 con
    ///    cuadricula de 240 px, y en pantalla se ve al 50% (120 px de pantalla
    ///    por unidad). Eso es a proposito: da nitidez de sobra y margen para
    ///    pantallas mas grandes.
    ///
    /// Cuadricula de DISENO      : 16 x 9 celdas de 1 x 1 u  (un bloque = 1 celda)
    /// Cuadricula de LAYOUT      : 32 x 18 tiles de 0.5 x 0.5 u
    ///    El documento de arte dice "16x9 o el doble para detalles finos": el
    ///    doble es justamente esta grilla de 32x18. Sirve para colocar cosas de
    ///    media celda (pinchos de 1 x 0.5, boton de 1 x 0.3) sin romper la
    ///    cuadricula de 1 u con la que se dibuja el arte.
    ///
    /// Tile (0,0) es la esquina INFERIOR IZQUIERDA de la pantalla.
    /// </summary>
    public static class GridSettings
    {
        // ---- Pantalla y camara --------------------------------------------

        public const int ReferenceWidthPx = 1920;
        public const int ReferenceHeightPx = 1080;

        /// <summary>Pixels per unit con el que se importan los sprites (documento de arte).</summary>
        public const int AssetsPixelsPerUnit = 240;

        /// <summary>Ancho visible en unidades de mundo. 16</summary>
        public const float WorldWidth = 16f;

        /// <summary>Alto visible en unidades de mundo. 9</summary>
        public const float WorldHeight = 9f;

        /// <summary>orthographicSize del documento de arte. 4.5</summary>
        public const float CameraOrthographicSize = WorldHeight * 0.5f;

        /// <summary>Pixels de PANTALLA por unidad. 1920 / 16 = 120.</summary>
        public const float ScreenPixelsPerUnit = ReferenceWidthPx / WorldWidth;

        /// <summary>Pixels de ARTE por unidad (lo que dibuja Melanie). 240.</summary>
        public const int UnitArtPixels = AssetsPixelsPerUnit;

        // ---- Cuadriculas ---------------------------------------------------

        /// <summary>Celda de diseno: 1 x 1 u = 240 x 240 px de arte.</summary>
        public const float CellSize = 1f;

        /// <summary>Lado del tile de layout en unidades. Media celda.</summary>
        public const float TileSize = CellSize * 0.5f;

        /// <summary>Tiles por celda de diseno, en cada eje. 2</summary>
        public const int TilesPerCell = 2;

        public const int Columns = 32;  // 16 celdas x 2
        public const int Rows = 18;     // 9 celdas x 2

        /// <summary>Pixels de arte que mide un tile. 120</summary>
        public const int TilePixels = (int)(AssetsPixelsPerUnit * TileSize);

        /// <summary>Borde izquierdo del mundo visible. -8</summary>
        public const float WorldMinX = -WorldWidth * 0.5f;

        /// <summary>Borde inferior del mundo visible. -4.5</summary>
        public const float WorldMinY = -WorldHeight * 0.5f;

        // ---- Tamanos de los elementos (documento de arte) ------------------
        //
        // ELEMENTO                  UNIDADES      A 240 PPU
        // Bloque / plataforma       1 x 1         240 x 240
        // Ingrediente               0.7 - 0.9     ~215 x 215
        // Caldero                   2 x 1.6       480 x 385   (boca 1.2 u)
        // Pinchos (modulo)          1 x 0.5       240 x 120
        // Boton                     1 x 0.3       240 x 72
        // Palanca                   1 x 1         240 x 240
        // Portal                    1 x 1.5       240 x 360
        // Kira                      1.5 x 2       360 x 480
        // Corriente de aire         variable, animacion tileable (collider invisible)

        /// <summary>Diametro del ingrediente. Entre 0.7 y 0.9 para que pase por los huecos de 1 u.</summary>
        public const float IngredientSize = 0.8f;

        /// <summary>Tamano del sprite del caldero.</summary>
        public const float CauldronWidth = 2f;
        public const float CauldronHeight = 1.6f;

        /// <summary>Ancho de la boca del caldero: el trigger por el que entra el ingrediente.</summary>
        public const float CauldronMouthWidth = 1.2f;

        /// <summary>Alto del modulo de pinchos.</summary>
        public const float SpikeHeight = 0.5f;

        /// <summary>Alto del boton de presion.</summary>
        public const float ButtonHeight = 0.3f;

        public const float PortalWidth = 1f;
        public const float PortalHeight = 1.5f;

        public const float KiraWidth = 1.5f;
        public const float KiraHeight = 2f;

        /// <summary>Grosor del marco que rodea la pantalla, en unidades. 1 u = 2 tiles.</summary>
        public const float FrameThickness = 1f;

        // ---- Placeholders ---------------------------------------------------

        /// <summary>
        /// Los placeholders se generan por codigo a baja resolucion a proposito:
        /// a 240 PPU reales, el fondo de pantalla completa seria una textura de
        /// 3840 x 2160 generada en runtime. 32 px por tile alcanza y sobra para
        /// formas de color, y el tamano en el mundo sale igual porque el PPU del
        /// sprite se ajusta en la misma proporcion.
        /// </summary>
        public const int PlaceholderPixelsPerTile = 32;

        public const float PlaceholderPixelsPerUnit = PlaceholderPixelsPerTile / TileSize; // 64

        // ---- Conversiones ---------------------------------------------------

        /// <summary>Centro del tile (col, row) en unidades de mundo.</summary>
        public static Vector2 TileCenter(int col, int row)
        {
            return new Vector2(
                WorldMinX + (col + 0.5f) * TileSize,
                WorldMinY + (row + 0.5f) * TileSize);
        }

        /// <summary>Esquina inferior izquierda del tile (col, row).</summary>
        public static Vector2 TileCorner(int col, int row)
        {
            return new Vector2(
                WorldMinX + col * TileSize,
                WorldMinY + row * TileSize);
        }

        /// <summary>
        /// Centro de un rectangulo de tiles [col..col+width-1] x [row..row+height-1].
        /// </summary>
        public static Vector2 RectCenter(int col, int row, int width, int height)
        {
            return new Vector2(
                WorldMinX + (col + width * 0.5f) * TileSize,
                WorldMinY + (row + height * 0.5f) * TileSize);
        }

        /// <summary>Tamano en unidades de mundo de un rectangulo de tiles.</summary>
        public static Vector2 RectSize(int width, int height)
        {
            return new Vector2(width * TileSize, height * TileSize);
        }

        /// <summary>Convierte una posicion de mundo al tile que la contiene.</summary>
        public static Vector2Int WorldToTile(Vector2 world)
        {
            return new Vector2Int(
                Mathf.FloorToInt((world.x - WorldMinX) / TileSize),
                Mathf.FloorToInt((world.y - WorldMinY) / TileSize));
        }

        /// <summary>Celda de diseno (1 u) que contiene una posicion de mundo.</summary>
        public static Vector2Int WorldToCell(Vector2 world)
        {
            return new Vector2Int(
                Mathf.FloorToInt((world.x - WorldMinX) / CellSize),
                Mathf.FloorToInt((world.y - WorldMinY) / CellSize));
        }

        /// <summary>Redondea una posicion al pixel de arte (1/240 u).</summary>
        public static Vector3 SnapToPixel(Vector3 world)
        {
            float ppu = AssetsPixelsPerUnit;
            return new Vector3(
                Mathf.Round(world.x * ppu) / ppu,
                Mathf.Round(world.y * ppu) / ppu,
                world.z);
        }
    }
}
