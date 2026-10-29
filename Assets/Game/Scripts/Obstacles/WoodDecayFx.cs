using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Desgaste VISUAL de la madera: de vez en cuando se desprende una astilla o una
    /// mota de polvo del borde inferior de la plataforma y cae. Solo decoracion:
    /// sin colliders ni fisica. Sube/baja la cantidad con "interval".
    /// </summary>
    public class WoodDecayFx : MonoBehaviour
    {
        public Vector2 size = Vector2.one;

        [Tooltip("Plataforma a la que pertenece. Si se mueve o cambia de tamano, el desgaste la sigue.")]
        public Transform target;

        BoxCollider2D m_Box;
        [Tooltip("Segundos medios entre particulas por cada 2 u de plataforma. Mayor = menos desgaste.")]
        public float interval = 1.1f;
        public Color woodColor = new Color32(0x8A, 0x5A, 0x34, 0xFF);
        public Color dustColor = new Color32(0xC9, 0xA2, 0x78, 0xFF);

        float m_Timer;

        void Start()
        {
            m_Timer = Random.Range(0f, interval);
        }

        void Update()
        {
            if (target != null)
            {
                if (m_Box == null) m_Box = target.GetComponent<BoxCollider2D>();
                if (m_Box != null)
                {
                    transform.position = m_Box.bounds.center;
                    size = m_Box.bounds.size;
                }
            }

            float widthFactor = Mathf.Max(0.5f, size.x / 2f);
            m_Timer -= Time.deltaTime * widthFactor;
            if (m_Timer > 0f) return;
            m_Timer = interval * Random.Range(0.6f, 1.6f);

            Vector2 p = (Vector2)transform.position + new Vector2(
                Random.Range(-size.x, size.x) * 0.45f, -size.y * 0.5f + 0.02f);

            if (Random.value < 0.6f)
            {
                // astillita: cae girando y se desvanece
                float s = Random.Range(0.035f, 0.07f);
                FxParticle.Spawn(transform.parent, FxSprites.Disc, p,
                    new Vector2(Random.Range(-0.08f, 0.08f), 0f), 3.2f, 0.1f,
                    Random.Range(0.8f, 1.3f), s, s * 0.7f, woodColor,
                    Random.Range(-180f, 180f), 0.5f, "FX", 1);
            }
            else
            {
                // polvillo: cae lento y se esfuma
                FxParticle.Spawn(transform.parent, FxSprites.Glow, p,
                    new Vector2(Random.Range(-0.05f, 0.05f), -0.05f), 0.6f, 0.8f,
                    Random.Range(0.9f, 1.5f), 0.07f, 0.16f,
                    new Color(dustColor.r, dustColor.g, dustColor.b, 0.45f), 0f, 0.1f, "FX", 1);
            }
        }
    }
}
