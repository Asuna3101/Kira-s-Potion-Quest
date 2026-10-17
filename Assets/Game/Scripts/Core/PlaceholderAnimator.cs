using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Animaciones PLACEHOLDER hechas por codigo (sin Animator ni clips) para que el
    /// prototipo tenga vida sin depender de arte final.
    ///
    /// Reemplazo previsto: cuando Melanie entregue los spritesheets, se quita este
    /// componente del objeto y se le pone un Animator con el AnimatorController de
    /// Assets/Game/Animations/&lt;NombreDelObjeto&gt;.controller. Ningun otro script
    /// depende de PlaceholderAnimator.
    /// </summary>
    public class PlaceholderAnimator : MonoBehaviour
    {
        public enum Mode
        {
            None,
            Bob,            // flota arriba y abajo
            Pulse,          // late de escala
            Spin,           // gira
            Flicker,        // parpadea de color (fuego, pinchos)
            ScrollUp,       // desplaza el color hacia arriba (corriente de aire)
            ShrinkAndFade   // one-shot: se encoge y desaparece (entrar al caldero)
        }

        public Mode mode = Mode.None;
        public float speed = 1f;
        public float amplitude = 0.08f;

        Vector3 m_BaseLocalPos;
        Vector3 m_BaseScale;
        SpriteRenderer m_Sr;
        Color m_BaseColor;
        float m_T;

        void Awake()
        {
            m_BaseLocalPos = transform.localPosition;
            m_BaseScale = transform.localScale;
            m_Sr = GetComponent<SpriteRenderer>();
            if (m_Sr != null) m_BaseColor = m_Sr.color;
            // desfase para que no latan todos igual (los one-shot siempre empiezan en 0)
            m_T = mode == Mode.ShrinkAndFade ? 0f : Random.Range(0f, 10f);
        }

        /// <summary>Arranca un modo desde cero (necesario para los one-shot).</summary>
        public void Play(Mode newMode, float newSpeed)
        {
            mode = newMode;
            speed = newSpeed;
            m_T = 0f;
            m_BaseLocalPos = transform.localPosition;
            m_BaseScale = transform.localScale;
            if (m_Sr == null) m_Sr = GetComponent<SpriteRenderer>();
            if (m_Sr != null) m_BaseColor = m_Sr.color;
        }

        void Update()
        {
            m_T += Time.deltaTime * Mathf.Max(0.01f, speed);

            switch (mode)
            {
                case Mode.None:
                    break;

                case Mode.Bob:
                    transform.localPosition = m_BaseLocalPos + new Vector3(0f, Mathf.Sin(m_T) * amplitude, 0f);
                    break;

                case Mode.Pulse:
                    transform.localScale = m_BaseScale * (1f + Mathf.Sin(m_T) * amplitude);
                    break;

                case Mode.Spin:
                    transform.localRotation = Quaternion.Euler(0f, 0f, m_T * 60f);
                    break;

                case Mode.Flicker:
                    if (m_Sr != null)
                    {
                        float f = 0.78f + 0.22f * Mathf.Sin(m_T * 7f) * Mathf.Sin(m_T * 3.1f);
                        m_Sr.color = new Color(m_BaseColor.r * f, m_BaseColor.g * f, m_BaseColor.b * f, m_BaseColor.a);
                    }
                    break;

                case Mode.ScrollUp:
                    // Solo color: NO se mueve el transform porque este objeto lleva el
                    // trigger de la corriente de aire y desplazarlo moveria la colision.
                    if (m_Sr != null)
                    {
                        float a = m_BaseColor.a * (0.6f + 0.4f * Mathf.Sin(m_T * 3f));
                        m_Sr.color = new Color(m_BaseColor.r, m_BaseColor.g, m_BaseColor.b, a);
                    }
                    break;

                case Mode.ShrinkAndFade:
                    float k = Mathf.Clamp01(m_T);
                    transform.localScale = m_BaseScale * (1f - k);
                    if (m_Sr != null)
                        m_Sr.color = new Color(m_BaseColor.r, m_BaseColor.g, m_BaseColor.b, m_BaseColor.a * (1f - k));
                    if (k >= 1f) gameObject.SetActive(false);
                    break;
            }
        }
    }
}
