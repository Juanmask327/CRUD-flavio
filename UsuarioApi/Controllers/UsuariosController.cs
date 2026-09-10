using Microsoft.AspNetCore.Mvc;
using UsuarioApi.DTOs;
using UsuarioApi.Interfaces;

namespace UsuarioApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _service;

    public UsuariosController(IUsuarioService service)
    {
        _service = service;
    }

    // GET api/usuarios?busqueda=laura&pagina=1&tamanoPagina=10
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? busqueda, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 10) =>
        Ok(await _service.ObtenerTodosAsync(busqueda, pagina, tamanoPagina));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var usuario = await _service.ObtenerPorIdAsync(id);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPost]
    public async Task<IActionResult> Post(CrearUsuarioDto dto)
    {
        var creado = await _service.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, ActualizarUsuarioDto dto)
    {
        var actualizado = await _service.ActualizarAsync(id, dto);
        return actualizado ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.EliminarAsync(id);
        return eliminado ? NoContent() : NotFound();
    }
}
