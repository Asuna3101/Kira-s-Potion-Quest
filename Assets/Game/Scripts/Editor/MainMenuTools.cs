using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// "Hornear" el menu = crear sus GameObjects de verdad en la escena, para que
    /// se vean en la Hierarchy y se puedan mover y retocar en el Inspector, igual
    /// que en cualquier escena hecha a mano.
    ///
    /// Al entrar en Play, MainMenuController detecta que el canvas ya existe y lo
    /// reutiliza tal cual: lo que se ve en la Hierarchy es lo que se juega. Si no
    /// esta horneado, lo construye en caliente como antes.
    /// </summary>
    public static class MainMenuTools
    {
        [MenuItem("Chocoflan/7 - Hornear menu en la escena", false, 16)]
        public static void BakeMenu()
        {
            MainMenuController menu = FindMenu();
            if (menu == null) return;

            menu.HornearEnEscena();
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

            Transform canvas = menu.transform.Find(MainMenuController.NombreCanvas);
            int hijos = canvas != null ? canvas.childCount : 0;
            Debug.Log("[Chocoflan] Menu horneado en la escena: " + hijos +
                      " elementos dentro de " + MainMenuController.NombreCanvas +
                      ". Acuerdate de guardar la escena (Cmd+S).", menu);
        }

        [MenuItem("Chocoflan/8 - Limpiar menu de la escena", false, 17)]
        public static void ClearMenu()
        {
            MainMenuController menu = FindMenu();
            if (menu == null) return;

            menu.LimpiarDeEscena();
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
            Debug.Log("[Chocoflan] Menu limpiado de la escena.", menu);
        }

        static MainMenuController FindMenu()
        {
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            if (menu == null)
            {
                Debug.LogError("[Chocoflan] No hay ningun MainMenuController en la escena abierta. " +
                               "Abre MainMenu con Chocoflan > 2b.");
            }
            return menu;
        }
    }
}
