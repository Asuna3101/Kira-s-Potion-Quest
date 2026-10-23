# Menú de inicio

Escena: `Assets/Game/Scenes/MainMenu.unity`
Código: `Assets/Game/Scripts/UI/MainMenuController.cs`
Arte: `Assets/Game/Sprites/UI/`

---

## Cómo se arma

Igual que el arte del nivel, cada PNG de `UI/` es el **lienzo completo de
3840 × 2160** con el elemento dibujado en su sitio y recortado en el Sprite Editor.
El canvas del menú es de **1920 × 1080**, exactamente la mitad, así que la posición
de cada pieza sale sola de su `rect`:

```
pos.x  = (rect.x + rect.ancho/2 - 1920) / 2
pos.y  = (rect.y + rect.alto/2  - 1080) / 2
tamaño = rect.ancho/2 , rect.alto/2
```

El menú queda idéntico al `Mockup.JPG` **sin colocar nada a mano**.

| Pieza | `rect` en el Sprite Editor | Posición en canvas | Tamaño |
|---|---|---|---|
| `Title.PNG` | (721, 694, 2489, 1262) | (22.75, 122.5) | 1244.5 × 631 |
| `Boton_créditos.PNG` | (528, 203, 772, 374) | (−503, −345) | 386 × 187 |
| `Boton_play.PNG` | (1539, 197, 772, 374) | (2.5, −348) | 386 × 187 |
| `Boton_settings.PNG` | (2550, 197, 772, 374) | (508, −348) | 386 × 187 |
| `Fondo_,menu.JPG` | lienzo completo | estirado a pantalla | — |

Si Melanie reexporta y mueve un botón, se miran los `rect` nuevos en el Sprite
Editor y se recalculan esas constantes en `MainMenuController`.

---

## Ver el menú en la Hierarchy ("hornearlo")

Por defecto el menú se construye por código al entrar en Play, así que antes de
darle Play la escena se ve vacía. Para tenerlo como las demás escenas, con sus
elementos visibles en el panel de la izquierda:

**`Chocoflan > 7 - Hornear menu en la escena`** → crea el canvas y sus hijos como
GameObjects de verdad. Después hay que **guardar la escena** (`Cmd+S`).

Queda así en la Hierarchy:

```
Main Camera
--- MENU ---
  Menu_Canvas          (Canvas + Canvas Scaler)
    Fondo
    Titulo
    Boton_Creditos
    Boton_Play
    Boton_Settings
```

A partir de ahí se puede mover, escalar y cambiar cualquier cosa en el Inspector:
**al entrar en Play el script detecta que el canvas ya existe y lo reutiliza tal
cual**, solo le engancha la acción a cada botón. Lo que se ve en la Hierarchy es
exactamente lo que se juega.

`Chocoflan > 8 - Limpiar menu de la escena` lo borra y se vuelve al modo de
construir en caliente.

Los paneles de **créditos** y **ajustes** se siguen armando al vuelo, estén o no
horneados los demás elementos: son diálogos que aparecen y desaparecen, no algo
que se retoque en la Hierarchy.

---

## Input: sin EventSystem

El proyecto usa **solo el Input System nuevo** (`activeInputHandler: 1`). El
`StandaloneInputModule` de siempre no funciona ahí, y el `InputSystemUIInputModule`
hay que configurarlo con un asset de acciones.

Para evitar esa dependencia, el menú **no usa `EventSystem` ni
`UnityEngine.UI.Button`**: lee `Mouse.current` y hace hit-test contra el
`RectTransform` de cada botón con `RectTransformUtility.RectangleContainsScreenPoint`.
Menos piezas que configurar y no se rompe si alguien toca el EventSystem.

Efecto de hover: el botón crece un 6 % (`hoverScale`).

---

## Qué hace cada botón

| Botón | Acción |
|---|---|
| **play** | Carga la escena `Level01`. Si no está en Build Settings, escribe un error claro en consola en vez de fallar en silencio. |
| **credits** | Abre un panel con el equipo (del GDD). |
| **settings** | Abre un panel con volumen general (`AudioListener.volume`, en pasos de 10 %) y alternar pantalla completa / ventana. |

`Esc` cierra el panel abierto. Con un panel abierto, los botones del menú de atrás
se desactivan; solo responden los del panel.

El panel de ajustes es **deliberadamente mínimo**: todavía no hay AudioMixer en el
proyecto, así que mueve el volumen global. Cuando exista el mixer, se cambia ahí.

---

## Build Settings

El menú se añadió **como escena 0**, que es la que arranca el juego:

```
0  Assets/Game/Scenes/MainMenu.unity
1  Assets/Game/Scenes/SampleScene.unity
2  Assets/Game/Scenes/Level01.unity
3  Assets/Game/Scenes/Level02_Enzo.unity
```

Si se desordena, el menú `Chocoflan > 1 - Aplicar ajustes de proyecto` lo vuelve a
dejar así. `Chocoflan > 2b - Abrir menu de inicio` abre la escena.

`SampleScene` sigue en la lista de antes; si ya no se usa, se puede quitar.

---

## Import del arte de UI

Los seis archivos venían a **100 PPU** y `Max Size 2048`. Se pasaron a:

| Ajuste | Valor |
|---|---|
| Pixels Per Unit | 240 |
| Max Size | 4096 |
| Filter Mode | Bilinear |
| Mipmaps | activados |

Es el mismo criterio que el arte del nivel: no es pixel art, se dibuja a 240 px por
unidad y se ve al 50 %.

---

## Si falta arte

Cada sprite es opcional. El que quede sin asignar se dibuja como un rectángulo de
color con su etiqueta (`play`, `credits`, `settings`), así que el menú sigue
funcionando mientras llega el arte.
