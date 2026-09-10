using UsuarioApi.DTOs;
using UsuarioApi.Entities;
using UsuarioApi.Interfaces;

namespace UsuarioApi.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repository;

    public UsuarioService(IUsuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginacionResponseDto<UsuarioResponseDto>> ObtenerTodosAsync(string? busqueda, int pagina, int tamanoPagina)
    {
        pagina = pagina < 1 ? 1 : pagina;
        tamanoPagina = tamanoPagina is < 1 or > 100 ? 10 : tamanoPagina;

        var (usuarios, total) = await _repository.ObtenerTodosAsync(busqueda, pagina, tamanoPagina);
        var totalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanoPagina);

        return new PaginacionResponseDto<UsuarioResponseDto>(
            usuarios.Select(Mapear).ToList(),
            pagina,
            tamanoPagina,
            total,
            totalPaginas);
    }

    public async Task<UsuarioResponseDto?> ObtenerPorIdAsync(int id)
    {
        var usuario = await _repository.ObtenerPorIdAsync(id);
        return usuario is null ? null : Mapear(usuario);
    }

    public async Task<UsuarioResponseDto> CrearAsync(CrearUsuarioDto dto)
    {
        var existente = await _repository.ObtenerPorCorreoAsync(dto.Correo.Trim().ToLower());
        if (existente is not null)
            throw new InvalidOperationException("El correo ya está registrado.");

        var usuario = new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            Correo = dto.Correo.Trim().ToLower(),
            Telefono = dto.Telefono.Trim()
        };

        await _repository.CrearAsync(usuario);
        return Mapear(usuario);
    }

    public async Task<bool> ActualizarAsync(int id, ActualizarUsuarioDto dto)
    {
        var usuario = await _repository.ObtenerPorIdAsync(id);
        if (usuario is null) return false;

        var correoNormalizado = dto.Correo.Trim().ToLower();
        var existente = await _repository.ObtenerPorCorreoAsync(correoNormalizado);
        if (existente is not null && existente.Id != id)
            throw new InvalidOperationException("El correo ya está registrado por otro usuario.");

        usuario.Nombre = dto.Nombre.Trim();
        usuario.Correo = correoNormalizado;
        usuario.Telefono = dto.Telefono.Trim();
        usuario.Activo = dto.Activo;

        await _repository.ActualizarAsync(usuario);
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var usuario = await _repository.ObtenerPorIdAsync(id);
        if (usuario is null) return false;

        await _repository.EliminarAsync(usuario);
        return true;
    }

    private static UsuarioResponseDto Mapear(Usuario u) =>
        new(u.Id, u.Nombre, u.Correo, u.Telefono, u.Activo);
}
