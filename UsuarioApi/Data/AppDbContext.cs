using Microsoft.EntityFrameworkCore;
using UsuarioApi.Entities;

namespace UsuarioApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
}
