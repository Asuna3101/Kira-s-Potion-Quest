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

        Collider2D m_Col;
        ContactFilter2D m_Filtro;
        static readonly Collider2D[] s_Dentro = new Collider2D[16];

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        void Awake()
        {
            m_Col = GetComponent<Collider2D>();
            if (m_Col != null) m_Col.isTrigger = true;

            m_Filtro = new ContactFilter2D();
            m_Filtro.NoFilter();
            m_Filtro.useTriggers = false;   // solo los colliders solidos del ingrediente
        }

        /// <summary>
        /// Se consulta quien esta dentro en cada paso de fisica, en vez de esperar
        /// a OnTriggerStay2D.
        ///
        /// POR QUE: Unity DUERME los Rigidbody que llevan un rato quietos, y a un
        /// cuerpo dormido no le llegan los avisos de trigger. Un ingrediente
        /// posado dentro de la columna no recibia ningun empuje y la corriente
        /// parecia rota: solo funcionaba si el ingrediente ya venia en movimiento.
        /// Preguntando nosotros podemos despertarlo.
        /// </summary>
        void FixedUpdate()
        {
            if (m_Col == null || !m_Col.enabled) return;

            var gm = GameManager.Instance;
            if (gm != null && gm.State != LevelState.Playing) return;

            Vector2 dir = direction.sqrMagnitude < 0.0001f ? Vector2.up : direction.normalized;

            // La zona es un rectangulo recto (girarlo 180 no cambia su caja), asi
            // que basta con consultar los limites del propio collider.
            Bounds b = m_Col.bounds;
            int n = Physics2D.OverlapBox(b.center, b.size, 0f, m_Filtro, s_Dentro);

            for (int i = 0; i < n; i++)
            {
                var otro = s_Dentro[i];
                if (otro == null) continue;

                var ing = otro.GetComponentInParent<Ingredient>();
                if (ing == null || ing.IsBroken || ing.IsDelivered) continue;

                Rigidbody2D rb = ing.Body;
                if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) continue;

                if (rb.IsSleeping()) rb.WakeUp();

                float along = Vector2.Dot(rb.linearVelocity, dir);
                if (along >= maxSpeed) continue;

                rb.AddForce(dir * acceleration * (ing.IsLevitating ? levitatingFactor : 1f) * rb.mass,
                    ForceMode2D.Force);
            }
        }
    }
}
