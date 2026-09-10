using System.ComponentModel.DataAnnotations;

namespace UsuarioApi.DTOs;

public record CrearUsuarioDto(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    string Correo,

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    string Telefono
);

public record ActualizarUsuarioDto(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    string Correo,

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    string Telefono,

    bool Activo
);

public record UsuarioResponseDto(
    int Id,
    string Nombre,
    string Correo,
    string Telefono,
    bool Activo
);

public record PaginacionResponseDto<T>(
    List<T> Items,
    int PaginaActual,
    int TamanoPagina,
    int TotalRegistros,
    int TotalPaginas
);
