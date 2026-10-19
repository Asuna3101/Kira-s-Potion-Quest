using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Los sprites del Nivel 1 dibujados por Melanie.
    /// Se asignan en el Inspector de LevelBuilder. Cualquiera que quede vacio se
    /// sustituye por el placeholder generado por codigo, asi que el nivel se puede
    /// jugar aunque falte arte.
    /// </summary>
    [System.Serializable]
    public class MelanieArt
    {
        [Header("Fondo")]
        public Sprite fondoCueva;       // Background/Background.JPG
        public Sprite tableroFondo;     // Background/Tablero_fondo.PNG
        public Sprite tableroMarco;     // Background/Tablero_encima.PNG  (va DELANTE)

        [Header("Plataformas")]
        public Sprite paredGrande;      // Ingredients/Obstacles/Pared_grande.PNG     (5 x 1 u)
        public Sprite paredMediana;     // Ingredients/Obstacles/Pared_mediana.PNG    (2 x 1 u)
        public Sprite paredPequena;     // Ingredients/Obstacles/Pared_pequeña.PNG    (1 x 1 u)

        [Header("Obstaculos")]
        public Sprite vela1;            // Ingredients/Obstacles/Vela_1.PNG
        public Sprite vela2;            // Ingredients/Obstacles/Vela_2.PNG

        [Header("Meta e ingredientes")]
        public Sprite caldero;          // Character/Caldero.PNG
        public Sprite huevo;            // Ingredients/Huevo.PNG

        [Header("Personaje")]
        public Sprite kira;             // Character/Kira.PNG
        public Sprite kiraVarita;       // Character/Kira_varita.PNG

        /// <summary>True si hay al menos una pieza de arte asignada.</summary>
        public bool HasAny =>
            tableroMarco != null || tableroFondo != null || paredGrande != null ||
            caldero != null || huevo != null;
    }

    /// <summary>
    /// Layout del Nivel 1 MEDIDO DEL PROPIO ARTE.
    ///
    /// Melanie entrega cada elemento dibujado sobre el lienzo completo de
    /// 3840 x 2160 px, en su sitio, con el resto transparente, y ya recortado en el
    /// Sprite Editor. A 240 PPU ese lienzo son exactamente los 16 x 9 u de pantalla,
    /// asi que basta con colocar cada recorte en el centro del rectangulo donde fue
    /// dibujado y la composicion queda idéntica al PNG.
    ///
    /// Las constantes de abajo son esos centros y tamanos, calculados del rect de
    /// cada sprite en su .meta:
    ///     centro.x = (rect.x + rect.ancho/2 - 1920) / 240
    ///     centro.y = (rect.y + rect.alto/2  - 1080) / 240
    ///
    /// Si Melanie reexporta y mueve algo, hay que recalcular aqui (o abrir el PNG y
    /// mirar el rect en el Sprite Editor). El arte manda: el collider se deriva de
    /// donde esta dibujada la pieza, no al reves.
    /// </summary>
    public static class Level01Art
    {
        public const string DisplayName = "Nivel 1 - La primera receta";

        // ---- fondo ---------------------------------------------------------

        public static readonly Vector2 FondoCentro = new Vector2(0f, 0f);
        public static readonly Vector2 FondoTamano = new Vector2(16f, 9f);

        public static readonly Vector2 TableroCentro = new Vector2(0.275f, -0.25f);
        public static readonly Vector2 TableroTamano = new Vector2(14.0583f, 7.925f);

        public static readonly Vector2 MarcoCentro = new Vector2(0.275f, -0.2458f);
        public static readonly Vector2 MarcoTamano = new Vector2(14.0583f, 7.9167f);

        /// <summary>
        /// Hueco interior del marco = zona jugable, medida del alfa de
        /// Tablero_encima.PNG. Aqui van los cuatro colliders de borde.
        /// </summary>
        public const float ZonaMinX = -5.875f;
        public const float ZonaMaxX = 6.371f;
        public const float ZonaMinY = -3.271f;
        public const float ZonaMaxY = 2.821f;

        // ---- plataformas (todas de 1 u de alto, como pide el documento) -----

        public static readonly Vector2 ParedGrandeCentro = new Vector2(-0.9417f, -0.8875f);
        public static readonly Vector2 ParedGrandeTamano = new Vector2(5.1333f, 1.0083f);

        public static readonly Vector2 ParedMedianaCentro = new Vector2(-2.7042f, 1.5021f);
        public static readonly Vector2 ParedMedianaTamano = new Vector2(2.0917f, 1.0125f);

        public static readonly Vector2 ParedPequenaCentro = new Vector2(0.2875f, 0.9583f);
        public static readonly Vector2 ParedPequenaTamano = new Vector2(1.0833f, 1.0167f);

        // ---- velas ----------------------------------------------------------
        //
        // Vela_1 y Vela_2 son el MISMO dibujo (identico pixel a pixel) colocado en
        // dos sitios: son dos velas del nivel, no dos frames de animacion.

        public static readonly Vector2 Vela1Centro = new Vector2(1.1875f, 0.3521f);
        public static readonly Vector2 Vela2Centro = new Vector2(-4.9083f, -2.5312f);
        public static readonly Vector2 VelaTamano = new Vector2(0.4833f, 1.5292f);

        /// <summary>
        /// Solo la llama quema. Medida por color sobre Vela_1: ocupa 0.346 x 0.583 u
        /// y su centro queda 0.469 u por encima del centro de la vela.
        /// </summary>
        public static readonly Vector2 LlamaTamano = new Vector2(0.346f, 0.583f);
        public const float LlamaOffsetY = 0.469f;

        /// <summary>La cera es solida: el ingrediente se puede apoyar al lado sin quemarse.</summary>
        public static readonly Vector2 CeraTamano = new Vector2(0.4f, 0.93f);
        public const float CeraOffsetY = -0.3f;

        // ---- caldero ---------------------------------------------------------

        public static readonly Vector2 CalderoCentro = new Vector2(4.8458f, -2.3937f);
        public static readonly Vector2 CalderoTamano = new Vector2(2.2083f, 1.8292f);

        /// <summary>
        /// La boca: 1.2 u de ancho (documento de arte). El ingrediente no entra,
        /// choca con este trigger y cuenta como entregado.
        ///
        /// El borde de la olla esta en y = -1.65 (medido del alfa: es la primera
        /// fila que aparece con los 2 u de ancho completos; por encima solo queda
        /// un pico suelto del dibujo). La boca se centra ahi, no en el borde de
        /// arriba del sprite, para que el huevo tenga que llegar a la olla.
        /// </summary>
        public const float BocaAncho = 1.2f;
        public const float BocaAlto = 0.35f;

        /// <summary>Desplazamiento de la boca respecto al centro del caldero.</summary>
        public const float BocaOffsetY = 0.619f;

        // ---- ingrediente -----------------------------------------------------

        /// <summary>Centro del huevo tal como esta dibujado (el sprite incluye el brillo).</summary>
        public static readonly Vector2 HuevoCentro = new Vector2(-1.1938f, 0.0958f);
        public static readonly Vector2 HuevoTamano = new Vector2(1.1375f, 1.375f);

        /// <summary>
        /// El collider NO es el sprite: el dibujo incluye el resplandor amarillo.
        /// El huevo de verdad son los 0.8 u que pide el documento de arte.
        /// </summary>
        public const float HuevoColliderDiametro = GridSettings.IngredientSize;

        // ---- Kira (decorativa) ------------------------------------------------

        public static readonly Vector2 KiraCentro = new Vector2(-6.2458f, 3.0396f);
        public static readonly Vector2 KiraTamano = new Vector2(2.6333f, 2.3375f);

        public static readonly Vector2 VaritaCentro = new Vector2(-5.7542f, 1.7437f);
        public static readonly Vector2 VaritaTamano = new Vector2(1.4917f, 1.1875f);

        // ---- orden de dibujo --------------------------------------------------

        public const int OrdenFondoCueva = -100;
        public const int OrdenTableroFondo = -50;
        public const int OrdenPlataformas = 10;
        public const int OrdenObstaculos = 20;
        public const int OrdenMarco = 100;   // el marco tapa los bordes del tablero
    }
}
