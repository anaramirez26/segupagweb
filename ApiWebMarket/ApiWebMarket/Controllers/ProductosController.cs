using ApiWebMarket.DTO;
using ApiWebMarket.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiWebMarket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly MercaUcaContext _context;

        public ProductosController(MercaUcaContext context)
        {
            _context = context;
        }
        private string GenerarIdCorto()
        {
            Guid guid = Guid.NewGuid();

            // Convertir GUID → base64
            string base64 = Convert.ToBase64String(guid.ToByteArray());

            
            base64 = base64.Replace("/", "_")
                           .Replace("+", "-")
                           .Replace("=", "");

            
            return base64;
        }
        [HttpPost]

      
        public async Task<IActionResult> CrearProducto([FromForm] CrearProductoRequest request)
        {
            if (request.Imagen == null || request.Imagen.Length == 0)
                return BadRequest("La imagen del producto es obligatoria.");

            
            string idGenerado = GenerarIdCorto();  
            string idMedia = idGenerado + "M";     
            string idVariante = idGenerado;         

            
            byte[] fotoBytes;
            using (var ms = new MemoryStream())
            {
                await request.Imagen.CopyToAsync(ms);
                fotoBytes = ms.ToArray();
            }

            
            var producto = new Producto
            {
                IdProducto = idGenerado,
                IdVendedor = request.IdUsuario,
                TituloProducto = request.Titulo,
                Descripcion = request.Descripcion,
                Marca = request.Marca,
                IdCondicion = request.IdCondicion,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow,
                Foto = fotoBytes,
                IdCategoria = request.IdCategoria,
                Precio = request.Precio
            };

            
            var media = new ProductoMedium
            {
                IdMedia = idMedia,
                IdProducto = idGenerado,
                PosicionGaleria = 0,
                ProductoPrimario = false, 
                Novedades = true          
            };

            
            var variante = new VariantesProducto
            {
                IdProductoVariante = idVariante,
                IdProducto = idGenerado,
                Talla = null,
                Color = null,
                Stock = 0
            };

            
            _context.Productos.Add(producto);
            _context.ProductoMedia.Add(media);
            _context.VariantesProductos.Add(variante);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                idProducto = idGenerado,
                mensaje = "Producto creado correctamente"
            });
        }
    }
}
