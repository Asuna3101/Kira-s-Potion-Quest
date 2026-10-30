using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Mueve un objeto de arriba abajo en bucle.
    ///
    /// Para que dos objetos se muevan JUNTOS (la vela sobre su plataforma), se les
    /// pone el mismo Amplitud, Velocidad y Desfase. Cada uno oscila alrededor de su
    /// propia posicion inicial, asi que mantienen la distancia entre ellos.
    ///
    /// Si el objeto no tiene Rigidbody2D se le anade uno Kinematic: es la forma
    /// correcta de mover un collider en Unity. Moviendo solo el Transform, el
    /// motor de fisicas recalcula el collider entero cada frame y los choques
    /// salen irregulares.
    /// </summary>
    [DisallowMultipleComponent]
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Recorrido")]
        [Tooltip("Cuanto sube y baja desde su sitio, en unidades. 1 unidad = un bloque.")]
        public float amplitud = 0.8f;

        [Tooltip("Velocidad del vaiven. 1 = un ciclo completo cada ~6 segundos.")]
        public float velocidad = 1f;

        [Tooltip("Desfase del ciclo (0 a 1). Dos objetos con el mismo desfase van sincronizados.")]
        [Range(0f, 1f)]
        public float desfase;

        [Tooltip("Horizontal en vez de vertical.")]
        public bool horizontal;

        Rigidbody2D m_Rb;
        Vector2 m_Origen;
        float m_T;

        void Awake()
        {
            m_Origen = transform.position;

            // El Rigidbody2D solo hace falta si el objeto tiene collider. Para un
            // objeto decorativo (un efecto, por ejemplo) basta mover el Transform.
            m_Rb = GetComponent<Rigidbody2D>();
            if (m_Rb == null && GetComponent<Collider2D>() != null)
                m_Rb = gameObject.AddComponent<Rigidbody2D>();

            if (m_Rb != null)
            {
                m_Rb.bodyType = RigidbodyType2D.Kinematic;
                m_Rb.simulated = true;
                m_Rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                m_Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        void FixedUpdate()
        {
            m_T += Time.fixedDeltaTime * velocidad;

            float ciclo = (m_T + desfase * Mathf.PI * 2f);
            float offset = Mathf.Sin(ciclo) * amplitud;

            Vector2 destino = horizontal
                ? new Vector2(m_Origen.x + offset, m_Origen.y)
                : new Vector2(m_Origen.x, m_Origen.y + offset);

            if (m_Rb != null) m_Rb.MovePosition(destino);
            else transform.position = destino;
        }

        /// <summary>Vuelve a tomar la posicion actual como centro del recorrido.</summary>
        public void Reanclar()
        {
            m_Origen = transform.position;
            m_T = 0f;
        }

        void OnDrawGizmosSelected()
        {
            Vector3 c = Application.isPlaying ? (Vector3)m_Origen : transform.position;
            Vector3 d = horizontal ? Vector3.right * amplitud : Vector3.up * amplitud;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
            Gizmos.DrawLine(c - d, c + d);
            Gizmos.DrawWireSphere(c - d, 0.08f);
            Gizmos.DrawWireSphere(c + d, 0.08f);
        }
    }
}
