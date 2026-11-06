# Audio del juego

Todos los sonidos pasan por un solo componente: **AudioDirector**.

En cada escena está puesto en el objeto de arriba de la jerarquía:

| Escena | Objeto |
|---|---|
| MainMenu | `--- MENU ---` |
| LevelSelect | `--- SELECCION ---` |
| Level01 / Level02 | `--- GAME ---` |

## Cómo cambiar un sonido

1. Selecciona ese objeto en la jerarquía.
2. En el Inspector, busca **Audio Director**.
3. Arrastra el `.wav` nuevo al hueco que corresponda.

**No hay que tocar código.** Los huecos son casillas del Inspector, igual que los sprites.

## Qué suena y cuándo

| Hueco del Inspector | Archivo | Cuándo suena |
|---|---|---|
| Musica | `snd_backgroundmusicmenu` | fondo del menú y de selección de nivel (en bucle) |
| Musica | `snd_backgroundmusiclevel` | fondo de los niveles (en bucle) |
| Empezar Nivel | `snd_startlevel` | al entrar al nivel |
| Lanzar | `snd_lanzar` | al soltar un ingrediente lanzado |
| Caldero | `snd_caldero` | el ingrediente entra al caldero |
| Romper | `snd_romper` | el ingrediente se rompe: pinchos o caída |
| Quemar | `snd_fire` | el ingrediente se quema en el fuego |
| Ganar | `snd_win` | con el libro de nivel superado |
| Perder | `snd_perder` | con el cartel de nivel fallido |
| Boton | `snd_boton` | cualquier botón del menú, selección de nivel o el botón de volver |

## Volumen

Dos deslizadores en el mismo componente:

- **Volumen Musica** — la música de fondo (por defecto 0.40 en niveles, 0.45 en menús)
- **Volumen Efectos** — todo lo demás (0.85)

**Fundido Entrada** son los segundos que tarda la música en subir al entrar a la escena, para que no entre de golpe.

**Sonar Al Empezar** está activado en los niveles y desactivado en los menús. Si lo activas en el menú, el sonido de inicio de nivel también suena ahí.

## Detalles técnicos

- Los efectos se oyen aunque el nivel esté congelado al ganar o perder (`ignoreListenerPause`), así que el sonido de victoria no se corta.
- Los `.wav` están configurados como **sonido 2D** y en **Vorbis**, con la calidad bajada, para que la build de itch no crezca de más. Los efectos además van en mono.
- Si una escena no tiene AudioDirector, el juego funciona igual: simplemente no suena. Ningún script se rompe por eso.
