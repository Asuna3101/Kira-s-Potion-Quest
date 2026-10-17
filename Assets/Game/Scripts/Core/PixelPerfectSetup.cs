using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Deja la camara exactamente como pide el documento de arte:
    /// 1920 x 1080 (16:9), ortografica con Size 4.5, centrada en (0,0).
    /// Asi la pantalla mide 16 x 9 unidades y cada sprite importado a 240 PPU se
    /// ve con su medida de diseno.
    ///
    /// SOBRE PIXEL PERFECT
    /// -------------------
    /// Esta clase se llamaba asi porque el plan anterior era 480x320 a 32 PPU, un
    /// pipeline de pixel art. Con las medidas nuevas el juego ya NO es pixel art:
    /// el arte se dibuja a 240 px por unidad y en pantalla se ve a 120 px por
    /// unidad, o sea al 50%. Eso es "HD 2D" con supersampling, y la Pixel Perfect
    /// Camera no corresponde:
    ///
    ///   - Pixel Perfect Camera exige orthographicSize = refResY / (2 * PPU).
    ///     Con PPU 240 y Size 4.5 eso da refResY = 2160, o sea 3840 x 2160.
    ///   - En una pantalla de 1080p ese factor es 0.5x, un escalado NO entero,
    ///     que es justo lo que la Pixel Perfect Camera no sabe hacer bien.
    ///   - Ademas el ajuste al pixel haria "saltar" a los ingredientes, que se
    ///     mueven de forma continua con la levitacion.
    ///
    /// Por eso queda APAGADA por defecto. Si mas adelante el equipo decide
    /// cambiar a pixel art de verdad, se activa usePixelPerfect y ya queda
    /// configurada con el componente correcto de URP
    /// (UnityEngine.Rendering.Universal.PixelPerfectCamera; NO hay que instalar
    /// com.unity.2d.pixel-perfect, que es el de Built-in).
    ///
    /// Para que el arte a 240 PPU se vea nitido al 50%, lo que SI importa es
    /// Filter Mode = Bilinear (o Trilinear) y mipmaps activados en los sprites:
    /// lo contrario de lo que pediria el pixel art.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class PixelPerfectSetup : MonoBehaviour
    {
        [Header("Camara (documento de arte)")]
        [Tooltip("1920 x 1080, 16:9.")]
        public int referenceWidth = GridSettings.ReferenceWidthPx;
        public int referenceHeight = GridSettings.ReferenceHeightPx;

        [Header("Pixel Perfect (apagado: el juego no es pixel art)")]
        [Tooltip("Solo activar si el equipo pasa a un pipeline de pixel art. Ver el comentario de la clase.")]
        public bool usePixelPerfect = false;

        public int assetsPixelsPerUnit = GridSettings.AssetsPixelsPerUnit;

        [Tooltip("Windowbox anade bandas negras en vez de estirar cuando la pantalla no es multiplo exacto.")]
        public PixelPerfectCamera.CropFrame cropFrame = PixelPerfectCamera.CropFrame.Windowbox;

        public PixelPerfectCamera.GridSnapping gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;

        void Awake() { Apply(true); }

        // En OnValidate NO se puede llamar a AddComponent, solo configurar lo existente.
        void OnValidate() { Apply(false); }

        public void Apply(bool allowAddComponent)
        {
            var cam = GetComponent<Camera>();
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = GridSettings.CameraOrthographicSize; // 4.5
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.allowDynamicResolution = false;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PlaceholderArt.BgCol;

            var urp = GetComponent<UniversalAdditionalCameraData>();
            if (urp != null)
            {
                urp.antialiasing = AntialiasingMode.None;
                urp.renderPostProcessing = false;
            }

            // La camara queda centrada en el tablero (el mundo visible es
            // x -8..8, y -4.5..4.5).
            transform.position = new Vector3(0f, 0f, -10f);
            transform.rotation = Quaternion.identity;

            var ppc = GetComponent<PixelPerfectCamera>();

            if (!usePixelPerfect)
            {
                if (ppc != null) ppc.enabled = false;
                return;
            }

            if (ppc == null)
            {
                if (!allowAddComponent) return;
                ppc = gameObject.AddComponent<PixelPerfectCamera>();
            }
            ppc.enabled = true;
            ppc.assetsPPU = assetsPixelsPerUnit;

            // La resolucion de referencia la MANDA la camara: tiene que cumplir
            // refResY = 2 * orthographicSize * PPU, si no la Pixel Perfect Camera
            // pisa el Size y el encuadre deja de ser 16 x 9 unidades.
            ppc.refResolutionY = Mathf.RoundToInt(2f * GridSettings.CameraOrthographicSize * assetsPixelsPerUnit);
            ppc.refResolutionX = Mathf.RoundToInt(ppc.refResolutionY * (GridSettings.WorldWidth / GridSettings.WorldHeight));
            ppc.cropFrame = cropFrame;
            ppc.gridSnapping = gridSnapping;
        }
    }
}
