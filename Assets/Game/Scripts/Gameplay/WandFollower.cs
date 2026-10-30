using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// La varita de Kira apunta al ingrediente mientras se levita.
    ///
    /// El sprite Kira_varita es solo el palo con el brillo en la punta. Gira
    /// alrededor del MANGO (donde lo sostiene Kira), no alrededor de su centro,
    /// asi la base no se despega de la mano. Sin ingrediente levitando vuelve a su
    /// posicion original con un balanceo suave.
    /// </summary>
    public class WandFollower : MonoBehaviour
    {
        // Los valores por defecto son los de Level01Art (VaritaMango, VaritaPunta,
        // VaritaAnguloReposo). Estan aqui como campos serializados a proposito: con
        // el nivel HORNEADO en la escena el LevelBuilder no corre, asi que nadie
        // llamaria a Configure() y la varita giraria sobre su centro en vez de
        // sobre el mango.

        [Header("Seguimiento")]
        [Tooltip("Suavidad al girar (segundos). Mayor = mas lenta.")]
        public float smoothTime = 0.12f;

        [Tooltip("Giro maximo respecto a la posicion de reposo (grados).")]
        public float maxDeflection = 70f;

        [Tooltip("Segundos que sigue apuntando despues de soltar ESPACIO (mientras el ingrediente cae).")]
        public float returnDelay = 0.3f;

        [Header("Reposo")]
        public float idleSwayDegrees = 1.5f;
        public float idleSwaySpeed = 1.6f;

        [Header("Chispas en la punta mientras levita")]
        public float sparkInterval = 0.09f;

        [Header("Geometria de la varita")]
        [Tooltip("Donde esta el mango respecto al centro del sprite. Es el pivote al girar.")]
        public Vector2 handleOffset = new Vector2(-0.641f, 0.508f);

        [Tooltip("Centro del brillo de la punta, de donde salen las chispas.")]
        public Vector2 tipOffset = new Vector2(0.316f, -0.148f);

        [Tooltip("Angulo del palo (mango hacia punta) con el sprite sin girar.")]
        public float restAngleDegrees = -32.2f;

        // Alias internos, para no tocar el resto del codigo.
        Vector2 m_HandleOffset => handleOffset;
        Vector2 m_TipOffset => tipOffset;
        float m_RestAngle => restAngleDegrees;

        Vector3 m_RestCenter;       // posicion del sprite en reposo
        float m_Angle;              // giro actual respecto al reposo
        float m_AngleVelocity;
        float m_LastTarget;
        float m_NoTargetTimer;
        float m_SwayTime;
        float m_SparkTimer;

        LevitationController m_Levitation;

        void Awake()
        {
            m_RestCenter = transform.position;
        }

        /// <summary>
        /// handleOffset: donde esta el mango; tipOffset: centro del brillo de la punta;
        /// restAngleDegrees: angulo del palo (mango hacia punta) con el sprite sin girar.
        /// </summary>
        public void Configure(Vector2 handle, Vector2 tip, float restAngle)
        {
            handleOffset = handle;
            tipOffset = tip;
            restAngleDegrees = restAngle;
            m_RestCenter = transform.position;
        }

        void LateUpdate()
        {
            if (m_Levitation == null && GameManager.Instance != null)
                m_Levitation = GameManager.Instance.GetComponent<LevitationController>();

            Vector3 handleWorld = m_RestCenter + (Vector3)m_HandleOffset;

            // --- a donde quiere apuntar
            bool tracking = false;
            float target = m_LastTarget;
            if (m_Levitation != null && m_Levitation.IsLevitating && m_Levitation.Selected != null)
            {
                Vector2 toIngredient = (Vector2)m_Levitation.Selected.transform.position - (Vector2)handleWorld;
                if (toIngredient.sqrMagnitude > 0.0001f)
                {
                    float desired = Mathf.Atan2(toIngredient.y, toIngredient.x) * Mathf.Rad2Deg;
                    target = Mathf.Clamp(Mathf.DeltaAngle(m_RestAngle, desired), -maxDeflection, maxDeflection);
                    m_LastTarget = target;
                    tracking = true;
                }
            }

            if (tracking) m_NoTargetTimer = 0f;
            else m_NoTargetTimer += Time.deltaTime;

            m_SwayTime += Time.deltaTime;
            if (!tracking && m_NoTargetTimer >= returnDelay)
            {
                target = Mathf.Sin(m_SwayTime * idleSwaySpeed) * idleSwayDegrees;
                m_LastTarget = target;
            }

            m_Angle = Mathf.SmoothDampAngle(m_Angle, target, ref m_AngleVelocity, Mathf.Max(0.01f, smoothTime));

            // --- aplicar: gira alrededor del mango
            Quaternion rotation = Quaternion.Euler(0f, 0f, m_Angle);
            Vector3 centerFromHandle = rotation * (Vector3)(-m_HandleOffset);
            transform.position = new Vector3(
                handleWorld.x + centerFromHandle.x,
                handleWorld.y + centerFromHandle.y,
                m_RestCenter.z);
            transform.rotation = rotation;

            // --- chispas en la punta mientras levita
            if (tracking && sparkInterval > 0f)
            {
                m_SparkTimer -= Time.deltaTime;
                if (m_SparkTimer <= 0f)
                {
                    m_SparkTimer = sparkInterval * Random.Range(0.7f, 1.4f);
                    Vector3 tipFromHandle = rotation * (Vector3)(m_TipOffset - m_HandleOffset);
                    Vector2 tip = new Vector2(handleWorld.x + tipFromHandle.x, handleWorld.y + tipFromHandle.y);
                    Vector2 velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.1f, 0.5f));
                    FxParticle.Spawn(transform.parent, FxSprites.Glow, tip, velocity, 0f, 1f,
                        Random.Range(0.35f, 0.6f), Random.Range(0.07f, 0.12f), 0f,
                        new Color(1f, 0.96f, 0.78f, 0.95f), 0f, 0f, "FX", 6);
                }
            }
        }
    }
}
