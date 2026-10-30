using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Corriente de aire (GDD): empuja al ingrediente mientras esta dentro de la zona.
    /// En el Nivel 1 es ascendente y sirve de "ascensor gratis" para no gastar mana.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AirCurrent : MonoBehaviour
    {
        [Tooltip("Direccion del empuje en unidades de mundo (se normaliza).")]
        public Vector2 direction = Vector2.up;

        [Tooltip("Aceleracion aplicada en u/s^2. Con gravityScale 0.6 la gravedad efectiva es ~5.9.")]
        public float acceleration = 12f;

        [Tooltip("Velocidad maxima que la corriente puede imprimir en su eje (u/s).")]
        public float maxSpeed = 3.2f;

        [Tooltip("Fuerza de la corriente sobre un ingrediente que esta levitando (1 = igual, 0.4 = el jugador la vence con facilidad). Evita que el huevo quede atrapado en la columna.")]
        [Range(0f, 1f)] public float levitatingFactor = 0.4f;

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var ing = other.GetComponentInParent<Ingredient>();
            if (ing == null || ing.IsBroken || ing.IsDelivered) return;

            Rigidbody2D rb = ing.Body;
            if (rb == null) return;

            Vector2 dir = direction.sqrMagnitude < 0.0001f ? Vector2.up : direction.normalized;
            float along = Vector2.Dot(rb.linearVelocity, dir);
            if (along < maxSpeed)
                rb.AddForce(dir * acceleration * (ing.IsLevitating ? levitatingFactor : 1f) * rb.mass,
                    ForceMode2D.Force);
        }
    }
}
