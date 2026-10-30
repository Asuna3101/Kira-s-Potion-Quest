# Publicar Kira's Potion Quest en itch.io

El juego se sube como **WebGL**: se juega en el navegador, sin que nadie descargue
nada. Es lo normal para un proyecto de clase y lo que mejor funciona para que el
profesor o un jurado lo prueben de un clic.

---

## 1. Antes de nada: ¿está el módulo de WebGL?

Unity no compila para web si no está instalado el módulo.

1. Abrí **Unity Hub** → pestaña **Installs**
2. En tu versión (6000.3.13f1) hacé clic en los **tres puntos** → **Add modules**
3. Marcá **WebGL Build Support** → Install

Si ya está marcado, no hay nada que hacer. Si falta, la compilación va a fallar
con un mensaje diciendo justo eso.

---

## 2. Dentro de Unity

En la barra de menús de arriba de la pantalla, menú **Chocoflan → Itch**:

| Paso | Qué hace |
|---|---|
| **1 - Ajustar proyecto para WebGL** | Deja la configuración lista. Una sola vez |
| **2 - Optimizar texturas para web** | *Opcional pero muy recomendable.* Ver abajo |
| **3 - Compilar WebGL** | Genera la build en `Builds/WebGL`. Tarda bastante |
| **4 - Abrir la carpeta de la build** | Atajo para encontrarla |

### Qué hace el paso 1

| Ajuste | Valor | Por qué |
|---|---|---|
| Compresión | **Gzip** | Archivos más pequeños |
| Decompression Fallback | **Activado** | **Imprescindible en itch** |
| Lienzo | 960 × 540 | Entra bien en una página de itch |
| Run In Background | Activado | El juego no se congela al salir del foco |
| Nombre | Kira's Potion Quest / Chocoflan | Lo que ve el jugador |

> **Lo del Decompression Fallback es el error más típico.** itch sirve los archivos
> sin la cabecera que le dice al navegador "esto viene comprimido". Sin el
> fallback, el juego se queda en la pantalla de carga para siempre. El proyecto
> estaba configurado con compresión Brotli y **sin** fallback, que es exactamente
> la combinación que falla.

### Sobre el paso 2 (texturas)

El arte pesa **53 MB en disco** y casi todo está a 4096 px. En una build web eso
se traduce en varios cientos de MB y una carga larguísima.

El paso 2 baja esos dibujos a 2048 px. **No se pierde nitidez visible**: el arte
está dibujado a 240 px por unidad y en pantalla se ve a 120, o sea al 50%, así que
2048 ya es el doble de lo que hace falta. A cambio, la build pesa cerca de la
cuarta parte.

Es reversible: se vuelve a poner Max Size 4096 en los sprites.

Aparte, hay tres archivos que no son del juego y conviene sacar de `Sprites/`
antes de compilar, porque se empaquetan igual:

- `Sprites/IMG_4367.JPG`
- `Sprites/UI/Mockup.JPG`
- `Sprites/Background/Nuevo` (6 MB)

---

## 3. Comprimir la build

Unity deja la build en `Builds/WebGL/`, con esta pinta:

```
Builds/WebGL/
  index.html
  Build/
  TemplateData/
```

En Finder, clic derecho sobre la carpeta **`WebGL`** → **Comprimir**.
Te deja un `WebGL.zip`.

> ⚠️ **`index.html` tiene que quedar en la raíz del zip.** Si comprimís la carpeta
> que contiene a `WebGL`, el `index.html` queda un nivel más abajo y itch no lo
> encuentra. Si al probarlo ves un listado de archivos en vez del juego, es esto.

---

## 4. En itch.io

1. Entrá en **itch.io** → arriba a la derecha, tu usuario → **Upload new project**
2. Rellená:

| Campo | Qué poner |
|---|---|
| **Title** | Kira's Potion Quest |
| **Short description** | Ayuda a Kira a llevar los ingredientes al caldero |
| **Classification** | Game |
| **Kind of project** | **HTML** ← importante |
| **Release status** | Prototype / In development |

3. En **Uploads**, subí `WebGL.zip`
4. Cuando termine, marcá la casilla **"This file will be played in the browser"**
5. En **Embed options**:
   - **Viewport dimensions**: `960` × `540`
   - Marcá **Mobile friendly** solo si lo probaste en móvil (el juego usa teclado,
     así que probablemente no)
   - Marcá **Fullscreen button** — recomendado, el juego se ve mejor a pantalla completa
6. **Community** → Comments, si queréis recibir opiniones
7. **Visibility & access**: empezá en **Draft** o **Restricted** para probarlo sin
   que sea público. Cuando funcione, pasalo a **Public**
8. **Save** y después **View page** para probarlo

---

## 5. La página

Vale la pena dedicarle diez minutos:

- **Cover image**: 630 × 500 px. El `Title.PNG` sobre el fondo del menú queda bien
- **Screenshots**: ya hay dos listas en `Sprites/UI/Preview_Level01.png` y
  `Preview_Level02.png`
- **Descripción**: el pitch del GDD — *"Kira's Potion Quest es un juego de puzzles
  donde tienes que utilizar hechizos para hacer que los ingredientes adecuados
  lleguen al caldero y completar la poción."*
- **Controles**: ponelos en la descripción, que si no nadie los adivina

```
Click            elegir ingrediente
ESPACIO (o Ctrl) + flechas    levitar
Click y mantener elegir / interactuar
Tab              cambiar de ingrediente
R                reiniciar nivel
Esc              volver al menú
```

- **Credits**: los cinco del equipo

---

## 6. Si algo falla

| Síntoma | Causa |
|---|---|
| Se queda cargando para siempre | Falta el **Decompression Fallback**. Paso 1 otra vez y recompilar |
| itch muestra una lista de archivos | El `index.html` no está en la raíz del zip. Ver §3 |
| "Failed to download file" | El zip se subió incompleto. Volvé a subirlo |
| Tarda muchísimo en cargar | No corriste el paso 2. Ver §2 |
| Se ve diminuto o cortado | Viewport mal en Embed options. Poné 960 × 540 |
| Unity no deja compilar a WebGL | Falta el módulo. Ver §1 |

Mirá siempre la **consola del navegador** (`Cmd + Opción + J` en Chrome): los
errores de WebGL salen ahí con bastante claridad.

---

## 7. Para actualizar

Repetís el paso 3, volvés a comprimir y en itch, en **Uploads**, usás el botón de
**reemplazar** del archivo que ya está (no subas uno nuevo al lado, que se acumulan).
