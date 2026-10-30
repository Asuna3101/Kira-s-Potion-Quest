namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Nivel 2 - El jardin del viento (Enzo).
    /// Mapa independiente 32 x 18, filas de arriba hacia abajo.
    /// Leyenda: # muro, = estante, ^ pinchos, * fuego, A aire,
    /// 1/2 portales, b boton, L palanca, G compuerta, C caldero,
    /// E huevo fragil, M hongo resistente, K Kira, . vacio.
    /// </summary>
    public static class Level02EnzoData
    {
        public const string DisplayName = "El jardín del viento";
        public static readonly string[] MapTopDown =
        {
            "################################", // fila 17
            "################################", // fila 16
            "##.K..........................##", // fila 15
            "##............................##", // fila 14
            "##...........====.....L.GG....##", // fila 13
            "##......................GG....##", // fila 12
            "##...E...............2..GGCCCC##", // fila 11
            "##.======.AA....##......GGCCCC##", // fila 10
            "##........AA.1..##......GGCCCC##", // fila 9
            "##........AA............GGCCCC##", // fila 8
            "##........AA....##..M...GG====##", // fila 7
            "##...M....AA....##=====.GG....##", // fila 6
            "##.=====..AA....##............##", // fila 5
            "##........AA..................##", // fila 4
            "##........AA..................##", // fila 3
            "##...........^^^..***bb.......##", // fila 2
            "################################", // fila 1
            "################################", // fila 0
        };
    }
}
