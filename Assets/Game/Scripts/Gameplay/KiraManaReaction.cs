using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    [DisallowMultipleComponent]
    public class KiraManaReaction : MonoBehaviour
    {
        [Header("Caras (Assets/Game/Sprites/Character)")]
        public Sprite caraFeliz;
        public Sprite caraTriste;
        public Sprite caraSinMana;

        [Header("Umbrales de mana")]
        [Range(0.05f, 0.95f)] public float tiredAt = 0.66f;
        [Range(0.01f, 0.70f)] public float exhaustedAt = 0.33f;

        [Header("Resultado del nivel")]
        [Tooltip("Al perder el nivel se queda con la cara triste, pase lo que pase con el mana.")]
        public bool reaccionarAlResultado = true;

        const int Feliz = 0;
        const int Triste = 1;
        const int SinMana = 2;

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

            m_Happy = caraFeliz != null ? caraFeliz : Resources.Load<Sprite>("KiraMana/Kira_Feliz");
            m_Tired = caraTriste != null ? caraTriste : Resources.Load<Sprite>("KiraMana/Kira_Cansada");
            m_Exhausted = caraSinMana != null ? caraSinMana : Resources.Load<Sprite>("KiraMana/Kira_Agotada");

            Sprite cualquiera = m_Happy != null ? m_Happy : (m_Tired != null ? m_Tired : m_Exhausted);
            if (cualquiera == null)
            {
                Debug.LogWarning("KiraManaReaction: no hay ninguna cara asignada " +
                                 "(ni en el Inspector ni en Resources/KiraMana).", this);
                enabled = false;
                return;
            }
            if (m_Happy == null) m_Happy = cualquiera;
            if (m_Tired == null) m_Tired = cualquiera;
            if (m_Exhausted == null) m_Exhausted = cualquiera;

            Refresh(force: true);
        }

        void Update()
        {
            Refresh(force: false);
        }

        void Refresh(bool force)
        {
            if (m_Character == null) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            int mood = MoodActual(gm);
            if (!force && mood == m_LastMood) return;

            m_LastMood = mood;
            m_Character.sprite = mood == Feliz ? m_Happy : mood == Triste ? m_Tired : m_Exhausted;
        }

        int MoodActual(GameManager gm)
        {
            if (reaccionarAlResultado)
            {
                if (gm.State == LevelState.Lost) return Triste;
                if (gm.State == LevelState.Won) return Feliz;
            }

            float mana = gm.ManaNormalized;
            float medio = Mathf.Max(tiredAt, exhaustedAt + 0.01f);
            if (mana <= exhaustedAt) return SinMana;
            if (mana <= medio) return Triste;
            return Feliz;
        }

        void OnDestroy()
        {
            if (m_Character != null && m_Original != null)
                m_Character.sprite = m_Original;
        }
    }
}
