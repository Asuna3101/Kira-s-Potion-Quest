# Kira's Potion Quest — Nivel 1: medidas y layout

Hito 16/10/2026 — *Implementación y presentación de la mecánica principal*.
Equipo Chocoflan. Escena: `Assets/Game/Scenes/Level01.unity`.

> **Actualizado al documento de arte "KIRAS POTION CRAFT MEDIDAS".**
> El prototipo estaba montado a 480 × 320 / PPU 32. Ahora sigue las medidas con
> las que va a dibujar Melanie: **1920 × 1080, PPU 240, cámara ortográfica 4.5**.
> Lo que cambió está resumido en §1.1.

---

## 1. Sistema de medidas

Todo el juego se mide en **tiles**. `GridSettings.cs` es el único sitio que traduce
tiles a unidades de mundo, así que cambiar una constante ahí recoloca el nivel entero.

| Dato | Valor | De dónde sale |
|---|---|---|
| Resolución de juego | **1920 × 1080 (16:9)** | documento de arte |
| `Camera.orthographicSize` | **4.5** | documento de arte |
| Mundo visible | **16 × 9 u** | 4.5 × 2 = 9 de alto, × 16/9 de ancho |
| Pixels Per Unit (import) | **240** | documento de arte |
| Píxeles de arte por unidad | **240** | lienzo 3840 × 2160 con cuadrícula de 240 px |
| Píxeles de pantalla por unidad | **120** | 1920 / 16 → el arte se ve al **50 %** |
| Celda de diseño | **1 × 1 u = 240 × 240 px** | "bloque / plataforma / obstáculo" |
| Cuadrícula de diseño | **16 × 9 celdas** | 16 u × 9 u |
| Tile de layout | **0.5 u = 120 px** | "o el doble para detalles finos" |
| Cuadrícula de layout | **32 × 18 tiles** | 16×2 y 9×2 |
| Rango de x | **−8 … 8** | |
| Rango de y | **−4.5 … 4.5** | |
| Marco de la pantalla | **1 u de grosor** (2 tiles) | mockup |

**Por qué dos cuadrículas.** El arte se dibuja contra la de **1 u** (un bloque = una
celda). El nivel se *coloca* en la de **0.5 u**, que es "el doble para detalles
finos" del documento: hace falta porque hay piezas de media celda o menos (pinchos
de 1 × 0.5, botón de 1 × 0.3) y si no no se pueden apoyar en el suelo sin romper la
cuadrícula de 1 u.

### 1.1 Qué cambió respecto de la versión anterior

| | Antes | Ahora |
|---|---|---|
| Resolución | 480 × 320 (3:2) | 1920 × 1080 (16:9) |
| PPU | 32 | 240 |
| `orthographicSize` | 5 | 4.5 |
| Mundo | 15 × 10 u | 16 × 9 u |
| Grilla | 30 × 20 tiles | 32 × 18 tiles |
| Tile | 0.5 u (16 px) | 0.5 u (120 px) |
| Ingrediente | 0.4 u | **0.8 u** |
| Caldero | 1.5 × 1.5 u | **2 × 1.6 u**, boca 1.2 u |
| Pixel Perfect Camera | activada | **desactivada** (ver §1.4) |
| Import de sprites | Point, sin mipmaps | **Bilinear, con mipmaps** |

El tile sigue midiendo 0.5 u, así que la lógica del mapa ASCII no cambió: solo
cambió cuánto mide una unidad en píxeles y el encuadre de la cámara.

### 1.2 Fórmulas

```
centro del tile (col, row):   x = -8.0 + (col + 0.5) * 0.5
                              y = -4.5 + (row + 0.5) * 0.5

centro de un rect de tiles:   x = -8.0 + (col + ancho/2) * 0.5
                              y = -4.5 + (row + alto/2)  * 0.5

celda de diseño (1 u):        col de celda = col de tile / 2
```

Tile **(0, 0) es la esquina inferior izquierda** de la pantalla.
Tile (31, 17) es la superior derecha.

### 1.3 Tamaños de los elementos (documento de arte)

| Elemento | Unidades | A 240 PPU | Constante en `GridSettings` |
|---|---|---|---|
| Bloque / plataforma / muro | 1 × 1 | 240 × 240 | `CellSize` |
| Ingrediente | **0.8** (rango 0.7–0.9) | ~192 × 192 | `IngredientSize` |
| Caldero | 2 × 1.6 | 480 × 385 | `CauldronWidth` / `CauldronHeight` |
| Boca del caldero (trigger) | 1.2 de ancho | 288 | `CauldronMouthWidth` |
| Pinchos (módulo repetible) | 1 × 0.5 | 240 × 120 | `SpikeHeight` |
| Botón | 1 × 0.3 | 240 × 72 | `ButtonHeight` |
| Palanca | 1 × 1 | 240 × 240 | `CellSize` |
| Portal | 1 × 1.5 | 240 × 360 | `PortalWidth` / `PortalHeight` |
| Kira | 1.5 × 2 | 360 × 480 | `KiraWidth` / `KiraHeight` |
| Corriente de aire | variable, tileable | — | el collider es invisible |
| Punto de trayectoria | Ø 0.15 | 36 | `ThrowController.previewDotSize` |
| Arrastre máx. al lanzar | 1.8 | — | `ThrowController.maxDrag` |

El ingrediente mide 0.8 **a propósito**: menos de 1 u para que pase por los huecos
de la cuadrícula, como dice el documento de arte.

### 1.4 Cámara: por qué la Pixel Perfect Camera queda apagada

Con las medidas nuevas el juego **ya no es pixel art**. El arte se dibuja a 240 px
por unidad y se ve a 120 px por unidad: es *HD 2D* con supersampling al 50 %.

La Pixel Perfect Camera exige `orthographicSize = refResY / (2 × PPU)`. Con PPU 240
y Size 4.5 eso da una resolución de referencia de **3840 × 2160**, que en una
pantalla de 1080p es un escalado de **0.5×** — no entero, justo lo que la Pixel
Perfect Camera no sabe resolver. Además el ajuste al píxel haría "saltar" a los
ingredientes, que se mueven de forma continua al levitar.

Por eso el componente sigue en `Main Camera` pero **desactivado**, y
`PixelPerfectSetup.usePixelPerfect` está en `false`. Si el equipo decidiera volver
a pixel art, se marca ese check y queda configurado solo (y con el componente
correcto de URP: `UnityEngine.Rendering.Universal.PixelPerfectCamera`; **no**
instalar `com.unity.2d.pixel-perfect`, que es el de Built-in).

**Lo que sí importa ahora** es el import de los sprites: **Filter Mode Bilinear** y
**mipmaps activados**, que es lo contrario de lo que pediría el pixel art. Está en
el menú `Chocoflan > Opcional - Reimportar sprites a 240 PPU`.

El HUD va en un Canvas **Screen Space - Overlay** con Canvas Scaler a
**1920 × 1080**, match por altura.

---

## 2. Layout del Nivel 1 — grilla 32 × 18

El mapa vive en `Level01Data.MapTopDown` y `LevelBuilder` lo construye. **Este
diagrama es literalmente el código**: si se edita el mapa, cambia el nivel.

```
      0         1         2         3
      01234567890123456789012345678901
 17   ################################
 16   ################################
 15   ##.K....................GG....##   <- Kira (decorativa, sobre el marco)
 14   ##......................GG....##
 13   ##......AAA.............GG....##   <- hueco de 2 u: por aquí se pasa el muro
 12   ##.E....AAA.............GG....##   <- huevo en el estante alto
 11   ##======AAA......##.....GG....##   <- estante alto (3 x 1 u)
 10   ##======AAA......##.2...GG....##   <- portal B
  9   ##......AAA......##.....GG....##
  8   ##......AAA.1....##.....GG....##   <- portal A
  7   ##.M....AAA......##.....GG....##   <- hongo en el estante bajo
  6   ##======AAA......##.....GG....##   <- estante bajo (3 x 1 u)
  5   ##======AAA......##.....GGCCCC##   <- caldero
  4   ##......AAA......##.....GGCCCC##
  3   ##......AAA......##.....GGCCCC##
  2   ##......AAA^^^^..##^^^bbGGCCCC##   <- pinchos / botón / compuerta
  1   ################################
  0   ################################
```

El marco (filas 0-1 y 16-17, columnas 0-1 y 30-31) mide **1 u** de grosor, como en
el mockup. El interior jugable son **14 × 7 u**.

### Leyenda

| Char | Elemento | Comportamiento |
|---|---|---|
| `#` | Pared / marco | Estático, obstruye |
| `=` | Estante | Estático, obstruye |
| `^` | Pinchos | Trigger, **destruye** el ingrediente |
| `*` | Fuego | Trigger, destruye (**no usado en el Nivel 1**) |
| `A` | Corriente de aire | Trigger, empuja hacia arriba |
| `1` / `2` | Par de portales | Teletransportan conservando la velocidad |
| `b` | Botón de presión | Se activa al contacto con un ingrediente |
| `L` | Palanca | Click y mantener (**no usada en el Nivel 1**) |
| `G` | Compuerta | Cerrada bloquea; el botón la abre |
| `C` | Caldero | Meta |
| `E` | Huevo | Ingrediente **frágil** |
| `M` | Hongo | Ingrediente **resistente** |
| `K` | Kira | Decorativa, sin colisión |

### Objetos resultantes

Los tiles contiguos del mismo tipo se fusionan en un solo GameObject con un solo
collider. Los elementos cuyo tamaño lo fija el documento de arte (caldero, botón,
portales, ingredientes, Kira) se construyen a ese tamaño exacto y se **apoyan** en
el rectángulo que los marca en el mapa, no lo rellenan.

| Elemento | Tiles (col, row) | Tiles | Centro real (u) | Tamaño real (u) |
|---|---|---|---|---|
| Suelo `#` | (0, 0) | 32 × 2 | (0, −4) | 16 × 1 |
| Marco izquierdo `#` | (0, 2) | 2 × 16 | (−7.5, 0.5) | 1 × 8 |
| Marco superior `#` | (2, 16) | 28 × 2 | (0, 4) | 14 × 1 |
| Marco derecho `#` | (30, 2) | 2 × 16 | (7.5, 0.5) | 1 × 8 |
| **Muro central** `#` | (17, 2) | 2 × 10 | (1, −1) | 1 × 5 |
| Estante bajo `=` | (2, 5) | 6 × 2 | (−5.5, −1.5) | 3 × 1 |
| Estante alto `=` | (2, 10) | 6 × 2 | (−5.5, 1) | 3 × 1 |
| Corriente de aire `A` | (8, 2) | 3 × 12 | (−3.25, −0.5) | 1.5 × 6 |
| Pinchos izquierda `^` | (11, 2) | 4 × 1 | (−1.5, −3.25) | 2 × 0.5 |
| Pinchos derecha `^` | (19, 2) | 3 × 1 | (2.25, −3.25) | 1.5 × 0.5 |
| **Botón** `b` | (22, 2) | 2 × 1 | (3.5, −3.35) | **1 × 0.3** |
| **Compuerta** `G` | (24, 2) | 2 × 14 | (4.5, 0) | 1 × 7 |
| **Caldero** `C` | (26, 2) | 4 × 4 | (6, −2.7) | **2 × 1.6** |

| Elemento de 1 tile | Tile (col, row) | Centro (u) | Tamaño (u) |
|---|---|---|---|
| **Huevo** `E` | (3, 12) | (−6.25, 1.9) | 0.8 — frágil, sobre el estante alto |
| **Hongo** `M` | (3, 7) | (−6.25, −0.6) | 0.8 — resistente, estante bajo |
| **Portal A** `1` | (12, 8) | (−1.75, −0.25) | 1 × 1.5, ciano |
| **Portal B** `2` | (20, 10) | (2.25, 0.75) | 1 × 1.5, magenta |
| Kira `K` | (3, 15) | (−6.25, 3.25) | 1.5 × 2, decorativa sobre el marco |

Los ingredientes se apoyan en la **base** del tile que los marca, no en su centro:
miden 0.8 u y el tile 0.5 u, así que centrarlos los dejaría medio hundidos en el
estante.

El caldero **no se abre por dentro**: como dice el documento de arte, los
ingredientes "nunca entran, solo chocan con su collider". El trigger es solo la
**boca**, de 1.2 u de ancho, colocada en la parte de arriba del sprite.

### Solución esperada

1. Click en el **huevo** (3, 12) para seleccionarlo.
2. **Espacio + flechas**: salir del estante y subir. La **corriente de aire** de las
   columnas 8-10 sube gratis, sin gastar maná.
3. Pasar al otro lado del **muro central** (columnas 17-18): por encima, usando el
   hueco de 2 u de las filas 12-15, o por el **portal A** (12, 8), que sale por el
   **B** (20, 10) ya del otro lado.
4. Bajar y **tocar el botón** (22-23, 2), cuidando los pinchos de las columnas
   19-21. La compuerta de las columnas 24-25 se abre y queda abierta.
5. Levitar hasta la **boca del caldero** (26-29, 2-5).
6. Repetir con el **hongo** (3, 7). La compuerta ya está abierta: segunda vuelta más
   fácil, curva de dificultad suave.

Caer sobre los pinchos (columnas 11-14 o 19-21, fila 2) mata al huevo de un toque;
la compuerta impide ganar sin pasar por el botón.

---

## 3. Controles

| Acción | Control | Estado |
|---|---|---|
| Seleccionar ingrediente | Click izquierdo sobre él | ✅ |
| Cambiar de ingrediente | `Tab` | ✅ |
| **Levitar (mecánica principal)** | **`Espacio` + flechas** (también acepta `Ctrl`) | ✅ |
| Flotar quieto | `Espacio` sin flechas | ✅ |
| Interactuar | Click y **mantener** sobre caldero / portales / palanca | ✅ |
| **Lanzar** (*por alcance* en el GDD) | Click sobre el ingrediente y **arrastrar hacia atrás**, soltar | ✅ (desactivable) |
| Reiniciar nivel | `R` | ✅ |

La tecla de levitar es **Espacio**, no `Ctrl` como dice el GDD: `Ctrl` + flechas
choca con atajos del sistema (en macOS `Ctrl`+flecha cambia de escritorio) y
espacio es más cómodo de mantener mientras la otra mano está en las flechas.
`Ctrl` sigue funcionando como alias. Se cambia en el Inspector:
`--- GAME --- > LevitationController > Levitate Key` (y el check `Also Accept Ctrl`).

Levitar consume **maná** (barra abajo a la izquierda): 100 de máximo, −16/s
mientras se levita, +25/s cuando no. No es una condición de derrota nueva: si se
agota, el ingrediente simplemente cae y el maná se recupera.

El proyecto usa **solo el Input System nuevo** (`activeInputHandler: 1`), así que
todo el input va por `Keyboard.current` / `Mouse.current`. `Input.GetKey` **no
funcionaría** en este proyecto.

---

## 4. Victoria y derrota (GDD)

- **Victoria**: todos los ingredientes (2 en el Nivel 1) dentro del caldero.
- **Derrota**: un ingrediente es destruido.
  - Pinchos y fuego destruyen a cualquiera.
  - El **huevo** además se rompe con impactos ≥ 10 u/s (≈ caída libre de 8.5 u,
    casi la pantalla completa). El **hongo** no se rompe por impacto.

---

## 5. Arte y animaciones placeholder

Todo el arte del nivel se **genera por código** en `PlaceholderArt.cs`: formas y
colores legibles, con las dimensiones exactas que manda el documento de arte.
Ventaja: nada se ve rosa ni invisible y no depende del arte final.

Los placeholders se generan a **32 px por tile** (64 PPU), no a los 240 PPU reales,
porque a tamaño real el fondo de pantalla completa sería una textura de
3840 × 2160 creada en tiempo de ejecución. El tamaño **en el mundo** sale idéntico
porque el PPU del sprite se ajusta en la misma proporción. Esto no afecta al arte
final de Melanie, que sí va a 240 PPU.

Las animaciones son `PlaceholderAnimator.cs` (sin Animator ni clips): `Bob`,
`Pulse`, `Spin`, `Flicker`, `ScrollUp`, `ShrinkAndFade`.

### Cómo sustituir por el arte de Melanie

En el Inspector de `--- GAME --- > LevelBuilder > Art` hay un hueco por elemento
(`wall`, `shelf`, `spikes`, `fire`, `airCurrent`, `portalA`, `portalB`, `button`,
`lever`, `gate`, `cauldron`, `ingredientEgg`, `ingredientMushroom`, `kira`,
`background`). Al arrastrar un sprite ahí, `LevelBuilder` lo usa y **escala el
objeto al tamaño exacto que le toca en tiles**, sin tocar código y sin importar a
qué PPU esté importado el PNG.

Para las animaciones: quitar `PlaceholderAnimator` del objeto y ponerle un
`Animator`. Ningún otro script depende de `PlaceholderAnimator`.

---

## 6. Decisiones de diseño tomadas aquí

Cosas que el GDD no cerraba y se resolvieron para poder entregar hoy:

1. **Dos cuadrículas: 16 × 9 celdas de 1 u para el arte, 32 × 18 tiles de 0.5 u
   para colocar el nivel.** El documento de arte dice "16 × 9 o el doble para
   detalles finos" y el doble es necesario: los pinchos (1 × 0.5) y el botón
   (1 × 0.3) no se pueden apoyar en el suelo sin media celda. El arte se sigue
   dibujando contra la cuadrícula de 1 u.
2. **Maná de levitación.** Sin un coste, levitar directo al caldero hace trivial
   cualquier puzzle. Se recupera solo, así que no añade una derrota nueva (el GDD
   solo contempla derrota por ingrediente destruido).
3. **Gravedad por `gravityScale = 0.6`** en los ingredientes en vez de bajar la
   gravedad global de Physics2D. Mismo efecto (≈ −5.9), sin tocar ProjectSettings
   ni afectar a otras escenas.
4. **El fuego no se coloca en el Nivel 1.** Es el primer nivel y debe enseñar, no
   castigar: basta con un peligro (pinchos). El componente `Hazard` ya soporta
   fuego; se activa poniendo `*` en el mapa.
5. **La palanca tampoco se coloca en el Nivel 1**, por lo mismo. `Lever.cs` está
   implementado y se activa poniendo `L` en el mapa.
6. **El botón es *latching***: una vez pisado, la compuerta queda abierta. En un
   tutorial, que se vuelva a cerrar sería frustrante. `PressurePlate.latching`
   lo controla.
7. **Dos ingredientes, uno frágil y uno resistente**, para mostrar en vivo la
   frase del GDD "algunos ingredientes son más frágiles que otros".
8. **Levitar con `Espacio` en vez de `Ctrl`.** El GDD dice `Ctrl` + flechas, pero
   esa combinación choca con atajos del sistema operativo. `Ctrl` se mantiene como
   alias para no contradecir el documento. Si se cambia en el GDD, la tecla es un
   campo del Inspector.
9. **`Lanzar` quedó implementado y activado** (`GameManager.enableThrow`) aunque
   el GDD lo marca "por alcance", porque Levitar quedó sólido. Se desactiva con un
   check si se prefiere presentar solo la mecánica principal.
10. **El nivel se construye por código desde el mapa ASCII**, no a mano en la
   escena. Así el nivel jugado y este documento no pueden desincronizarse.
11. **Los tiles contiguos se fusionan**: el nivel entero son 13 colliders en vez
    de ~200.

---

## 7. Qué falta hacer a mano en el Editor de Unity

Lo que no se puede hacer desde fuera del Editor:

1. **Menú `Chocoflan > 1 - Aplicar ajustes de proyecto`.**
   Pone la resolución por defecto del Player en 1920 × 1080, Anti-Aliasing a 0,
   Anisotropic Filtering off, el snap del editor a 1/240 (un píxel de arte) y añade
   `Level01` a Build Settings. *(Hay que hacerlo desde el menú y no editando
   ProjectSettings a mano: si el Editor está abierto, lo sobreescribiría.)*
2. **Game view** → ponerla en **16:9** o en la resolución fija **1920 × 1080**, para
   ver el encuadre real.
3. **Abrir `Assets/Game/Scenes/Level01.unity`** (o menú `Chocoflan > 2`) y **Play**.
   El nivel se construye solo al entrar en Play.
4. *(Opcional)* **`Chocoflan > 3 - Hornear Nivel 1 en la escena`** para que los
   GameObjects existan de verdad en la jerarquía y se puedan tocar en el Inspector.
   `Chocoflan > 4` los limpia.
5. *(Opcional)* **`Chocoflan > 5 - Exportar layout`** regenera
   `Docs/Nivel01_Layout.md` desde el mapa, con las coordenadas recalculadas.
6. *(Opcional, destructivo)* **`Chocoflan > Opcional - Reimportar sprites a 240
   PPU`**: pasa los PNG de `Assets/Game/Sprites` a **240 PPU, filtro Bilinear y
   mipmaps**, que es lo que pide el documento de arte. Cambia cómo se ve
   `SashasScene`, por eso no se ejecuta solo.
7. **`SashasScene` sigue con el bug de la luz** (sprites con material Sprite-Lit y
   ninguna `Light2D` en la escena → todo negro). `Level01` no lo tiene: trae
   `Global Light 2D` y además los placeholders usan material **unlit**.
8. **`_ScriptsDescartados/`** (en la raíz del proyecto, fuera de `Assets/`): había
   un intento anterior de scripts a medio hacer (`GridMetrics`, `ProceduralSprites`,
   `SpriteFitter`, `Spikes`, `FireHazard`, `IInteractable`, `Art/*`) que **no
   compilaba** — llamaba a métodos de `Ingredient` que no existen (`TakeHit`,
   `IsDeposited`, `levitationResponse`). Se movió ahí para no perderlo sin romper
   la compilación. Si no se va a rescatar, se puede borrar la carpeta.
