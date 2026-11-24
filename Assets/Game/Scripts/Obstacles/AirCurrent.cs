
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    [RequireComponent(typeof(Collider2D))]
    public class AirCurrent : MonoBehaviour
    {
        [Header("Fisica de la corriente")]

        [Tooltip("Direccion del empuje en unidades de mundo.")]
        public Vector2 direction = Vector2.up;

        [Tooltip("Aceleracion aplicada en u/s^2.")]
        public float acceleration = 12f;

        [Tooltip("Velocidad maxima sobre el eje de la corriente.")]
        public float maxSpeed = 3.2f;

        [Range(0f, 1f)]
        public float levitatingFactor = 0.4f;


        [Header("Viento intermitente")]

        [Tooltip("Si esta desactivado, el viento es permanente.")]
        public bool vientoIntermitente = false;

        [Tooltip("Segundos durante los que el viento empuja.")]
        [Min(0.01f)]
        public float duracionActivo = 4f;

        [Tooltip("Segundos durante los que no hay viento.")]
        [Min(0.01f)]
        public float duracionInactivo = 2f;

        [Tooltip("La corriente comienza sin viento.")]
        public bool comenzarApagado = false;

        [Tooltip("Comienza en un punto aleatorio del ciclo.")]
        public bool cicloAleatorio = false;

        [Tooltip("Desfase inicial del ciclo en segundos.")]
        [Min(0f)]
        public float desfaseCiclo = 0f;


        [Header("Estado actual")]

        [SerializeField]
        private bool vientoActivo = true;

        [SerializeField]
        private float tiempoRestante;

        public bool VientoActivo => vientoActivo;
        public float TiempoRestante => tiempoRestante;


        private float m_Fase;
        private Collider2D m_Col;
        private ContactFilter2D m_Filtro;

        static readonly Collider2D[] s_Dentro =
            new Collider2D[16];


        void Reset()
        {
            var col = GetComponent<Collider2D>();

            if (col != null)
                col.isTrigger = true;
        }


        void Awake()
        {
            m_Col = GetComponent<Collider2D>();

            if (m_Col != null)
                m_Col.isTrigger = true;

            m_Filtro = new ContactFilter2D();
            m_Filtro.NoFilter();
            m_Filtro.useTriggers = false;

            InicializarCiclo();
        }


        void InicializarCiclo()
        {
            float activo = Mathf.Max(
                0.01f, duracionActivo
            );

            float inactivo = Mathf.Max(
                0.01f, duracionInactivo
            );

            float cicloTotal = activo + inactivo;

            m_Fase = comenzarApagado ? activo : 0f;

            m_Fase += desfaseCiclo;

            if (cicloAleatorio)
            {
                m_Fase = Random.Range(
                    0f, cicloTotal
                );
            }

            m_Fase = Mathf.Repeat(
                m_Fase, cicloTotal
            );

            ActualizarEstado(0f);
        }


        void ActualizarEstado(float dt)
        {
            if (!vientoIntermitente)
            {
                vientoActivo = true;
                tiempoRestante = 0f;
                return;
            }

            float activo = Mathf.Max(
                0.01f, duracionActivo
            );

            float inactivo = Mathf.Max(
                0.01f, duracionInactivo
            );

            float cicloTotal = activo + inactivo;

            m_Fase = Mathf.Repeat(
                m_Fase + dt, cicloTotal
            );

            vientoActivo = m_Fase < activo;

            tiempoRestante = vientoActivo
                ? activo - m_Fase
                : cicloTotal - m_Fase;
        }


        void FixedUpdate()
        {
            if (m_Col == null || !m_Col.enabled)
                return;

            var gm = GameManager.Instance;

            if (gm != null &&
                gm.State != LevelState.Playing)
                return;

            // Actualizar el ciclo de viento.
            ActualizarEstado(Time.fixedDeltaTime);

            // Si no hay viento, no aplicar fuerzas.
            if (!vientoActivo)
                return;

            Vector2 dir =
                direction.sqrMagnitude < 0.0001f
                ? Vector2.up
                : direction.normalized;

            Bounds b = m_Col.bounds;

            int n = Physics2D.OverlapBox(
                b.center,
                b.size,
                0f,
                m_Filtro,
                s_Dentro
            );

            for (int i = 0; i < n; i++)
            {
                var otro = s_Dentro[i];

                if (otro == null)
                    continue;

                var ing =
                    otro.GetComponentInParent<Ingredient>();

                if (ing == null ||
                    ing.IsBroken ||
                    ing.IsDelivered)
                    continue;

                Rigidbody2D rb = ing.Body;

                if (rb == null ||
                    rb.bodyType != RigidbodyType2D.Dynamic)
                    continue;

                if (rb.IsSleeping())
                    rb.WakeUp();

                float along = Vector2.Dot(
                    rb.linearVelocity, dir
                );

                if (along >= maxSpeed)
                    continue;

                float factor = ing.IsLevitating
                    ? levitatingFactor
                    : 1f;

                rb.AddForce(
                    dir * acceleration * factor * rb.mass,
                    ForceMode2D.Force
                );
            }
        }
    }
}
