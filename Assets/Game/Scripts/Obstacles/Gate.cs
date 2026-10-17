using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Compuerta. Cerrada bloquea el paso; abierta se desliza hacia abajo y deja pasar.
    /// La abren PressurePlate (boton, por contacto) o Lever (palanca, click y mantener).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Gate : MonoBehaviour
    {
        [Tooltip("Cuanto se desplaza la compuerta al abrirse, en unidades de mundo.")]
        public float openOffsetY = -9.5f;

        public float openSpeed = 7f;

        public bool IsOpen { get; private set; }

        Vector3 m_ClosedPos;
        Vector3 m_OpenPos;
        Collider2D[] m_Colliders;

        void Awake()
        {
            m_ClosedPos = transform.position;
            m_OpenPos = m_ClosedPos + new Vector3(0f, openOffsetY, 0f);
            m_Colliders = GetComponents<Collider2D>();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            foreach (var c in m_Colliders) c.enabled = false;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            foreach (var c in m_Colliders) c.enabled = true;
        }

        void Update()
        {
            Vector3 target = IsOpen ? m_OpenPos : m_ClosedPos;
            if ((transform.position - target).sqrMagnitude > 0.0001f)
                transform.position = Vector3.MoveTowards(transform.position, target, openSpeed * Time.deltaTime);
        }
    }
}
