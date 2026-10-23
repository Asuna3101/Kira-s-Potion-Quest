# Kira's Potion Quest — Guía para Arte y Diseño

Esta guía es para **Melanie y Sasha**. No hay que saber programar ni escribir una
línea de código: todo lo que necesitan hacer es arrastrar archivos y hacer clic.

Si algo de acá no funciona como dice, avisen a Andrea en vez de pelearse con
Unity. Casi siempre es un ajuste de dos minutos.

---

## 1. Lo mínimo, en diez líneas

- El juego mide **16 × 9 unidades** en pantalla. Una "unidad" es la medida interna
  del juego: **1 unidad = 240 píxeles de dibujo**.
- Melanie dibuja en un lienzo de **3840 × 2160 px** con una cuadrícula de **240 px**.
  Cada cuadradito de esa cuadrícula es **1 unidad**.
- Un bloque o pared ocupa **1 cuadradito**. El caldero, **2 de ancho por 1.6 de alto**.
- Cada elemento se exporta **en el lienzo completo, en el sitio donde va**, con el
  resto transparente. Eso ya lo hiciste así y funcionó perfecto: seguí igual.
- Para cambiar un dibujo del juego: **arrastrarlo a su hueco en Unity**. Nada más.

---

## 2. Cómo exportar el arte

Esta es la parte más importante de la guía, porque si el arte sale con estas
medidas, entra al juego sin que nadie tenga que retocar nada.

### La regla

```
Lienzo:      3840 × 2160 px
Cuadrícula:  240 px
1 cuadradito de la cuadrícula = 1 unidad del juego
```

### Cómo exportar cada elemento

1. Armá **toda la escena** en el lienzo: las paredes donde van, el caldero donde
   va, el ingrediente donde va.
2. Exportá **un PNG por elemento**, cada uno del lienzo completo (3840 × 2160),
   con ese elemento en su sitio y **todo lo demás transparente**.
3. Nombre claro: `Pared_grande.PNG`, `Caldero.PNG`, `Vela_1.PNG`.

**Por qué el lienzo completo.** Así el juego sabe dónde va cada cosa sin que nadie
lo coloque a mano: lee la posición del propio dibujo. Si exportaras cada elemento
recortado, habría que reubicarlos uno por uno en Unity.

> ⚠️ **El fondo y el marco sí o sí en archivos separados.** `Tablero_fondo` va
> detrás de todo y `Tablero_encima` (el marco de madera) va delante. Si vinieran
> en el mismo archivo no se podría poner al personaje entre medio.

### Tamaño de cada elemento

| Elemento | Unidades | A 240 px |
|---|---|---|
| Bloque / pared / plataforma | 1 × 1 | 240 × 240 |
| Ingrediente | entre 0.7 y 0.9 | ~190 × 190 |
| Caldero | 2 × 1.6 | 480 × 385 |
| Pinchos (módulo repetible) | 1 × 0.5 | 240 × 120 |
| Botón | 1 × 0.3 | 240 × 72 |
| Palanca | 1 × 1 | 240 × 240 |
| Portal | 1 × 1.5 | 240 × 360 |
| Kira | 1.5 × 2 | 360 × 480 |
| Vela / fuego | 0.5 × 1.5 | 120 × 360 |
| Corriente de aire | libre, dibujo repetible | — |

El **ingrediente tiene que medir menos de 1 unidad** para que pase por los huecos
entre bloques. Si se dibuja más grande, se traba y el nivel no se puede resolver.

### Qué está fuera de medida ahora mismo

Medí el arte entregado contra esta tabla. Las paredes y las velas están
perfectas (±8%, que es el margen normal del trazo a mano). Tres se pasan:

| Elemento | Dibujado | Debería | |
|---|---|---|---|
| Caldero | 2.21 × 1.83 | 2 × 1.6 | 14% más alto |
| Kira | 2.63 × 2.34 | 1.5 × 2 | 76% más ancha |
| Huevo | 0.73 × 1.01 de alto | máximo 0.9 | un poco alto |

Ninguno rompe el juego ahora mismo, pero conviene decidir: **o se redibujan a la
medida, o actualizamos la tabla a lo que ya está dibujado.** Lo que no sirve es
dejar la tabla diciendo una cosa y el arte otra, porque el próximo nivel se va a
dibujar contra la tabla.

Detalle del huevo: el PNG mide 1.26 × 1.35 porque **incluye el resplandor
amarillo**. Eso está bien y es bonito; el juego usa solo el huevo de adentro para
las colisiones. Solo téngalo en cuenta al medir.

---

## 3. Cómo reemplazar un dibujo en el juego

Esto es todo. No hay que tocar código.

1. Poné el PNG nuevo en `Assets/Game/Sprites/` (en la subcarpeta que corresponda:
   `Obstacles`, `Character`, `Ingredients`, `Background`, `UI`).
2. En Unity, en el panel de la izquierda (**Hierarchy**), hacé clic en
   **`--- GAME ---`**.
3. En el panel de la derecha (**Inspector**) buscá el bloque **`Level Builder`**.
   Vas a ver listas de huecos con nombres: `pared Grande`, `caldero`, `huevo`…
4. **Arrastrá tu PNG desde abajo (Project) hasta el hueco que le toca.**

Listo. Al darle al botón de Play, tu dibujo está en el juego.

### Hay tres listas de huecos, ¿cuál uso?

| Lista | Para qué |
|---|---|
| **`Melanie Art`** | **Esta.** El arte final del Nivel 1 |
| `Placeholders` | Los dibujos provisionales de relleno |
| `Art` | Arte del nivel de prueba de Enzo |

El juego usa primero lo que haya en `Melanie Art`. Si un hueco está vacío, pone el
placeholder, para que nunca quede un objeto invisible.

### Los placeholders

Están en `Assets/Game/Sprites/PlaceHolders/Generados/` y se llaman `PH_Wall`,
`PH_Cauldron`, `PH_Spikes`, etc. Son dibujos feos a propósito —rectángulos de
colores— pero **tienen exactamente las medidas correctas** de la tabla de arriba.

Sirven de referencia: si querés saber cuánto tiene que medir un portal, abrí
`PH_PortalA.png` y miralo.

---

## 4. Cómo ver el juego sin molestar a nadie

### Abrir las escenas

En la barra de menús **de arriba de todo de la pantalla** (la del Mac, donde está
la manzanita — **no** la barra de adentro de la ventana de Unity), hay un menú que
se llama **Chocoflan**, entre `Component` y `Window`.

| Opción | Qué hace |
|---|---|
| **2 - Abrir escena Nivel 1** | Abre el nivel |
| **2b - Abrir menu de inicio** | Abre la pantalla de título |
| **3 - Hornear Nivel 1 en la escena** | Hace que los objetos del nivel aparezcan en la lista de la izquierda, para moverlos a mano |

Después de usar la **3**, guardá con **`Cmd + S`**. Si no guardás, se pierde.

### Mover cosas a mano

Una vez horneado el nivel, en el panel de la izquierda aparecen `Fondo`,
`Tablero`, `Obstaculos`, `Ingredientes`, `Decoracion`. Abriendo esas carpetitas
están las paredes, las velas, el caldero y el huevo.

Hacé clic en cualquiera y movelo con el ratón en la ventana del medio. Lo que
muevas es lo que se juega.

### Probar

El botón **▶ (Play)** arriba en el centro. Para salir, el mismo botón otra vez.

> ⚠️ **Lo que cambies mientras está en Play se pierde al salir.** Es la trampa
> clásica de Unity. Movelo con el Play apagado.

---

## 5. Qué conviene no tocar

No se rompe nada que no tenga arreglo —todo está en git— pero para ahorrar vueltas:

| No tocar | Por qué |
|---|---|
| La carpeta `Scripts` | Es el código |
| El tamaño de la cámara (`Size 4.5`) | Define el encuadre de 16 × 9 |
| El `Pixels Per Unit` de un sprite | Tiene que ser **240** en todos |
| Los objetos que terminan en `Borde_` | Son las paredes invisibles |
| Borrar un objeto `--- GAME ---` o `--- MENU ---` | Son los que hacen funcionar la escena |

Si algo se rompió y no sabés qué tocaste: **no guardes**, cerrá la escena sin
guardar y volvé a abrirla. Vuelve como estaba.

---

## 6. Diccionario Unity → castellano

| Dicen | Es |
|---|---|
| **Sprite** | Un dibujo |
| **Asset** | Un archivo del proyecto |
| **Hierarchy** | La lista de la izquierda: lo que hay en la escena |
| **Inspector** | El panel de la derecha: las propiedades de lo seleccionado |
| **Project** | El panel de abajo: las carpetas |
| **Scene** | La ventana donde se edita |
| **Game** | La ventana donde se ve como lo verá el jugador |
| **Escena** | Una pantalla del juego (el menú, el nivel 1…) |
| **Unidad** | La medida interna. 1 unidad = 240 px de dibujo |
| **PPU** (Pixels Per Unit) | Cuántos píxeles de dibujo entran en una unidad. Siempre **240** |
| **Collider** | La forma invisible con la que chocan las cosas |
| **Hornear** | Convertir algo generado por código en objetos de verdad, editables |
| **Placeholder** | Dibujo provisional de relleno |

---

## 7. Las escenas que hay

| Escena | Qué es |
|---|---|
| `MainMenu` | Pantalla de título: fondo, logo y los tres botones |
| `Level01` | El nivel con el arte de Melanie |
| `Level02_Enzo` | Nivel de pruebas de Enzo, con placeholders |
| `SampleScene`, `SashasScene` | Viejas, de los primeros días |

> `SashasScene` tiene un problema conocido: los dibujos se ven negros porque le
> falta una luz. `Level01` y `MainMenu` no lo tienen.

---

## 8. Si algo sale mal

| Síntoma | Qué hacer |
|---|---|
| Un dibujo se ve **borroso** | Su `Pixels Per Unit` no es 240. Avisá a Andrea |
| Un dibujo se ve **gigante o diminuto** | Lo mismo |
| Todo **negro** en la escena | Es `SashasScene`. Abrí `Level01` |
| Un objeto **rosa fuerte** | Le falta el dibujo. Revisá que el hueco del Inspector no esté vacío |
| La escena se ve **vacía** | Faltó hornear: `Chocoflan > 3`, y después `Cmd + S` |
| **No encuentro el menú Chocoflan** | Está en la barra de arriba de todo de la pantalla, no en la ventana de Unity |
| Unity pregunta si **guardar** y no sé | Si no querías cambiar nada, decí **No guardar** |

---

## 9. Para el documento de diseño

Datos que conviene tener a mano al escribir el GDD:

- Resolución: **1920 × 1080 (16:9)**, estándar PC
- Mundo visible: **16 × 9 unidades**
- Mecánica principal: **Levitar** — click para elegir un ingrediente, `Espacio`
  (o `Ctrl`) + flechas para moverlo por el aire
- Mecánica opcional: **Lanzar** — arrastrar con el ratón y soltar
- Interactuar: **click y mantener** sobre un elemento
- Victoria: todos los ingredientes al caldero
- Derrota: un ingrediente destruido
- El Nivel 1 hoy tiene: dos plataformas, dos velas, un huevo y el caldero.
  Portales, botón, compuerta y corriente de aire **están programados pero sin
  arte**, por eso no están en el nivel. En cuanto haya dibujos, entran.

---

*Guía mantenida por Andrea. Detalle técnico en `Docs/Nivel01_Medidas.md` y
`Docs/MenuInicio.md`.*
