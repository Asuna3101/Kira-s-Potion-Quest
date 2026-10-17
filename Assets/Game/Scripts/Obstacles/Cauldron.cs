using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Caldero: la meta. Cuando TODOS los ingredientes entran, se gana el nivel.
    /// El trigger es mas pequeno que el sprite para que el ingrediente tenga que
    /// entrar por la boca y no valga rozarlo de lado.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Cauldron : MonoBehaviour, IClickHoldInteractable
    {
        public float holdSeconds = 0.35f;
        public float HoldSeconds => holdSeconds;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var ing = other.GetComponentInParent<Ingredient>();
            if (ing == null || ing.IsBroken || ing.IsDelivered) return;
            ing.Deliver(transform.position);
        }

        // "Interactuar: click y mantener" -> consultar la receta.
        public void OnHoldProgress(float normalized) { }

        public void OnHoldCompleted()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.ShowHint("Receta: faltan " + gm.RemainingIngredients + " de " + gm.TotalIngredients + " ingredientes.");
        }

        public void OnHoldCancelled() { }
    }
}
