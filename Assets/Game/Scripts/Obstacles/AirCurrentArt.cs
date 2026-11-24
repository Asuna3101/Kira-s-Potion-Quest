
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(AirCurrent))]
    public class AirCurrentArt : MonoBehaviour
    {
        [Header("Animacion original")]

        public Sprite[] fotogramas;

        [Range(0.5f, 24f)]
        public float fotogramasPorSegundo = 6f;

        public bool animarFotogramas = true;

        [Tooltip("Desfase aleatorio solo de los fotogramas.")]
        public bool desfaseAleatorio = false;


        [Header("Desplazamiento infinito")]

        public bool desplazamientoInfinito = true;

        [Min(0f)]
        public float velocidadDesplazamiento = 1.5f;

        public bool sincronizarConCorriente = true;

        public Vector2 direccionManual = Vector2.up;

        public bool invertirDesplazamiento = false;

        [Tooltip("Impide cualquier desplazamiento horizontal local.")]
        public bool bloquearEjeX = true;


        [Header("Transiciones de viento")]

        [Min(0f)]
        public float duracionFadeIn = 0.5f;

        [Min(0f)]
        public float duracionFadeOut = 0.5f;

        [Range(0f, 1f)]
        public float opacidadMaxima = 1f;

        public bool moverDuranteFadeOut = true;

        public bool fadeAlComenzar = false;

        public bool animarDurantePausa = false;

        [Tooltip("Reinicia el desplazamiento cuando el arte es invisible.")]
        public bool reiniciarAlApagarse = false;


        [Header("Estado visual")]

        [SerializeField]
        private float opacidadActual;


        private SpriteRenderer m_Sr;
        private AirCurrent m_Corriente;
        private MaterialPropertyBlock m_Propiedades;

        private float m_T;
        private int m_Actual = -1;

        private Vector2 m_Offset;

        private float m_Alpha;


        static readonly int OffsetID =
            Shader.PropertyToID("_ScrollOffset");

        static readonly int UVRectID =
            Shader.PropertyToID("_UVRect");

        static readonly int WindAlphaID =
            Shader.PropertyToID("_WindAlpha");


        void Awake()
        {
            m_Sr = GetComponent<SpriteRenderer>();
            m_Corriente = GetComponent<AirCurrent>();

            m_Propiedades = new MaterialPropertyBlock();

            // El dibujo siempre comienza alineado.
            m_Offset = Vector2.zero;

            // El azar solo afecta a los fotogramas,
            // nunca a la posicion de la textura.
            m_T = desfaseAleatorio
                ? Random.value * 10f
                : 0f;

            ActualizarFotogramas(0f);
        }


        void Start()
        {
            bool activo = HayViento();

            m_Alpha = activo && !fadeAlComenzar
                ? 1f
                : 0f;

            ActualizarMaterial();
        }


        void Update()
        {
            if (m_Sr == null)
                return;

            if (!animarDurantePausa)
            {
                var gm = GameManager.Instance;

                if (gm != null &&
                    gm.State != LevelState.Playing)
                    return;
            }

            float dt = animarDurantePausa
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            bool activo = HayViento();

            // Actualizar el fade siempre,
            // incluso cuando no existe viento.
            ActualizarFade(activo, dt);

            bool debeAnimarse = activo ||
                (moverDuranteFadeOut &&
                 m_Alpha > 0f);

            if (debeAnimarse)
            {
                ActualizarFotogramas(dt);
                ActualizarDesplazamiento(dt);
            }

            // Reiniciar solamente cuando es invisible.
            if (!activo && m_Alpha <= 0f &&
                reiniciarAlApagarse)
            {
                m_Offset = Vector2.zero;
            }

            // Mantener el eje horizontal bloqueado.
            if (bloquearEjeX)
                m_Offset.x = 0f;

            // IMPORTANTE:
            // El material se actualiza siempre.
            // No depende de que exista viento.
            ActualizarMaterial();
        }


        bool HayViento()
        {
            return m_Corriente != null &&
                   m_Corriente.isActiveAndEnabled &&
                   m_Corriente.VientoActivo;
        }


        void ActualizarFade(bool activo, float dt)
        {
            float objetivo = activo ? 1f : 0f;

            float duracion = activo
                ? duracionFadeIn
                : duracionFadeOut;

            if (duracion <= 0f)
            {
                m_Alpha = objetivo;
            }
            else
            {
                m_Alpha = Mathf.MoveTowards(
                    m_Alpha,
                    objetivo,
                    dt / duracion
                );
            }
        }


        void ActualizarFotogramas(float dt)
        {
            if (fotogramas == null ||
                fotogramas.Length == 0)
                return;

            if (animarFotogramas)
                m_T += dt;

            int i = animarFotogramas
                ? Mathf.FloorToInt(
                    m_T * fotogramasPorSegundo
                ) % fotogramas.Length
                : 0;

            if (i == m_Actual)
                return;

            m_Actual = i;

            if (fotogramas[i] != null)
                m_Sr.sprite = fotogramas[i];
        }


        void ActualizarDesplazamiento(float dt)
        {
            if (!desplazamientoInfinito ||
                m_Sr.sprite == null)
                return;

            Vector2 dir =
                sincronizarConCorriente &&
                m_Corriente != null
                ? m_Corriente.direction
                : direccionManual;

            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector2.up;

            dir.Normalize();

            if (invertirDesplazamiento)
                dir = -dir;

            Vector3 movimientoLocal =
                transform.InverseTransformVector(
                    new Vector3(dir.x, dir.y, 0f)
                );

            Vector2 tamanoSprite =
                m_Sr.sprite.bounds.size;

            float dx = movimientoLocal.x /
                Mathf.Max(tamanoSprite.x, 0.0001f);

            float dy = movimientoLocal.y /
                Mathf.Max(tamanoSprite.y, 0.0001f);

            if (m_Sr.flipX)
                dx = -dx;

            if (m_Sr.flipY)
                dy = -dy;

            // Evitar desplazamientos laterales.
            if (bloquearEjeX)
                dx = 0f;

            m_Offset += new Vector2(dx, dy)
                * velocidadDesplazamiento * dt;

            m_Offset.x = bloquearEjeX
                ? 0f
                : Mathf.Repeat(m_Offset.x, 1f);

            m_Offset.y = Mathf.Repeat(
                m_Offset.y, 1f
            );
        }


        void ActualizarMaterial()
        {
            // Fade suave.
            opacidadActual = Mathf.SmoothStep(
                0f, 1f, m_Alpha
            ) * opacidadMaxima;

            m_Sr.GetPropertyBlock(m_Propiedades);

            // Enviar directamente la opacidad al shader.
            m_Propiedades.SetFloat(
                WindAlphaID,
                opacidadActual
            );

            Vector2 offset = desplazamientoInfinito
                ? m_Offset
                : Vector2.zero;

            m_Propiedades.SetVector(
                OffsetID,
                new Vector4(offset.x, offset.y, 0f, 0f)
            );

            Sprite sprite = m_Sr.sprite;

            if (sprite != null)
            {
                Rect rect = sprite.textureRect;
                Texture2D textura = sprite.texture;

                Vector4 uvRect = new Vector4(
                    rect.x / textura.width,
                    rect.y / textura.height,
                    rect.width / textura.width,
                    rect.height / textura.height
                );

                m_Propiedades.SetVector(
                    UVRectID,
                    uvRect
                );
            }

            m_Sr.SetPropertyBlock(m_Propiedades);
        }
    }
}
