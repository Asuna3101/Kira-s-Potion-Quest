using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest.EditorTools
{
    /// <summary>
    /// Herramientas de autor del Nivel 1.
    ///
    /// "Hornear" = ejecutar LevelBuilder en modo edicion para que los GameObjects
    /// existan de verdad en la escena y se puedan tocar a mano en el Inspector.
    /// En Play el nivel se construye solo, asi que hornear es opcional.
    /// </summary>
    public static class LevelLayoutTools
    {
        [MenuItem("Chocoflan/3 - Hornear Nivel 1 en la escena", false, 12)]
        public static void BakeLevel()
        {
            LevelBuilder builder = FindBuilder();
            if (builder == null) return;

            builder.Rebuild();

            Transform root = builder.transform.Find(
                builder.levelLayout == LevelBuilder.LevelLayout.Level02Enzo ? "Nivel02_Enzo" : "Nivel01");
            if (root == null)
            {
                Debug.LogError("[Chocoflan] El builder no genero ninguna raiz de nivel.");
                return;
            }

            // 1) Permanente: la vista previa automatica marca los objetos como
            //    temporales (no se guardan). Al hornear a mano los queremos en el
            //    .unity de verdad.
            MarcarPermanente(root);

            // 2) Y que NO se reconstruya al entrar en Play: si no, Awake llamaria a
            //    Rebuild(), borraria esta jerarquia y con ella cualquier cosa que el
            //    equipo haya movido a mano en el Inspector.
            builder.buildOnAwake = false;

            EditorUtility.SetDirty(builder);
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);

            Debug.Log("[Chocoflan] Nivel horneado como objetos permanentes (" +
                      ContarHijos(root) + " objetos, " + builder.Ingredients.Count +
                      " ingredientes). 'Build On Awake' quedo DESACTIVADO para que lo que " +
                      "edites a mano no se pierda al darle Play. Guarda la escena con Cmd+S.",
                      builder);
        }

        static void MarcarPermanente(Transform t)
        {
            t.gameObject.hideFlags = HideFlags.None;
            for (int i = 0; i < t.childCount; i++) MarcarPermanente(t.GetChild(i));
        }

        static int ContarHijos(Transform t)
        {
            int n = 1;
            for (int i = 0; i < t.childCount; i++) n += ContarHijos(t.GetChild(i));
            return n;
        }

        [MenuItem("Chocoflan/4 - Limpiar Nivel 1 de la escena", false, 13)]
        public static void ClearLevel()
        {
            LevelBuilder builder = FindBuilder();
            if (builder == null) return;

            builder.Clear();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
            Debug.Log("[Chocoflan] Nivel 1 limpiado de la escena.");
        }

        [MenuItem("Chocoflan/5 - Exportar layout a Docs/Nivel01_Layout.md", false, 14)]
        public static void ExportLayout()
        {
            string markdown = BuildLayoutMarkdown();
            string path = System.IO.Path.Combine(
                System.IO.Directory.GetParent(Application.dataPath).FullName,
                "Docs", "Nivel01_Layout.md");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path, markdown);
            Debug.Log("[Chocoflan] Layout exportado a " + path);
        }

        static LevelBuilder FindBuilder()
        {
            var builder = Object.FindFirstObjectByType<LevelBuilder>();
            if (builder == null)
                Debug.LogError("[Chocoflan] No hay LevelBuilder en la escena abierta. " +
                               "Abre Assets/Game/Scenes/Level01.unity.");
            return builder;
        }

        // ------------------------------------------------------------------

        static readonly Dictionary<char, string> s_Names = new Dictionary<char, string>
        {
            { '#', "Muro / borde" },
            { '=', "Estante" },
            { '^', "Pinchos" },
            { '*', "Fuego" },
            { 'A', "Corriente de aire" },
            { 'G', "Compuerta" },
            { 'b', "Boton de presion" },
            { 'L', "Palanca" },
            { 'C', "Caldero" },
            { '1', "Portal A" },
            { '2', "Portal B" },
            { 'E', "Ingrediente Huevo (fragil)" },
            { 'M', "Ingrediente Hongo (resistente)" },
            { 'K', "Kira (decorativa)" },
        };

        public static string BuildLayoutMarkdown()
        {
            var sb = new StringBuilder();
            char[,] grid = LevelBuilder.ParseMap(Level01Data.MapTopDown);

            sb.AppendLine("# " + Level01Data.DisplayName + " - layout en coordenadas de tile");
            sb.AppendLine();
            sb.AppendLine("Generado automaticamente desde `Level01Data.MapTopDown`");
            sb.AppendLine("(menu `Chocoflan > 5 - Exportar layout`). No editar a mano.");
            sb.AppendLine();
            sb.AppendLine("## Medidas");
            sb.AppendLine();
            sb.AppendLine("| Dato | Valor |");
            sb.AppendLine("|---|---|");
            sb.AppendLine("| Resolucion de referencia | " + GridSettings.ReferenceWidthPx + " x " + GridSettings.ReferenceHeightPx + " px |");
            sb.AppendLine("| Pixels Per Unit | " + GridSettings.AssetsPixelsPerUnit + " |");
            sb.AppendLine("| Tile | " + GridSettings.TilePixels + " px = " + GridSettings.TileSize + " u |");
            sb.AppendLine("| Grilla | " + GridSettings.Columns + " x " + GridSettings.Rows + " tiles |");
            sb.AppendLine("| Mundo visible | x " + GridSettings.WorldMinX + " .. " + (-GridSettings.WorldMinX) +
                          " , y " + GridSettings.WorldMinY + " .. " + (-GridSettings.WorldMinY) + " |");
            sb.AppendLine("| orthographicSize | " + GridSettings.CameraOrthographicSize + " |");
            sb.AppendLine();
            sb.AppendLine("Centro de un tile: `x = " + GridSettings.WorldMinX + " + (col + 0.5) * " + GridSettings.TileSize +
                          "` , `y = " + GridSettings.WorldMinY + " + (row + 0.5) * " + GridSettings.TileSize + "`");
            sb.AppendLine();

            // Mapa ASCII con numeracion de columnas.
            sb.AppendLine("## Mapa (row 19 arriba, row 0 abajo)");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine("     " + ColumnRuler());
            for (int row = GridSettings.Rows - 1; row >= 0; row--)
            {
                var line = new StringBuilder();
                for (int col = 0; col < GridSettings.Columns; col++) line.Append(grid[col, row]);
                sb.AppendLine(row.ToString().PadLeft(3) + "  " + line);
            }
            sb.AppendLine("```");
            sb.AppendLine();

            // Tabla de objetos.
            sb.AppendLine("## Objetos");
            sb.AppendLine();
            sb.AppendLine("| Objeto | Tiles (col, row) | Tamano (tiles) | Centro (u) | Tamano (u) |");
            sb.AppendLine("|---|---|---|---|---|");

            foreach (LevelBuilder.TileRect r in LevelBuilder.ExtractRects(grid))
            {
                Vector2 c = GridSettings.RectCenter(r.Col, r.Row, r.Width, r.Height);
                Vector2 s = GridSettings.RectSize(r.Width, r.Height);
                sb.AppendLine("| " + Label(r.Kind) +
                              " | (" + r.Col + ", " + r.Row + ")" +
                              " | " + r.Width + " x " + r.Height +
                              " | (" + c.x + ", " + c.y + ")" +
                              " | " + s.x + " x " + s.y + " |");
            }

            for (int row = 0; row < GridSettings.Rows; row++)
            {
                for (int col = 0; col < GridSettings.Columns; col++)
                {
                    char ch = grid[col, row];
                    if (LevelBuilder.IsMergeable(ch) || ch == '.') continue;
                    Vector2 c = GridSettings.TileCenter(col, row);
                    sb.AppendLine("| " + Label(ch) +
                                  " | (" + col + ", " + row + ")" +
                                  " | 1 x 1" +
                                  " | (" + c.x + ", " + c.y + ")" +
                                  " | " + GridSettings.TileSize + " x " + GridSettings.TileSize + " |");
                }
            }

            return sb.ToString();
        }

        static string ColumnRuler()
        {
            var sb = new StringBuilder();
            for (int col = 0; col < GridSettings.Columns; col++)
                sb.Append(col % 10 == 0 ? (col / 10).ToString() : ".");
            sb.AppendLine();
            sb.Append("     ");
            for (int col = 0; col < GridSettings.Columns; col++)
                sb.Append(col % 10);
            return sb.ToString();
        }

        static string Label(char c)
        {
            return s_Names.TryGetValue(c, out string name) ? name + " `" + c + "`" : "`" + c + "`";
        }
    }
}
