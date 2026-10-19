using UnityEngine;
using UnityEngine.UI;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// HUD placeholder construido por codigo con uGUI (el proyecto no tiene TextMeshPro
    /// importado). Canvas en Screen Space - Overlay a proposito: con la Pixel Perfect
    /// Camera en modo Upscale Render Texture el texto se ve borroso si el canvas
    /// renderiza a traves de la camara.
    ///
    /// Muestra: nombre del nivel, ingredientes entregados, barra de mana, controles
    /// y el panel de victoria / derrota.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("Apariencia")]
        public int referenceWidth = GridSettings.ReferenceWidthPx;
        public int referenceHeight = GridSettings.ReferenceHeightPx;

        Text m_TitleText;
        Text m_HintText;
        Text m_ControlsText;
        Text m_ResultText;
        Image m_ManaFill;
        Image m_ResultPanel;

        GameManager m_Game;
        float m_HintTimer;

        void Start()
        {
            BuildUi();
            m_Game = GameManager.Instance;
            if (m_Game != null)
            {
                m_Game.StateChanged += OnStateChanged;
                m_Game.HintChanged += OnHint;
                m_Game.IngredientsChanged += RefreshTitle;
            }
            RefreshTitle();
            ShowResult(false, "");
        }

        void OnDestroy()
        {
            if (m_Game == null) return;
            m_Game.StateChanged -= OnStateChanged;
            m_Game.HintChanged -= OnHint;
            m_Game.IngredientsChanged -= RefreshTitle;
        }

        void Update()
        {
            if (m_Game == null) m_Game = GameManager.Instance;
            if (m_Game != null && m_ManaFill != null)
                m_ManaFill.rectTransform.localScale = new Vector3(m_Game.ManaNormalized, 1f, 1f);

            if (m_HintTimer > 0f)
            {
                m_HintTimer -= Time.deltaTime;
                if (m_HintTimer <= 0f && m_HintText != null) m_HintText.text = "";
            }
        }

        // ---------------- eventos ------------------------------------------

        void OnStateChanged(LevelState state, string message)
        {
            switch (state)
            {
                case LevelState.Won:
                    ShowResult(true, "NIVEL SUPERADO\n" + message + "\n\nR = reiniciar");
                    break;
                case LevelState.Lost:
                    ShowResult(true, "NIVEL FALLIDO\n" + message);
                    break;
                default:
                    ShowResult(false, "");
                    break;
            }
            RefreshTitle();
        }

        void OnHint(string message)
        {
            if (m_HintText == null) return;
            m_HintText.text = message;
            m_HintTimer = 3.5f;
        }

        void RefreshTitle()
        {
            if (m_TitleText == null) return;
            int done = m_Game != null ? m_Game.DeliveredIngredients : 0;
            int total = m_Game != null ? m_Game.TotalIngredients : 0;
            var builder = m_Game != null ? m_Game.builder : null;
            string title = builder != null ? builder.CurrentDisplayName : Level01Data.DisplayName;
            m_TitleText.text = title + "   Ingredientes " + done + " / " + total;
        }

        void ShowResult(bool visible, string message)
        {
            if (m_ResultPanel != null) m_ResultPanel.gameObject.SetActive(visible);
            if (m_ResultText != null) m_ResultText.text = message;
        }

        // ---------------- construccion del canvas ---------------------------

        void BuildUi()
        {
            var canvasGo = new GameObject("HUD_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            Font font = LoadFont();

            // El HUD esta disenado en un espacio logico de 480 x 270 (16:9) y se
            // escala al alto de referencia real. Con 1080 de alto, k = 4.
            float k = referenceHeight / 270f;
            int Sz(float v) => Mathf.Max(1, Mathf.RoundToInt(v * k));
            Vector2 V(float x, float y) => new Vector2(x * k, y * k);

            // --- barra superior
            m_TitleText = MakeText(canvasGo.transform, "Txt_Titulo", font, Sz(12), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), V(6f, -6f), V(300f, 16f));

            m_HintText = MakeText(canvasGo.transform, "Txt_Pista", font, Sz(11), TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), V(-6f, -6f), V(300f, 32f));
            m_HintText.color = PlaceholderArt.Highlight;

            // --- barra de mana
            var manaBg = MakeImage(canvasGo.transform, "ManaBar_Bg",
                new Vector2(0f, 0f), new Vector2(0f, 0f), V(6f, 24f), V(96f, 8f));
            manaBg.color = new Color(0f, 0f, 0f, 0.55f);

            m_ManaFill = MakeImage(manaBg.transform, "ManaBar_Fill",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, V(96f, 8f));
            m_ManaFill.color = PlaceholderArt.PortalACol;
            m_ManaFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            m_ManaFill.rectTransform.anchoredPosition = Vector2.zero;

            var manaLabel = MakeText(canvasGo.transform, "Txt_Mana", font, Sz(10), TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), V(6f, 33f), V(120f, 12f));
            manaLabel.text = "Mana de levitacion";

            // --- controles
            m_ControlsText = MakeText(canvasGo.transform, "Txt_Controles", font, Sz(10), TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), V(6f, 4f), V(468f, 16f));
            m_ControlsText.text = "Click = elegir ingrediente   |   Mantener ESPACIO = SUBIR   |   " +
                                  "Flechas IZQ/DER = MOVER   |   Soltar ESPACIO = CAER   |   " +
                                  "Click y mantener = interactuar   |   Tab = cambiar   |   R = reiniciar";

            // --- panel de resultado
            m_ResultPanel = MakeImage(canvasGo.transform, "Panel_Resultado",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(320f, 110f));
            m_ResultPanel.color = new Color(0.05f, 0.03f, 0.1f, 0.88f);

            m_ResultText = MakeText(m_ResultPanel.transform, "Txt_Resultado", font, Sz(14), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(300f, 100f));
        }

        static Font LoadFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor anchor,
                             Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size2)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            RectTransform rt = text.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rt.sizeDelta = size2;
            rt.anchoredPosition = position;
            return text;
        }

        static Image MakeImage(Transform parent, string name,
                               Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;

            RectTransform rt = img.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return img;
        }
    }
}
