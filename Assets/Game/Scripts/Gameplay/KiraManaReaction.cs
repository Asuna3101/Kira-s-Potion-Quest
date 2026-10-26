using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Cambia el sprite COMPLETO de Kira segun el mana (sin caritas superpuestas).
    /// Tres variantes del arte original: feliz, cansada y agotada.
    /// No altera la levitacion ni se utiliza en Nivel 2.
    /// </summary>
    [DisallowMultipleComponent]
    public class KiraManaReaction : MonoBehaviour
    {
        [Header("Umbrales de mana")]
        [Range(0.05f, 0.95f)] public float tiredAt = 0.66f;
        [Range(0.01f, 0.70f)] public float exhaustedAt = 0.33f;

        SpriteRenderer m_Character;
        Sprite m_Original;
        Sprite m_Happy;
        Sprite m_Tired;
        Sprite m_Exhausted;
        int m_LastMood = -1;

        void Start()
        {
            if (!Application.isPlaying) return;
            m_Character = GetComponent<SpriteRenderer>();
            if (m_Character == null) return;

            m_Original = m_Character.sprite;
            m_Happy = Resources.Load<Sprite>("KiraMana/Kira_Feliz");
            m_Tired = Resources.Load<Sprite>("KiraMana/Kira_Cansada");
            m_Exhausted = Resources.Load<Sprite>("KiraMana/Kira_Agotada");

            // En ausencia de assets no aparece una expresion flotante.
            if (m_Happy == null || m_Tired == null || m_Exhausted == null)
            {
                Debug.LogWarning("KiraManaReaction: faltan sprites KiraMana en Resources.");
                enabled = false;
                return;
            }
            Refresh(force: true);
        }

        void Update()
        {
            Refresh(force: false);
        }

        void Refresh(bool force)
        {
            if (m_Character == null || GameManager.Instance == null) return;

            float mana = GameManager.Instance.ManaNormalized;
            float medio = Mathf.Max(tiredAt, exhaustedAt + 0.01f);
            int mood = mana <= exhaustedAt ? 2 : mana <= medio ? 1 : 0;
            if (!force && mood == m_LastMood) return;

            m_LastMood = mood;
            // Es Kira COMPLETA, no una segunda capa ni un icono sobre su cara.
            m_Character.sprite = mood == 0 ? m_Happy : mood == 1 ? m_Tired : m_Exhausted;
        }

        void OnDestroy()
        {
            if (m_Character != null && m_Original != null)
                m_Character.sprite = m_Original;
        }
    }
}
