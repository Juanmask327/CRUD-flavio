using UsuarioApi.DTOs;

namespace UsuarioApi.Interfaces;

public interface IUsuarioService
{
    Task<PaginacionResponseDto<UsuarioResponseDto>> ObtenerTodosAsync(string? busqueda, int pagina, int tamanoPagina);
    Task<UsuarioResponseDto?> ObtenerPorIdAsync(int id);
    Task<UsuarioResponseDto> CrearAsync(CrearUsuarioDto dto);
    Task<bool> ActualizarAsync(int id, ActualizarUsuarioDto dto);
    Task<bool> EliminarAsync(int id);
}
