using SQLite;

namespace ExploradorPersonajes.Tests.Dobles;

/// <summary>
/// Base SQLite en memoria para las pruebas de integración: es SQLite real
/// (mismas consultas, mismas restricciones que en el teléfono), pero no
/// escribe ningún archivo y desaparece al cerrarse.
///
/// Cada instancia usa un nombre único. Con ":memory:" a secas no alcanza:
/// sqlite-net reutiliza la conexión para el mismo nombre, y dos pruebas que
/// corrieran en paralelo terminarían compartiendo la misma base.
/// </summary>
public sealed class BaseEnMemoria : IAsyncLifetime
{
    public SQLiteAsyncConnection Conexion { get; } = new(
        $"file:prueba-{Guid.NewGuid():N}?mode=memory&cache=shared",
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.Uri);

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Conexion.CloseAsync();
}
