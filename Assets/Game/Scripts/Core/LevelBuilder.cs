using System.Collections.Generic;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Sprites finales opcionales. Si Melanie entrega el PNG, se arrastra aqui y el
    /// builder lo usa en lugar del placeholder, escalando el objeto al tamano exacto
    /// que le toca en la grilla. No hay que tocar codigo.
    /// </summary>
    [System.Serializable]
    public class ArtOverrides
    {
        public Sprite wall;
        public Sprite shelf;
        public Sprite spikes;
        public Sprite fire;
        public Sprite airCurrent;
        public Sprite portalA;
        public Sprite portalB;
        public Sprite button;
        public Sprite lever;
        public Sprite gate;
        public Sprite cauldron;
        public Sprite ingredientEgg;
        public Sprite ingredientMushroom;
        public Sprite kira;
        public Sprite background;

        public Sprite For(ArtKind kind)
        {
            switch (kind)
            {
                case ArtKind.Wall: return wall;
                case ArtKind.Shelf: return shelf;
                case ArtKind.Spikes: return spikes;
                case ArtKind.Fire: return fire;
                case ArtKind.AirCurrent: return airCurrent;
                case ArtKind.PortalA: return portalA;
                case ArtKind.PortalB: return portalB;
                case ArtKind.Button: return button;
                case ArtKind.Lever: return lever;
                case ArtKind.Gate: return gate;
                case ArtKind.Cauldron: return cauldron;
                case ArtKind.IngredientEgg: return ingredientEgg;
                case ArtKind.IngredientMushroom: return ingredientMushroom;
                case ArtKind.Kira: return kira;
                case ArtKind.Background: return background;
                default: return null;
            }
        }
    }

    /// <summary>
    /// Construye el Nivel 1 a partir del mapa ASCII de Level01Data.
    ///
    /// Esto garantiza que el nivel que se juega y el layout documentado en
    /// Docs/Nivel01_Medidas.md sean literalmente lo mismo: si se edita el mapa,
    /// cambia el nivel.
    ///
    /// Los tiles contiguos del mismo tipo se fusionan en un unico GameObject con un
    /// solo BoxCollider2D (menos objetos, menos colliders, bordes sin costuras).
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        // Elegir un layout por escena. El valor 0 es el Nivel 1 original.
        public enum LevelLayout { Level01 = 0, Level02Enzo = 1 }
        [Header("Escena / Mapa")]
        public LevelLayout levelLayout = LevelLayout.Level01;

        public string CurrentDisplayName => levelLayout == LevelLayout.Level02Enzo
            ? Level02EnzoData.DisplayName : Level01Data.DisplayName;

        [Header("Construccion")]
        public bool buildOnAwake = true;
        public bool createBackground = true;

        [Header("Arte final (opcional, deja vacio para usar placeholders)")]
        public ArtOverrides art = new ArtOverrides();

        public Transform Root { get; private set; }
        public Cauldron CauldronRef { get; private set; }
        public Gate MainGate { get; private set; }
        public readonly List<Ingredient> Ingredients = new List<Ingredient>();

        PhysicsMaterial2D m_IngredientMaterial;

        /// <summary>Un bloque de tiles contiguos del mismo tipo, ya fusionado.</summary>
        public struct TileRect
        {
            public char Kind;
            public int Col, Row, Width, Height;
        }

        void Awake()
        {
            if (buildOnAwake) Rebuild();
        }

        public void Rebuild()
        {
            Clear();
            Build();
        }

        public void Clear()
        {
            Ingredients.Clear();
            CauldronRef = null;
            MainGate = null;

            // Borra cualquier raiz previa (por nombre, para que tambien funcione
            // cuando se reconstruye desde el Editor).
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == RootName)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }

        string RootName => levelLayout == LevelLayout.Level02Enzo ? "Nivel02_Enzo" : "Nivel01";

        // ------------------------------------------------------------------

        void Build()
        {
            char[,] grid = ParseMap(levelLayout == LevelLayout.Level02Enzo
                ? Level02EnzoData.MapTopDown : Level01Data.MapTopDown);

            var rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(transform, false);
            Root = rootGo.transform;

            Transform tablero = NewGroup("Tablero");
            Transform obstaculos = NewGroup("Obstaculos");
            Transform ingredientes = NewGroup("Ingredientes");
            Transform decoracion = NewGroup("Decoracion");

            if (createBackground) BuildBackground(NewGroup("Fondo"));

            m_IngredientMaterial = new PhysicsMaterial2D("IngredienteMat")
            {
                friction = 0.45f,
                bounciness = 0.05f
            };

            var plates = new List<PressurePlate>();
            var levers = new List<Lever>();
            var gates = new List<Gate>();
            Portal portalA = null, portalB = null;

            // 1) Rectangulos fusionados (estructura y zonas).
            foreach (TileRect r in ExtractRects(grid))
            {
                switch (r.Kind)
                {
                    case '#': BuildWall(tablero, r); break;
                    case '=': BuildShelf(tablero, r); break;
                    case '^': BuildHazard(obstaculos, r, Hazard.HazardType.Spikes); break;
                    case '*': BuildHazard(obstaculos, r, Hazard.HazardType.Fire); break;
                    case 'A': BuildAirCurrent(obstaculos, r); break;
                    case 'G': gates.Add(BuildGate(obstaculos, r)); break;
                    case 'b': plates.Add(BuildPressurePlate(obstaculos, r)); break;
                    case 'L': levers.Add(BuildLever(obstaculos, r)); break;
                    case 'C': BuildCauldron(obstaculos, r); break;
                }
            }

            // 2) Entidades de un solo tile.
            for (int row = 0; row < GridSettings.Rows; row++)
            {
                for (int col = 0; col < GridSettings.Columns; col++)
                {
                    switch (grid[col, row])
                    {
                        case '1': portalA = BuildPortal(obstaculos, col, row, true); break;
                        case '2': portalB = BuildPortal(obstaculos, col, row, false); break;
                        case 'E': Ingredients.Add(BuildIngredient(ingredientes, col, row, true)); break;
                        case 'M': Ingredients.Add(BuildIngredient(ingredientes, col, row, false)); break;
                        case 'K': BuildKira(decoracion, col, row); break;
                    }
                }
            }

            // 3) Cableado.
            if (portalA != null && portalB != null) portalA.Link(portalB);

            foreach (var plate in plates) plate.gates.AddRange(gates);
            foreach (var lever in levers) lever.gates.AddRange(gates);
            if (gates.Count > 0) MainGate = gates[0];

            var gm = GameManager.Instance;
            if (gm != null)
                foreach (var ing in Ingredients) gm.RegisterIngredient(ing);
        }

        Transform NewGroup(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            return go.transform;
        }

        // ---------------- parseo + fusion de rectangulos --------------------

        /// <summary>
        /// Convierte el mapa (indice 0 = fila superior) en grid[col, row] con row 0 abajo.
        /// </summary>
        public static char[,] ParseMap(string[] topDown)
        {
            var grid = new char[GridSettings.Columns, GridSettings.Rows];
            for (int row = 0; row < GridSettings.Rows; row++)
            {
                int lineIndex = topDown.Length - 1 - row;
                string line = (lineIndex >= 0 && lineIndex < topDown.Length) ? topDown[lineIndex] : "";
                for (int col = 0; col < GridSettings.Columns; col++)
                    grid[col, row] = col < line.Length ? line[col] : '.';
            }
            return grid;
        }

        public static bool IsMergeable(char c)
        {
            return c == '#' || c == '=' || c == '^' || c == '*' ||
                   c == 'A' || c == 'G' || c == 'b' || c == 'L' || c == 'C';
        }

        public static IEnumerable<TileRect> ExtractRects(char[,] grid)
        {
            int cols = GridSettings.Columns, rows = GridSettings.Rows;
            var used = new bool[cols, rows];
            var result = new List<TileRect>();

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    char c = grid[col, row];
                    if (used[col, row] || !IsMergeable(c)) continue;

                    int width = 1;
                    while (col + width < cols && grid[col + width, row] == c && !used[col + width, row])
                        width++;

                    int height = 1;
                    while (row + height < rows && RowMatches(grid, used, col, row + height, width, c))
                        height++;

                    for (int y = row; y < row + height; y++)
                        for (int x = col; x < col + width; x++)
                            used[x, y] = true;

                    result.Add(new TileRect { Kind = c, Col = col, Row = row, Width = width, Height = height });
                }
            }
            return result;
        }

        static bool RowMatches(char[,] grid, bool[,] used, int col, int row, int width, char c)
        {
            for (int x = col; x < col + width; x++)
                if (grid[x, row] != c || used[x, row]) return false;
            return true;
        }

        // ---------------- fabricas -----------------------------------------

        GameObject NewTileObject(Transform parent, string name, ArtKind kind,
                                 int col, int row, int width, int height,
                                 string sortingLayer, int sortingOrder)
        {
            return NewSizedObject(parent, name, kind,
                GridSettings.RectCenter(col, row, width, height),
                GridSettings.RectSize(width, height),
                sortingLayer, sortingOrder);
        }

        /// <summary>
        /// Igual que NewTileObject pero con un tamano exacto en unidades, para los
        /// elementos cuyo tamano lo fija el documento de arte y no la grilla
        /// (ingrediente 0.8, caldero 2 x 1.6, boton 1 x 0.3, portal 1 x 1.5, Kira 1.5 x 2).
        /// </summary>
        GameObject NewSizedObject(Transform parent, string name, ArtKind kind,
                                  Vector2 center, Vector2 target,
                                  string sortingLayer, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;

            var sr = go.AddComponent<SpriteRenderer>();
            Sprite custom = art != null ? art.For(kind) : null;

            if (custom != null)
            {
                sr.sprite = custom;
                Vector2 native = custom.bounds.size;
                go.transform.localScale = new Vector3(
                    native.x > 0.0001f ? target.x / native.x : 1f,
                    native.y > 0.0001f ? target.y / native.y : 1f,
                    1f);
            }
            else
            {
                // El placeholder se pide en tiles; se redondea hacia arriba para
                // que nunca quede en 0 en los objetos de menos de medio tile
                // (el boton mide 0.3 u = 0.6 tiles).
                int wTiles = Mathf.Max(1, Mathf.RoundToInt(target.x / GridSettings.TileSize));
                int hTiles = Mathf.Max(1, Mathf.RoundToInt(target.y / GridSettings.TileSize));
                sr.sprite = PlaceholderArt.Get(kind, wTiles, hTiles);

                // Y se estira al tamano real pedido, que puede no ser multiplo de tile.
                Vector2 phNative = sr.sprite.bounds.size;
                go.transform.localScale = new Vector3(
                    phNative.x > 0.0001f ? target.x / phNative.x : 1f,
                    phNative.y > 0.0001f ? target.y / phNative.y : 1f,
                    1f);
            }

            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            Material mat = PlaceholderArt.UnlitSpriteMaterial();
            if (mat != null) sr.sharedMaterial = mat;

            return go;
        }

        static BoxCollider2D AddBox(GameObject go, int width, int height, bool trigger, float shrink = 1f)
        {
            return AddBoxSized(go, GridSettings.RectSize(width, height) * shrink, trigger);
        }

        /// <summary>Collider de un tamano exacto en unidades de mundo.</summary>
        static BoxCollider2D AddBoxSized(GameObject go, Vector2 worldSize, bool trigger)
        {
            var box = go.AddComponent<BoxCollider2D>();
            // El collider se define en espacio local: hay que deshacer el escalado
            // que se aplica cuando se usa un sprite de arte final.
            Vector3 s = go.transform.localScale;
            box.size = new Vector2(
                Mathf.Approximately(s.x, 0f) ? worldSize.x : worldSize.x / s.x,
                Mathf.Approximately(s.y, 0f) ? worldSize.y : worldSize.y / s.y);
            box.isTrigger = trigger;
            return box;
        }

        void BuildWall(Transform parent, TileRect r)
        {
            var go = NewTileObject(parent, Nm("Muro", r), ArtKind.Wall, r.Col, r.Row, r.Width, r.Height, "Tablero", 0);
            AddBox(go, r.Width, r.Height, false);
        }

        void BuildShelf(Transform parent, TileRect r)
        {
            var go = NewTileObject(parent, Nm("Estante", r), ArtKind.Shelf, r.Col, r.Row, r.Width, r.Height, "Tablero", 0);
            AddBox(go, r.Width, r.Height, false);
        }

        void BuildHazard(Transform parent, TileRect r, Hazard.HazardType type)
        {
            ArtKind kind = type == Hazard.HazardType.Spikes ? ArtKind.Spikes : ArtKind.Fire;
            string baseName = type == Hazard.HazardType.Spikes ? "Pinchos" : "Fuego";
            var go = NewTileObject(parent, Nm(baseName, r), kind, r.Col, r.Row, r.Width, r.Height, "Tablero", 1);
            AddBox(go, r.Width, r.Height, true, 0.9f);
            var hazard = go.AddComponent<Hazard>();
            hazard.type = type;
            var anim = go.AddComponent<PlaceholderAnimator>();
            anim.mode = PlaceholderAnimator.Mode.Flicker;
            anim.speed = type == Hazard.HazardType.Fire ? 3f : 1f;
        }

        void BuildAirCurrent(Transform parent, TileRect r)
        {
            var go = NewTileObject(parent, Nm("CorrienteAire", r), ArtKind.AirCurrent, r.Col, r.Row, r.Width, r.Height, "Tablero", 2);
            AddBox(go, r.Width, r.Height, true);
            var air = go.AddComponent<AirCurrent>();
            air.direction = Vector2.up;
            var anim = go.AddComponent<PlaceholderAnimator>();
            anim.mode = PlaceholderAnimator.Mode.ScrollUp;
            anim.speed = 1.5f;
        }

        Gate BuildGate(Transform parent, TileRect r)
        {
            var go = NewTileObject(parent, Nm("Compuerta", r), ArtKind.Gate, r.Col, r.Row, r.Width, r.Height, "Tablero", 1);
            AddBox(go, r.Width, r.Height, false);
            var gate = go.AddComponent<Gate>();
            // Se hunde lo suficiente para desaparecer de la pantalla.
            gate.openOffsetY = -(r.Height * GridSettings.TileSize + GridSettings.TileSize);
            return gate;
        }

        PressurePlate BuildPressurePlate(Transform parent, TileRect r)
        {
            // Documento de arte: el boton mide 1 x 0.3 u. El tile que lo marca en
            // el mapa es de 0.5 u de alto, asi que se apoya en su base.
            float width = r.Width * GridSettings.TileSize;
            float height = GridSettings.ButtonHeight;
            Vector2 baseCenter = GridSettings.RectCenter(r.Col, r.Row, r.Width, r.Height);
            float bottom = baseCenter.y - r.Height * GridSettings.TileSize * 0.5f;
            var center = new Vector2(baseCenter.x, bottom + height * 0.5f);

            var go = NewSizedObject(parent, Nm("Boton", r), ArtKind.Button,
                center, new Vector2(width, height), "Tablero", 1);

            // El trigger es un poco mas alto que el sprite para que el ingrediente
            // lo pise aunque rebote un poco.
            AddBoxSized(go, new Vector2(width, height * 1.8f), true);
            return go.AddComponent<PressurePlate>();
        }

        Lever BuildLever(Transform parent, TileRect r)
        {
            var go = NewTileObject(parent, Nm("Palanca", r), ArtKind.Lever, r.Col, r.Row, r.Width, r.Height, "Tablero", 1);
            AddBox(go, r.Width, r.Height, true);
            return go.AddComponent<Lever>();
        }

        void BuildCauldron(Transform parent, TileRect r)
        {
            // Documento de arte: el caldero mide 2 x 1.6 u y la boca 1.2 u.
            // Se apoya en el suelo del rectangulo que lo marca en el mapa.
            Vector2 rectCenter = GridSettings.RectCenter(r.Col, r.Row, r.Width, r.Height);
            float bottom = rectCenter.y - r.Height * GridSettings.TileSize * 0.5f;
            var center = new Vector2(rectCenter.x, bottom + GridSettings.CauldronHeight * 0.5f);

            var go = NewSizedObject(parent, "Caldero", ArtKind.Cauldron, center,
                new Vector2(GridSettings.CauldronWidth, GridSettings.CauldronHeight), "Tablero", 1);

            // El trigger es solo la BOCA (1.2 u), arriba del caldero: el
            // ingrediente no "entra", choca con la boca y se cuenta como entregado.
            float mouthHeight = GridSettings.CauldronHeight * 0.35f;
            BoxCollider2D box = AddBoxSized(go, new Vector2(GridSettings.CauldronMouthWidth, mouthHeight), true);
            float localHalfHeight = GridSettings.CauldronHeight * 0.5f / Mathf.Max(0.0001f, go.transform.localScale.y);
            float localMouthHalf = mouthHeight * 0.5f / Mathf.Max(0.0001f, go.transform.localScale.y);
            box.offset = new Vector2(0f, localHalfHeight - localMouthHalf);
            CauldronRef = go.AddComponent<Cauldron>();
            var anim = go.AddComponent<PlaceholderAnimator>();
            anim.mode = PlaceholderAnimator.Mode.Pulse;
            anim.speed = 2f;
            anim.amplitude = 0.03f;
        }

        Portal BuildPortal(Transform parent, int col, int row, bool isA)
        {
            // Documento de arte: el portal mide 1 x 1.5 u, centrado en el tile marcado.
            ArtKind kind = isA ? ArtKind.PortalA : ArtKind.PortalB;
            var go = NewSizedObject(parent, isA ? "Portal_A" : "Portal_B", kind,
                GridSettings.TileCenter(col, row),
                new Vector2(GridSettings.PortalWidth, GridSettings.PortalHeight), "Tablero", 3);

            var circle = go.AddComponent<CircleCollider2D>();
            // Radio algo menor que el ancho del portal: hay que apuntarle.
            circle.radius = (GridSettings.PortalWidth * 0.4f) / Mathf.Max(0.0001f, go.transform.localScale.x);
            circle.isTrigger = true;
            var portal = go.AddComponent<Portal>();
            var anim = go.AddComponent<PlaceholderAnimator>();
            anim.mode = PlaceholderAnimator.Mode.Spin;
            anim.speed = 1.2f;
            return portal;
        }

        Ingredient BuildIngredient(Transform parent, int col, int row, bool isEgg)
        {
            // Documento de arte: el ingrediente mide entre 0.7 y 0.9 u para que
            // quepa por los huecos de la cuadricula de 1 u. Se usa 0.8.
            ArtKind kind = isEgg ? ArtKind.IngredientEgg : ArtKind.IngredientMushroom;
            string name = isEgg ? "Ingrediente_Huevo" : "Ingrediente_Hongo";
            float size = GridSettings.IngredientSize;

            // Se apoya en la BASE del tile que lo marca, no en su centro: el
            // ingrediente (0.8 u) es mas grande que el tile (0.5 u), asi que
            // centrarlo lo dejaria medio hundido en el estante.
            Vector2 tile = GridSettings.TileCenter(col, row);
            var center = new Vector2(tile.x, tile.y - GridSettings.TileSize * 0.5f + size * 0.5f);

            var go = NewSizedObject(parent, name, kind, center, new Vector2(size, size), "Ingredients", 0);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = (size * 0.5f) / Mathf.Max(0.0001f, go.transform.localScale.x);
            circle.sharedMaterial = m_IngredientMaterial;

            var ing = go.AddComponent<Ingredient>();
            ing.ingredientName = isEgg ? "El huevo" : "El hongo";
            ing.fragile = isEgg;                 // el huevo es el fragil del GDD
            // 10 u/s equivale a una caida libre de ~8.5 u: solo se rompe con caidas
            // casi de pantalla completa, para que el tutorial no castigue de gratis.
            ing.breakImpactSpeed = isEgg ? 10f : 999f;
            return ing;
        }

        void BuildKira(Transform parent, int col, int row)
        {
            // Kira es decorativa: 1.5 x 2 u (documento de arte), centrada en el
            // tile marcado. En el mockup va apoyada sobre el marco de la esquina
            // superior izquierda, sin piezas jugables debajo.
            var go = NewSizedObject(parent, "Kira", ArtKind.Kira,
                GridSettings.TileCenter(col, row),
                new Vector2(GridSettings.KiraWidth, GridSettings.KiraHeight), "Character", 0);
            var anim = go.AddComponent<PlaceholderAnimator>();
            anim.mode = PlaceholderAnimator.Mode.Bob;
            anim.speed = 1.6f;
            anim.amplitude = 0.03f;
        }

        void BuildBackground(Transform parent)
        {
            var go = NewTileObject(parent, "Fondo", ArtKind.Background, 0, 0,
                GridSettings.Columns, GridSettings.Rows, "BackGround", -100);
            go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, 1f);
        }

        static string Nm(string prefix, TileRect r)
        {
            return prefix + "_c" + r.Col + "r" + r.Row + "_" + r.Width + "x" + r.Height;
        }
    }
}
