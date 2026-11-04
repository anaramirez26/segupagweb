using ApiWebMarket.Models;                   
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiWebMarket.DTO;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace ApiWebMarket.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class CarritoController : ControllerBase
    {
        private readonly MercaUcaContext _context;

        public CarritoController(MercaUcaContext context)
        {
            _context = context;
        }

        // GET: api/Carrito/{usuario}
        [HttpGet("{usuario}")]
        public async Task<ActionResult<IEnumerable<CarritoItemDto>>> GetCarritoPorUsuario(string usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario))
                return BadRequest("El usuario es requerido.");

            var items = await (
                from ac in _context.ArticulosCarritos
                join c in _context.Carritos on ac.IdCarrito equals c.IdCarrito
                join vp in _context.VariantesProductos on ac.IdProductoVariante equals vp.IdProductoVariante
                join p in _context.Productos on vp.IdProducto equals p.IdProducto
                where c.IdComprador == usuario
                select new CarritoItemDto
                {
                    IdProducto = p.IdProducto,
                    TituloProducto = p.TituloProducto,
                    FotoBase64 = p.Foto != null
                        ? "data:image/jpeg;base64," + Convert.ToBase64String(p.Foto)
                        : null,

                   
                    Precio = p.Precio != null ? (double?)p.Precio : null,
                    Cantidad = ac.Cantidad
                }
            ).ToListAsync();

            return Ok(items);
        }


        [HttpPost("agregar-articulo")]
        public async Task<ActionResult> AgregarArticulo([FromBody] AgregarArticuloCarritoDto dto)
        {
            if (dto.Cantidad <= 0)
                return BadRequest("La cantidad debe ser mayor que cero.");

            // ===== 1) VALIDAR SI YA EXISTE ESE PRODUCTO EN EL CARRITO DEL USUARIO =====
            // Equivalente a:
            // SELECT COUNT(ac.id_producto_variante)
            // FROM Articulos_carrito ac
            // INNER JOIN Carrito c ON ac.id_carrito = c.id_carrito
            // WHERE c.id_comprador = @idUsuario AND ac.id_producto_variante = @idProducto

            var cantidadExistente = await (
                from ac in _context.ArticulosCarritos
                join c in _context.Carritos on ac.IdCarrito equals c.IdCarrito
                where c.IdComprador == dto.IdUsuario
                      && ac.IdProductoVariante == dto.IdProductoVariante
                select ac
            ).CountAsync();

            if (cantidadExistente > 0)
            {
                // Ya hay al menos una fila con ese producto en el carrito de ese usuario
                return BadRequest(new { mensaje = "El artículo ya está en el carrito." });
                // Si prefieres que no sea error HTTP, podrías usar:
                // return Ok(new { mensaje = "El artículo ya está en el carrito." });
            }

            // ===== 2) BUSCAR O CREAR EL CARRITO =====
            var carrito = await _context.Carritos
                .Include(c => c.ArticulosCarritos)
                .FirstOrDefaultAsync(c => c.IdComprador == dto.IdUsuario);

            if (carrito == null)
            {
                carrito = new Carrito
                {
                    IdCarrito = Guid.NewGuid().ToString(),
                    IdComprador = dto.IdUsuario,
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow
                };

                _context.Carritos.Add(carrito);
            }
            else
            {
                carrito.FechaModificacion = DateTime.UtcNow;
            }

            // ===== 3) INSERTAR ARTÍCULO EN ArticulosCarritos =====
            var articulo = new ArticulosCarrito
            {
                IdArticulosCarrito = Guid.NewGuid().ToString(),
                IdCarrito = carrito.IdCarrito,
                IdProductoVariante = dto.IdProductoVariante,
                Cantidad = dto.Cantidad,
                Precio = dto.Precio,
                Moneda = string.IsNullOrWhiteSpace(dto.Moneda) ? "$" : dto.Moneda
            };

            _context.ArticulosCarritos.Add(articulo);

            await _context.SaveChangesAsync();

            // ===== 4) RESPUESTA =====
            return Ok(new { mensaje = "Artículo agregado correctamente." });
        }


        [HttpPost("finalizar-compra/{idUsuario}")]
        public async Task<IActionResult> FinalizarCompra(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return BadRequest("El id de usuario es requerido.");

            // Buscar usuario y correo
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            if (string.IsNullOrWhiteSpace(usuario.EmailUsuario))
                return BadRequest("El usuario no tiene correo registrado.");

            string correoDestino = usuario.EmailUsuario;

            // Carrito del usuario
            var itemsCarrito = await (
                from ac in _context.ArticulosCarritos
                join c in _context.Carritos on ac.IdCarrito equals c.IdCarrito
                join vp in _context.VariantesProductos on ac.IdProductoVariante equals vp.IdProductoVariante
                join p in _context.Productos on vp.IdProducto equals p.IdProducto
                where c.IdComprador == idUsuario
                select new
                {
                    p.TituloProducto,
                    ac.Cantidad,
                    ac.Precio,
                    ac.Moneda,
                    c.IdCarrito
                }
            ).ToListAsync();

            if (!itemsCarrito.Any())
                return BadRequest("El carrito está vacío, no hay nada que finalizar.");

            var idCarrito = itemsCarrito.First().IdCarrito;

            double total = itemsCarrito.Sum(i => i.Precio);
            string moneda = itemsCarrito.First().Moneda;

            // Nombre de cliente
            var nombreCliente = string.IsNullOrWhiteSpace(usuario.NombreUsuario)
                ? "cliente"
                : usuario.NombreUsuario;

            // Tabla de productos
            var sbTabla = new StringBuilder();
            foreach (var item in itemsCarrito)
            {
                sbTabla.AppendLine($@"
            <tr>
              <td style='padding:8px 4px; border-bottom:1px solid #f3f4f6;'>{item.TituloProducto}</td>
              <td style='padding:8px 4px; text-align:center; border-bottom:1px solid #f3f4f6;'>{item.Cantidad}</td>
              <td style='padding:8px 4px; text-align:center; border-bottom:1px solid #f3f4f6;'>
                {item.Precio:0.00} {item.Moneda}
              </td>
            </tr>
        ");
            }

            string tablaProductosHtml = sbTabla.ToString();

            // ====== CUERPO HTML DEL CORREO ======
            string cuerpoCorreo = $@"
<html>
  <body style='font-family: Arial, sans-serif; background:#f6f6f6; padding:20px;'>
    <div style='max-width:600px; margin:0 auto; background:white; border-radius:10px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.1);'>

      <!-- Header -->
      <div style='background:#dc2626; padding:20px; text-align:center;'>
        <h1 style='color:white; margin:0; font-size:26px;'>MercaUca</h1>
        <p style='color:#ffeaea; margin:4px 0 0; font-size:14px;'>Confirmación de compra</p>
      </div>

      <!-- Contenido -->
      <div style='padding:24px; font-size:15px; color:#111827;'>
        <p>Hola <strong>{nombreCliente}</strong>,</p>
        <p>Gracias por tu compra en <strong>MercaUca</strong>. Este es el resumen de tu pedido:</p>

        <h3 style='color:#dc2626; margin-top:20px; margin-bottom:10px; font-size:16px;'>Detalle de productos</h3>

        <table style='width:100%; border-collapse:collapse; font-size:14px;'>
          <thead>
            <tr>
              <th style='text-align:left; padding:8px 4px; border-bottom:1px solid #e5e7eb;'>Producto</th>
              <th style='padding:8px 4px; border-bottom:1px solid #e5e7eb;'>Cantidad</th>
              <th style='padding:8px 4px; border-bottom:1px solid #e5e7eb;'>Total</th>
            </tr>
          </thead>
          <tbody>
            {tablaProductosHtml}
          </tbody>
        </table>

        <p style='margin-top:18px; font-size:16px;'>
          <strong>Total pagado:</strong>
          <span style='color:#dc2626; font-size:18px; margin-left:4px;'>
            {total:0.00} {moneda}
          </span>
        </p>

        <p style='margin-top:18px; color:#4b5563;'>
          Te enviaremos otra notificación cuando tu pedido esté listo para entrega o envío.
        </p>

        <p style='margin-top:12px; color:#6b7280; font-size:13px;'>
          Si tienes alguna duda, puedes responder directamente a este correo.
        </p>
      </div>

      <!-- Footer -->
      <div style='background:#f3f4f6; padding:12px 16px; text-align:center; color:#9ca3af; font-size:12px;'>
        © {DateTime.Now.Year} MercaUca — Todos los derechos reservados.
      </div>

    </div>
  </body>
</html>";

            // ====== ENVÍO DE CORREO ======
            try
            {
                using (var clienteSmtp = new SmtpClient("smtp.gmail.com"))
                {
                    clienteSmtp.Port = 587;
                    clienteSmtp.EnableSsl = true;
                    clienteSmtp.UseDefaultCredentials = false;

                    clienteSmtp.Credentials = new NetworkCredential(
                        "fertica.prueba@gmail.com",
                        "pctf eaci omfj qlwf"
                    );

                    using (var correo = new MailMessage())
                    {
                        correo.From = new MailAddress("fertica.prueba@gmail.com", "MercaUca");
                        correo.To.Add(correoDestino);
                        correo.Subject = "Confirmación de compra - MercaUca";
                        correo.Body = cuerpoCorreo;
                        correo.IsBodyHtml = true;   // 👈 AHORA ES HTML

                        await clienteSmtp.SendMailAsync(correo);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"No se pudo enviar el correo: {ex.Message}");
            }

            // ====== VACIAR CARRITO ======
            var articulos = _context.ArticulosCarritos
                .Where(a => a.IdCarrito == idCarrito);

            _context.ArticulosCarritos.RemoveRange(articulos);

            var carrito = await _context.Carritos
                .FirstOrDefaultAsync(c => c.IdCarrito == idCarrito);

            if (carrito != null)
            {
                _context.Carritos.Remove(carrito);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Compra finalizada. Se envió un correo de confirmación y se vació el carrito."
            });
        }
        // GET: api/Carrito/cantidad-articulos/{idUsuario}
        [HttpGet("cantidad-articulos/{idUsuario}")]
        public async Task<ActionResult<int>> ObtenerCantidadArticulos(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return BadRequest("El idUsuario es requerido.");

            // 1) Buscar el id_carrito del usuario
            var idCarrito = await _context.Carritos
                .Where(c => c.IdComprador == idUsuario)
                .Select(c => c.IdCarrito)
                .FirstOrDefaultAsync();

            if (idCarrito == null)
            {
                // El usuario no tiene carrito => 0 artículos
                return Ok(0);
            }

            // 2) Contar artículos en ArticulosCarrito para ese id_carrito
            var cantidadArticulos = await _context.ArticulosCarritos
                .Where(a => a.IdCarrito == idCarrito)
                .CountAsync();

            // Devuelve SOLO el número (JSON: 5, 0, 12, etc.)
            return Ok(cantidadArticulos);
        }

        [HttpDelete("eliminar-articulo/{idUsuario}/{idProducto}")]
        public async Task<IActionResult> EliminarArticuloCarrito(string idUsuario, string idProducto)
        {
            if (string.IsNullOrWhiteSpace(idUsuario) || string.IsNullOrWhiteSpace(idProducto))
                return BadRequest("El idUsuario y el idProducto son obligatorios.");

            // 1) Buscar carrito del usuario
            var carrito = await _context.Carritos
                .FirstOrDefaultAsync(c => c.IdComprador == idUsuario);

            if (carrito == null)
                return NotFound("El usuario no tiene un carrito registrado.");

            // 2) Buscar artículo dentro del carrito
            var articulo = await _context.ArticulosCarritos
                .FirstOrDefaultAsync(a =>
                    a.IdCarrito == carrito.IdCarrito &&
                    a.IdProductoVariante == idProducto);

            if (articulo == null)
                return NotFound("El artículo no está en el carrito del usuario.");

            // 3) Eliminar
            _context.ArticulosCarritos.Remove(articulo);

            // 4) Guardar
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Artículo eliminado correctamente del carrito.",
                idCarrito = carrito.IdCarrito,
                idProducto = idProducto
            });
        }
    }
}

