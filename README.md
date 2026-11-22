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
| [`explorador-personajes`](explorador-personajes) | **Exámenes parciales 1 y 2**: explorador de personajes. La entrega del primero quedó en la etiqueta [`parcial-1`](https://github.com/FabriLasas/MovilesII/tree/parcial-1/parcial-1-explorador) | .NET MAUI + SQLite + xUnit + GitHub Actions |

Cada carpeta contiene el proyecto completo y compilable. `01-perfil-alumno`
incluye además el ejercicio de navegación ya integrado: servicios de navegación
y notificación, rutas centralizadas y pantalla de detalle con paso de
parámetros. Su documentación está en
[`NAVIGATION.md`](01-perfil-alumno/NAVIGATION.md).

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
https://github.com/FabriLasas/MovilesII/tree/main/explorador-personajes
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
