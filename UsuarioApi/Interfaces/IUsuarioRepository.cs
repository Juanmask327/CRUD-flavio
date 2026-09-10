using UsuarioApi.Entities;

namespace UsuarioApi.Interfaces;

public interface IUsuarioRepository
{
    Task<(List<Usuario> Usuarios, int Total)> ObtenerTodosAsync(string? busqueda, int pagina, int tamanoPagina);
    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task<Usuario?> ObtenerPorCorreoAsync(string correo);
    Task<Usuario> CrearAsync(Usuario usuario);
    Task ActualizarAsync(Usuario usuario);
    Task EliminarAsync(Usuario usuario);
}
