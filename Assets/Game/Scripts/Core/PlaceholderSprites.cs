using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Los sprites PLACEHOLDER como assets de verdad
    /// (Assets/Game/Sprites/PlaceHolders/Generados/PH_*.png).
    ///
    /// Antes los placeholders se dibujaban por codigo en tiempo de ejecucion. Eso
    /// tenia dos problemas para el equipo:
    ///   - no se podian ver ni tocar en el Editor, solo existian en Play
    ///   - reemplazar uno por el arte final obligaba a tocar codigo
    ///
    /// Ahora cada uno es un PNG con su hueco en el Inspector. Para cambiar el arte
    /// SOLO hay que arrastrar el sprite nuevo al hueco correspondiente: ni el
    /// codigo ni la escena se tocan.
    ///
    /// Todos los PNG estan dibujados a 240 PPU con las medidas exactas del
    /// documento de arte (bloque 1x1, pinchos 1x0.5, boton 1x0.3, caldero 2x1.6,
    /// ingrediente 0.8, portal 1x1.5, Kira 1.5x2), asi que el reemplazo entra
    /// con el mismo tamano.
    /// </summary>
    [System.Serializable]
    public class PlaceholderSprites
    {
        [Header("Estructura")]
        public Sprite wall;
        public Sprite shelf;
        public Sprite gate;

        [Header("Obstaculos")]
        public Sprite spikes;
        public Sprite fire;
        public Sprite airCurrent;
        public Sprite portalA;
        public Sprite portalB;
        public Sprite button;
        public Sprite lever;

        [Header("Meta e ingredientes")]
        public Sprite cauldron;
        public Sprite ingredientEgg;
        public Sprite ingredientMushroom;

        [Header("Personaje y FX")]
        public Sprite kira;
        public Sprite selectionRing;
        public Sprite aimDot;
        public Sprite background;

        /// <summary>Sprite asignado para ese tipo, o null si el hueco esta vacio.</summary>
        public Sprite For(ArtKind kind)
        {
            switch (kind)
            {
                case ArtKind.Wall: return wall;
                case ArtKind.Shelf: return shelf;
                case ArtKind.Gate: return gate;
                case ArtKind.Spikes: return spikes;
                case ArtKind.Fire: return fire;
                case ArtKind.AirCurrent: return airCurrent;
                case ArtKind.PortalA: return portalA;
                case ArtKind.PortalB: return portalB;
                case ArtKind.Button: return button;
                case ArtKind.Lever: return lever;
                case ArtKind.Cauldron: return cauldron;
                case ArtKind.IngredientEgg: return ingredientEgg;
                case ArtKind.IngredientMushroom: return ingredientMushroom;
                case ArtKind.Kira: return kira;
                case ArtKind.SelectionRing: return selectionRing;
                case ArtKind.AimDot: return aimDot;
                case ArtKind.Background: return background;
                default: return null;
            }
        }
    }

    /// <summary>
    /// Punto unico desde el que el resto del codigo pide un sprite placeholder.
    /// Lo publica LevelBuilder al construir el nivel, para que Ingredient y
    /// ThrowController usen los mismos assets sin tener que arrastrarlos otra vez
    /// en cada objeto.
    /// </summary>
    public static class PlaceholderLibrary
    {
        public static PlaceholderSprites Current { get; set; }

        /// <summary>
        /// Sprite del tipo pedido. Orden: asset asignado en el Inspector y, solo si
        /// ese hueco esta vacio, el dibujo generado por codigo como ultimo recurso
        /// (para que nunca quede un objeto invisible).
        /// </summary>
        public static Sprite Get(ArtKind kind, int widthTiles = 1, int heightTiles = 1)
        {
            Sprite s = Current != null ? Current.For(kind) : null;
            return s != null ? s : PlaceholderArt.Get(kind, widthTiles, heightTiles);
        }
    }
}
