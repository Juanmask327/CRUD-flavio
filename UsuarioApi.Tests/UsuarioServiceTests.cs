using Moq;
using UsuarioApi.DTOs;
using UsuarioApi.Entities;
using UsuarioApi.Interfaces;
using UsuarioApi.Services;
using Xunit;

namespace UsuarioApi.Tests;

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock = new();
    private readonly UsuarioService _service;

    public UsuarioServiceTests()
    {
        _service = new UsuarioService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CrearAsync_CorreoNoRegistrado_CreaUsuario()
    {
        var dto = new CrearUsuarioDto("Laura Gómez", "Laura.Gomez@correo.com", "3001234567");

        _repositoryMock.Setup(r => r.ObtenerPorCorreoAsync(It.IsAny<string>()))
            .ReturnsAsync((Usuario?)null);
        _repositoryMock.Setup(r => r.CrearAsync(It.IsAny<Usuario>()))
            .ReturnsAsync((Usuario u) => u);

        var resultado = await _service.CrearAsync(dto);

        Assert.Equal("laura.gomez@correo.com", resultado.Correo);
        _repositoryMock.Verify(r => r.CrearAsync(It.IsAny<Usuario>()), Times.Once);
    }

    [Fact]
    public async Task CrearAsync_CorreoYaRegistrado_LanzaExcepcion()
    {
        var dto = new CrearUsuarioDto("Laura Gómez", "laura.gomez@correo.com", "3001234567");
        _repositoryMock.Setup(r => r.ObtenerPorCorreoAsync(It.IsAny<string>()))
            .ReturnsAsync(new Usuario { Id = 1, Correo = "laura.gomez@correo.com" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CrearAsync(dto));
        _repositoryMock.Verify(r => r.CrearAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_UsuarioNoExiste_RetornaNull()
    {
        _repositoryMock.Setup(r => r.ObtenerPorIdAsync(99)).ReturnsAsync((Usuario?)null);

        var resultado = await _service.ObtenerPorIdAsync(99);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task EliminarAsync_UsuarioExiste_RetornaTrueYElimina()
    {
        var usuario = new Usuario { Id = 5, Nombre = "Ana", Correo = "ana@correo.com" };
        _repositoryMock.Setup(r => r.ObtenerPorIdAsync(5)).ReturnsAsync(usuario);

        var resultado = await _service.EliminarAsync(5);

        Assert.True(resultado);
        _repositoryMock.Verify(r => r.EliminarAsync(usuario), Times.Once);
    }

    [Fact]
    public async Task EliminarAsync_UsuarioNoExiste_RetornaFalse()
    {
        _repositoryMock.Setup(r => r.ObtenerPorIdAsync(123)).ReturnsAsync((Usuario?)null);

        var resultado = await _service.EliminarAsync(123);

        Assert.False(resultado);
        _repositoryMock.Verify(r => r.EliminarAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task ObtenerTodosAsync_ConPaginacion_CalculaTotalPaginas()
    {
        var usuarios = new List<Usuario>
        {
            new() { Id = 1, Nombre = "A", Correo = "a@correo.com" },
            new() { Id = 2, Nombre = "B", Correo = "b@correo.com" }
        };
        _repositoryMock.Setup(r => r.ObtenerTodosAsync(null, 1, 10))
            .ReturnsAsync((usuarios, 2));

        var resultado = await _service.ObtenerTodosAsync(null, 1, 10);

        Assert.Equal(2, resultado.TotalRegistros);
        Assert.Equal(1, resultado.TotalPaginas);
        Assert.Equal(2, resultado.Items.Count);
    }
}
