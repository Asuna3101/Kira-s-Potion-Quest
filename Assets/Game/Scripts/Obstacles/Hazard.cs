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

            string reason = type == HazardType.Spikes
                ? ing.ingredientName + " cayo en los pinchos"
                : ing.ingredientName + " se quemo en el fuego";
            ing.Break(reason);
        }
    }
}
