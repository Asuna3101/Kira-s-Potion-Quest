using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Obstaculo que DESTRUYE o MODIFICA ingredientes (GDD: pinchos y fuego).
    /// Collider2D en modo trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        public enum HazardType { Spikes, Fire }

        public HazardType type = HazardType.Spikes;

        [Tooltip("Si es false solo rompe ingredientes fragiles (util para niveles faciles).")]
        public bool destroysEveryIngredient = true;

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var ing = other.GetComponentInParent<Ingredient>();
            if (ing == null || ing.IsBroken || ing.IsDelivered) return;
            if (!destroysEveryIngredient && !ing.fragile) return;

            // El ingrediente puede ser inmune a este obstaculo. Se marca con las
            // casillas "Resistente Al Fuego" / "Resistente A Los Pinchos" en su
            // propio Inspector, no aqui: asi un mismo fuego mata a unos y a otros no.
            if (!ing.LeAfecta(type)) return;

            bool pinchos = type == HazardType.Spikes;
            string reason = pinchos
                ? ing.ingredientName + " cayó en los pinchos"
                : ing.ingredientName + " se quemó en el fuego";

            // Los pinchos suenan a rotura; el fuego, a quemado.
            ing.Break(reason, pinchos ? Sonido.Romper : Sonido.Quemar);
        }
    }
}
