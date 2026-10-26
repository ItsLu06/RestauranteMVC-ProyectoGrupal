using RestauranteMVC.Models;

namespace RestauranteMVC.Data;

/// <summary>
/// Datos de demostración. Solo se insertan cuando la tabla correspondiente está vacía,
/// así que nunca duplica ni pisa información real.
/// </summary>
public static class DbInicializador
{
    public static void Sembrar(RestauranteDbContext db)
    {
        if (!db.Cargos.Any())
        {
            db.Cargos.AddRange(
                new Cargo { NombreCargo = "Administrador" },
                new Cargo { NombreCargo = "Mesero" },
                new Cargo { NombreCargo = "Cocinero" },
                new Cargo { NombreCargo = "Cajero" });
            db.SaveChanges();
        }

        if (!db.Categorias.Any())
        {
            var entradas = new Categoria { Nombre = "Entradas", Descripcion = "Para empezar" };
            var principales = new Categoria { Nombre = "Platos principales", Descripcion = "Cocina tradicional boliviana" };
            var bebidas = new Categoria { Nombre = "Bebidas", Descripcion = "Refrescos, jugos y cervezas" };
            var postres = new Categoria { Nombre = "Postres", Descripcion = "Para cerrar con dulce" };

            entradas.Platos.AddRange(new[]
            {
                new Plato { Nombre = "Salteña de carne", Precio = 12m },
                new Plato { Nombre = "Sopa de maní", Precio = 25m }
            });
            principales.Platos.AddRange(new[]
            {
                new Plato { Nombre = "Silpancho", Precio = 38m },
                new Plato { Nombre = "Pique macho", Precio = 55m },
                new Plato { Nombre = "Majadito de charque", Precio = 42m },
                new Plato { Nombre = "Fricasé", Precio = 40m }
            });
            bebidas.Platos.AddRange(new[]
            {
                new Plato { Nombre = "Mocochinchi", Precio = 8m },
                new Plato { Nombre = "Jugo de naranja", Precio = 12m },
                new Plato { Nombre = "Cerveza", Precio = 20m }
            });
            postres.Platos.AddRange(new[]
            {
                new Plato { Nombre = "Flan de leche", Precio = 14m },
                new Plato { Nombre = "Helado de canela", Precio = 15m }
            });

            db.Categorias.AddRange(entradas, principales, bebidas, postres);
            db.SaveChanges();
        }

        if (!db.Mesas.Any())
        {
            for (var n = 1; n <= 8; n++)
            {
                db.Mesas.Add(new Mesa { NumeroMesa = n, Capacidad = n <= 4 ? 2 : (n <= 6 ? 4 : 6), Estado = EstadosMesa.Libre });
            }
            db.SaveChanges();
        }

        if (!db.Empleados.Any())
        {
            var mesero = db.Cargos.FirstOrDefault(c => c.NombreCargo == "Mesero") ?? db.Cargos.First();
            var cajero = db.Cargos.FirstOrDefault(c => c.NombreCargo == "Cajero") ?? mesero;
            db.Empleados.AddRange(
                new Empleado { Nombre = "Ana", Apellido = "Rojas", Telefono = "70000001", CargoId = mesero.Id },
                new Empleado { Nombre = "Carlos", Apellido = "Mamani", Telefono = "70000002", CargoId = mesero.Id },
                new Empleado { Nombre = "Lucía", Apellido = "Vargas", Telefono = "70000003", CargoId = cajero.Id });
            db.SaveChanges();
        }
    }
}
