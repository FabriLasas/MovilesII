# Móviles II

Trabajos prácticos y exámenes de **Desarrollo de Aplicaciones Móviles 2**.

Cada actividad vive en su propia carpeta dentro de la rama `main`. Todas son
proyectos independientes: no comparten código entre sí.

---

## Actividades

| Carpeta | Actividad | Tecnología |
|---|---|---|
| [`01-perfil-alumno`](01-perfil-alumno) | Perfil del alumno, con ejercicio de navegación | .NET MAUI |
| [`02-agenda-contactos-consola`](02-agenda-contactos-consola) | Agenda de contactos, primera versión | Consola + SQLite |
| [`03-agenda-contactos-maui`](03-agenda-contactos-maui) | Agenda de contactos, versión final | .NET MAUI + SQLite |
| [`parcial-1-explorador`](parcial-1-explorador) | **Primer examen parcial**: explorador de personajes | .NET MAUI |

> `01-perfil-alumno` contiene únicamente los archivos de la raíz del proyecto.
> El resto del código (vistas, modelos y servicios) quedó fuera del repositorio
> al subirse la actividad en su momento.

---

## Organización

Las actividades se guardan **como carpetas en `main`**, no como ramas. De esa
manera el repositorio muestra el trabajo completo de una sola vez, el historial
queda en una única línea cronológica y se pueden comparar dos actividades sin
cambiar de rama.

Las ramas se reservan para su propósito real: trabajar en algo en paralelo
antes de integrarlo a `main`.

Para compartir una actividad puntual alcanza con enlazar su carpeta:

```
https://github.com/FabriLasas/MovilesII/tree/main/parcial-1-explorador
```

### Ramas anteriores

Las ramas `ParcialMobiles`, `perfil_alumno_navegacion` y `Parcial-I` conservan
el estado previo a esta reorganización. Se mantienen como respaldo y no reciben
cambios nuevos.

---

## Cómo ejecutar cualquiera de los proyectos

Requiere el **SDK de .NET 10**. Para los proyectos MAUI, además las cargas de
trabajo correspondientes.

```bash
cd <carpeta-de-la-actividad>
dotnet restore
dotnet build
```
