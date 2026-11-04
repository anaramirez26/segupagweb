namespace ApiWebMarket.DTO
{
    public class CrearProductoRequest
    {
        public string IdUsuario { get; set; } = null!;   // idUsuario (vendedor)
        public string Titulo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public string Marca { get; set; } = null!;
        public string IdCondicion { get; set; } = null!;
        public string IdCategoria { get; set; } = null!;
        public double Precio { get; set; }

        // Viene como archivo en el FormData
        public IFormFile Imagen { get; set; } = null!;
    }
}
