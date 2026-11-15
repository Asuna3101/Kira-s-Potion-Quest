using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Animacion de Kira al empezar un nivel.
    ///
    /// Es AUTOMATICA: no hay que poner nada en las escenas. Al cargarse una escena
    /// con GameManager (o sea, cualquier nivel), se instancia el prefab
    /// Assets/Resources/AnimacionKira (el objeto "AnimacionKira" de la escena
    /// Animacion_Kira) delante de la camara, tapando el nivel.
    ///
    /// Mientras suena:
    ///   - el nivel esta congelado (Time.timeScale = 0) y sin controles
    ///   - el HUD esta oculto
    ///   - se puede saltar con click, ESPACIO, ENTER o ESC (pasado medio segundo)
    ///
    /// Al terminar se descongela todo y se dispara Terminada; el HUD abre entonces
    /// la guia de como jugar (si le toca).
    ///
    /// NO se repite al reiniciar con R (se recarga la misma escena). Si se vuelve al
    /// menu y se entra otra vez, si se reproduce.
    /// </summary>
    public class KiraIntro : MonoBehaviour
    {
        /// <summary>Nombre del prefab dentro de una carpeta Resources.</summary>
        const string PrefabEnResources = "AnimacionKira";

        /// <summary>Duracion del clip Anim_kira (3.68 s), por si el Animator no la informa.</summary>
        const float DuracionPorDefecto = 3.6833f;

        /// <summary>Hasta pasado este tiempo no se puede saltar (evita saltarla sin querer).</summary>
        const float SegundosParaPoderSaltar = 0.5f;

        /// <summary>True mientras la animacion esta en pantalla.</summary>
        public static bool EnCurso { get; private set; }

        /// <summary>Se dispara cuando la animacion termina (o se salta).</summary>
        public static event Action Terminada;

        static string s_UltimoNivel = "";

        GameObject m_Instancia;
        Animator m_Anim;
        float m_T;
        float m_Duracion;
        bool m_Terminando;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarEstaticos()
        {
            EnCurso = false;
            Terminada = null;
            s_UltimoNivel = "";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Registrar()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        static void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo != LoadSceneMode.Single) return;

            // Menu, seleccion de nivel, etc.: no hay GameManager. Se olvida el ultimo
            // nivel para que al volver a entrar si suene la animacion.
            if (GameManager.Instance == null)
            {
                s_UltimoNivel = "";
                return;
            }

            // Misma escena otra vez = reinicio con R: no se repite.
            if (escena.name == s_UltimoNivel) return;
            s_UltimoNivel = escena.name;

            var prefab = Resources.Load<GameObject>(PrefabEnResources);
            if (prefab == null)
            {
                Debug.LogWarning("[KiraIntro] No encuentro Assets/Resources/" + PrefabEnResources + ".prefab");
                return;
            }

            var host = new GameObject("KiraIntro");
            host.AddComponent<KiraIntro>().Iniciar(prefab);
        }

        void Iniciar(GameObject prefab)
        {
            EnCurso = true;
            Time.timeScale = 0f;

            // Delante de la camara del nivel (ortografica, centrada).
            var pos = Vector3.zero;
            var cam = Camera.main;
            if (cam != null) pos = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);

            m_Instancia = Instantiate(prefab, pos, Quaternion.identity);
            m_Instancia.name = "AnimacionKira";

            // Por encima de todo el nivel: ultima capa de orden y orden altisimo.
            var sr = m_Instancia.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                var capas = SortingLayer.layers;
                if (capas.Length > 0) sr.sortingLayerID = capas[capas.Length - 1].id;
                sr.sortingOrder = 30000;
            }

            // El nivel esta congelado, asi que el Animator tiene que usar tiempo real.
            m_Anim = m_Instancia.GetComponentInChildren<Animator>();
            if (m_Anim != null)
            {
                m_Anim.updateMode = AnimatorUpdateMode.UnscaledTime;
                m_Anim.Play(0, 0, 0f);
            }
        }

        void Update()
        {
            if (m_Terminando) return;

            m_T += Time.unscaledDeltaTime;

            if (m_Duracion <= 0f && m_Anim != null)
            {
                var info = m_Anim.GetCurrentAnimatorStateInfo(0);
                if (info.length > 0.1f) m_Duracion = info.length;
            }
            float fin = m_Duracion > 0f ? m_Duracion : DuracionPorDefecto;

            bool saltar = false;
            if (m_T >= SegundosParaPoderSaltar)
            {
                var mouse = Mouse.current;
                var kb = Keyboard.current;
                saltar = (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                         (kb != null && (kb.spaceKey.wasPressedThisFrame ||
                                         kb.enterKey.wasPressedThisFrame ||
                                         kb.numpadEnterKey.wasPressedThisFrame ||
                                         kb.escapeKey.wasPressedThisFrame));
            }

            if (m_T >= fin || saltar)
            {
                m_Terminando = true;
                StartCoroutine(Terminar());
            }
        }

        IEnumerator Terminar()
        {
            // Un fotograma de margen: el click o la tecla con que se salto no tiene que
            // llegar tambien al juego (seleccionar un ingrediente, abrir la pausa...).
            if (m_Instancia != null) Destroy(m_Instancia);
            yield return null;

            EnCurso = false;
            Time.timeScale = 1f;
            Terminada?.Invoke();
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            // La escena se descargo con la animacion en marcha (R, por ejemplo).
            EnCurso = false;
            if (m_Instancia != null) Destroy(m_Instancia);
        }
    }
}
