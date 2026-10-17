using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Nivel 1 "La primera receta" descrito como un mapa ASCII de 32 x 18 tiles.
    /// Este mapa es la UNICA fuente de verdad del layout: el documento
    /// Docs/Nivel01_Medidas.md y LevelBuilder leen exactamente esto.
    ///
    /// Medidas (documento de arte "KIRAS POTION CRAFT MEDIDAS"):
    ///   1 tile  = 0.5 u = 120 px de arte
    ///   2 tiles = 1 celda de diseno = 1 u = 240 px  <- con lo que dibuja Melanie
    ///   32 x 18 tiles = 16 x 9 u = la pantalla completa a 1920x1080
    ///   El marco que rodea la pantalla mide 1 u (2 tiles) de grosor.
    ///
    /// Leyenda
    ///   #  Pared / columna (estatico, obstruye)
    ///   =  Plataforma / estante (estatico, obstruye)
    ///   ^  Pinchos (trigger, destruye el ingrediente)
    ///   *  Fuego (trigger, destruye el ingrediente)  -- no usado en el Nivel 1
    ///   A  Corriente de aire ascendente (trigger, empuja)
    ///   1  Portal A   (par con el portal B)
    ///   2  Portal B
    ///   b  Boton de presion (se activa al contacto con un ingrediente)
    ///   L  Palanca (click y mantener)  -- no usada en el Nivel 1
    ///   G  Compuerta (cerrada; se abre con el boton)
    ///   C  Caldero (meta)
    ///   E  Ingrediente Huevo  (FRAGIL)
    ///   M  Ingrediente Hongo  (RESISTENTE)
    ///   K  Kira (decorativa, sin colision; se dibuja 1.5 x 2 u sobre el marco)
    ///   .  Vacio
    ///
    /// El indice 0 del array es la fila SUPERIOR (row 17). LevelBuilder invierte
    /// el array para que row 0 sea el suelo.
    /// </summary>
    public static class Level01Data
    {
        public const string DisplayName = "Nivel 1 - La primera receta";

        /// <summary>Filas de arriba (row 17) hacia abajo (row 0). 18 filas x 32 columnas.</summary>
        public static readonly string[] MapTopDown =
        {
            "################################", // row 17  marco superior (1 u = 2 tiles)
            "################################", // row 16
            "##.K....................GG....##", // row 15  Kira (decorativa, sobre el marco)
            "##......................GG....##", // row 14
            "##......AAA.............GG....##", // row 13  <- hueco de 2 u sobre el muro central
            "##.E....AAA.............GG....##", // row 12  huevo sobre el estante alto
            "##======AAA......##.....GG....##", // row 11  estante alto (3 x 1 u)
            "##======AAA......##.2...GG....##", // row 10  portal B
            "##......AAA......##.....GG....##", // row  9
            "##......AAA.1....##.....GG....##", // row  8  portal A
            "##.M....AAA......##.....GG....##", // row  7  hongo sobre el estante bajo
            "##======AAA......##.....GG....##", // row  6  estante bajo (3 x 1 u)
            "##======AAA......##.....GGCCCC##", // row  5  caldero (2 x 1.6 u)
            "##......AAA......##.....GGCCCC##", // row  4
            "##......AAA......##.....GGCCCC##", // row  3
            "##......AAA^^^^..##^^^bbGGCCCC##", // row  2  pinchos / boton / compuerta
            "################################", // row  1  marco inferior (suelo)
            "################################", // row  0
        };

        // ---- Parametros de diseno del nivel -------------------------------

        /// <summary>Mana de levitacion maximo.</summary>
        public const float MaxMana = 100f;

        /// <summary>Mana consumido por segundo mientras se levita.</summary>
        public const float ManaDrainPerSecond = 16f;

        /// <summary>Mana recuperado por segundo cuando NO se levita.</summary>
        public const float ManaRegenPerSecond = 25f;
    }
}
