using ApiWebMarket.VirtualModels;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ApiWebMarket.VirtualTables
{

    public interface ICodigosVerificacionStore
    {
        CodigoVerificacion GenerarYGuardarCodigo(string email, int minutosValidez = 5);
        bool ValidarYEliminarCodigo(string email, string codigo);
    }

    public class InMemoryCodigosVerificacionStore : ICodigosVerificacionStore
    {
        
        private readonly ConcurrentDictionary<string, CodigoVerificacion> _tabla =
            new ConcurrentDictionary<string, CodigoVerificacion>();

        public CodigoVerificacion GenerarYGuardarCodigo(string email, int minutosValidez = 5)
        {
            
            var random = RandomNumberGenerator.GetInt32(0, 1_000_000);
            string codigo = random.ToString("D6"); // ej: 004281

            var registro = new CodigoVerificacion
            {
                Email = email,
                Codigo = codigo,
                ExpiraEn = DateTime.UtcNow.AddMinutes(minutosValidez)
            };

            
            _tabla.AddOrUpdate(email, registro, (key, old) => registro);

            return registro;
        }

        public bool ValidarYEliminarCodigo(string email, string codigo)
        {
            if (!_tabla.TryGetValue(email, out var registro))
                return false;

            
            if (DateTime.UtcNow > registro.ExpiraEn)
            {
                _tabla.TryRemove(email, out _); 
                return false;
            }

            
            if (!string.Equals(registro.Codigo, codigo, StringComparison.Ordinal))
                return false;

            
            _tabla.TryRemove(email, out _);
            return true;
        }
    }
}
