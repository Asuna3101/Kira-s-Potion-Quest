using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Pared movil (compuerta). Cerrada bloquea el paso; abierta se desliza y deja
    /// pasar. La abren PressurePlate (boton, por contacto) o Lever (palanca).
    ///
    /// COMO SE AJUSTA
    /// --------------
    /// "Desplazamiento" es hacia donde y cuanto se mueve al abrirse, en unidades.
    /// Por defecto baja 2 u, pero puede ir a los lados: (-2, 0) la mete en la pared
    /// izquierda. Se ve en la escena como una flecha amarilla.
    ///
    /// QUE SE ARREGLO
    /// --------------
    /// 1. Antes los colliders se APAGABAN de golpe al abrir: el ingrediente
    ///    atravesaba una pared que todavia se veia cerrada, y al cerrarse volvian
    ///    a encenderse estando la pared abajo. Ahora los colliders no se tocan: la
    ///    pared estorba mientras esta arriba y deja pasar cuando ya bajo. Lo que
    ///    se ve es lo que bloquea.
    ///
    /// 2. Antes se movia con transform.position y sin Rigidbody, o sea como
    ///    collider ESTATICO. Unity reconstruye el arbol de colisiones estaticas en
    ///    cada frame que eso pasa, y ademas la pared atravesaba al ingrediente en
    ///    vez de empujarlo. Ahora lleva un Rigidbody2D cinematico y se mueve con
    ///    MovePosition en FixedUpdate, que es lo que empuja y arrastra bien.
    ///
    /// 3. Al cerrarse podia aplastar un ingrediente contra el suelo y dejarlo
    ///    atascado. Ahora, si hay algo en medio, la pared espera.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Gate : MonoBehaviour
    {
        [Header("Movimiento")]
        [Tooltip("Hacia donde y cuanto se desplaza al abrirse, en unidades.")]
        public Vector2 desplazamiento = new Vector2(0f, -2f);

        [Tooltip("Velocidad del deslizamiento (u/s).")]
        public float velocidad = 7f;

        [Tooltip("Empieza el nivel ya abierta.")]
        public bool abiertaAlEmpezar = false;

        [Header("Seguridad")]
        [Tooltip("Al cerrarse, si hay un ingrediente en medio la pared espera en vez " +
                 "de aplastarlo contra el suelo.")]
        public bool noAplastar = true;

        public bool IsOpen { get; private set; }

        /// <summary>True mientras se esta deslizando.</summary>
        public bool EnMovimiento { get; private set; }

        Rigidbody2D m_Rb;
        Vector2 m_Cerrada;
        Vector2 m_Abierta;
        Collider2D[] m_Cols;
        ContactFilter2D m_Filtro;
        static readonly Collider2D[] s_Buf = new Collider2D[8];

        void Awake()
        {
            m_Cerrada = transform.position;
            m_Abierta = m_Cerrada + desplazamiento;
            m_Cols = GetComponents<Collider2D>();

            // Cinematico: lo mueve el codigo, pero participa en la fisica como es
            // debido (barre, empuja y arrastra en vez de teletransportarse).
            m_Rb = GetComponent<Rigidbody2D>();
            if (m_Rb == null) m_Rb = gameObject.AddComponent<Rigidbody2D>();
            m_Rb.bodyType = RigidbodyType2D.Kinematic;
            m_Rb.simulated = true;
            m_Rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            m_Filtro = new ContactFilter2D();
            m_Filtro.NoFilter();
            m_Filtro.useTriggers = false;

            IsOpen = abiertaAlEmpezar;
            if (IsOpen) transform.position = m_Abierta;
        }

        public void Open() => IsOpen = true;
        public void Close() => IsOpen = false;
        public void Toggle() => IsOpen = !IsOpen;

        void FixedUpdate()
        {
            Vector2 objetivo = IsOpen ? m_Abierta : m_Cerrada;
            Vector2 actual = m_Rb.position;

            if ((actual - objetivo).sqrMagnitude <= 0.000001f)
            {
                EnMovimiento = false;
                return;
            }

            // Cerrandose con algo debajo: mejor esperar que atascarlo.
            if (!IsOpen && noAplastar && HayIngredienteEnMedio())
            {
                EnMovimiento = false;
                return;
            }

            EnMovimiento = true;
            m_Rb.MovePosition(Vector2.MoveTowards(actual, objetivo, velocidad * Time.fixedDeltaTime));
        }

        /// <summary>
        /// Mira si hay un ingrediente dentro del hueco por el que la pared va a
        /// pasar. Se consulta un poco mas alla del borde, en la direccion del
        /// cierre, para frenar antes de tocarlo.
        /// </summary>
        bool HayIngredienteEnMedio()
        {
            if (m_Cols == null || m_Cols.Length == 0) return false;

            Vector2 haciaCerrado = (m_Cerrada - m_Rb.position);
            Vector2 margen = haciaCerrado.sqrMagnitude < 0.0001f
                ? Vector2.zero
                : haciaCerrado.normalized * 0.12f;

            foreach (var c in m_Cols)
            {
                if (c == null || !c.enabled) continue;

                Bounds b = c.bounds;
                Vector2 centro = (Vector2)b.center + margen * 0.5f;
                Vector2 tam = (Vector2)b.size + new Vector2(Mathf.Abs(margen.x), Mathf.Abs(margen.y));

                int n = Physics2D.OverlapBox(centro, tam, 0f, m_Filtro, s_Buf);
                for (int i = 0; i < n; i++)
                {
                    var ing = s_Buf[i] != null ? s_Buf[i].GetComponentInParent<Ingredient>() : null;
                    if (ing != null && !ing.IsBroken && !ing.IsDelivered) return true;
                }
            }
            return false;
        }

        void OnDrawGizmosSelected()
        {
            // Flecha de a donde se va a mover, para colocarla a ojo en la escena.
            Vector3 desde = Application.isPlaying ? (Vector3)m_Cerrada : transform.position;
            Vector3 hasta = desde + (Vector3)desplazamiento;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(desde, hasta);
            Gizmos.DrawWireSphere(hasta, 0.12f);
        }
    }
}
