using ApiWebMarket.DTO;
using ApiWebMarket.Models;
using ApiWebMarket.VirtualTables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
namespace ApiWebMarket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VerificacionController : ControllerBase
    {
        private readonly ICodigosVerificacionStore _store;

        public VerificacionController(ICodigosVerificacionStore store)
        {
            _store = store;
        }

        // POST: api/verificacion/enviar-codigo
        [HttpPost("enviar-codigo")]
        public async Task<IActionResult> EnviarCodigo([FromBody] EnviarCodigoRequestcs request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("El correo electrónico es obligatorio.");

            // Generar y guardar código (válido por 5 minutos)
            var registro = _store.GenerarYGuardarCodigo(request.Email, minutosValidez: 5);

            //  Enviar correo con diseño MERCAUCA
            try
            {
                await EnviarCorreoCodigoAsync(registro.Email, registro.Codigo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al enviar el correo de verificación.", detalle = ex.Message });
            }

            
            return Ok(new
            {
                mensaje = "Código de verificación enviado al correo."
            });
        }

        // POST: api/verificacion/verificar-codigo
        [HttpPost("verificar-codigo")]
        public IActionResult VerificarCodigo([FromBody] VerificarCodigoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Codigo))
                return BadRequest("El correo y el código son obligatorios.");

            bool esValido = _store.ValidarYEliminarCodigo(request.Email, request.Codigo);

            if (!esValido)
                return BadRequest(new { mensaje = "Código incorrecto o expirado." });

            return Ok(new { mensaje = "OK - Código verificado correctamente." });
        }

        private async Task EnviarCorreoCodigoAsync(string correoDestino, string codigo)
        {
            string smtpUsuario = "fertica.prueba@gmail.com";
            string smtpPassword = "pctf eaci omfj qlwf"; 
            string asunto = "Código de verificación - MercaUca";

            string cuerpoCorreo = $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <title>Código de verificación - MercaUca</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f4;font-family:Arial,Helvetica,sans-serif;"">
    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color:#f4f4f4;padding:20px 0;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""max-width:600px;background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 4px 12px rgba(0,0,0,0.08);"">
                    <!-- HEADER -->
                    <tr>
                        <td style=""background:linear-gradient(135deg,#00897B,#00695C);padding:20px 30px;text-align:center;color:#ffffff;"">
                            <div style=""font-size:26px;font-weight:bold;letter-spacing:1px;"">MERCAUCA</div>
                            <div style=""font-size:13px;margin-top:4px;opacity:0.9;"">
                                Tu mercado universitario de confianza
                            </div>
                        </td>
                    </tr>

                    <!-- CONTENIDO -->
                    <tr>
                        <td style=""padding:25px 30px 10px 30px;color:#333333;"">
                            <h1 style=""margin:0 0 12px 0;font-size:20px;font-weight:600;color:#222222;"">
                                ¡Hola!
                            </h1>
                            <p style=""margin:0 0 12px 0;font-size:14px;line-height:1.6;"">
                                Has solicitado un <strong>código de verificación</strong> para tu cuenta en <strong>MercaUca</strong>.
                            </p>
                            <p style=""margin:0 0 16px 0;font-size:14px;line-height:1.6;"">
                                Ingresa el siguiente código en la pantalla de verificación para continuar:
                            </p>
                        </td>
                    </tr>

                    <!-- CÓDIGO -->
                    <tr>
                        <td style=""padding:0 30px 10px 30px;text-align:center;"">
                            <div style=""display:inline-block;padding:14px 24px;border-radius:6px;background-color:#00897B;color:#ffffff;font-size:28px;font-weight:bold;letter-spacing:6px;font-family:'Courier New',monospace;"">
                                {codigo}
                            </div>
                            <p style=""margin:12px 0 0 0;font-size:12px;color:#666666;"">
                                Este código es válido por <strong>5 minutos</strong>.
                            </p>
                        </td>
                    </tr>

                    <!-- BOTÓN / CTA -->
                    <tr>
                        <td style=""padding:20px 30px 10px 30px;text-align:center;"">
                            <a href=""https://mercauca.com"" 
                               style=""display:inline-block;padding:10px 22px;border-radius:20px;text-decoration:none;
                                      background-color:#00695C;color:#ffffff;font-size:13px;font-weight:600;text-transform:uppercase;letter-spacing:0.8px;"">
                                Ir a MercaUca
                            </a>
                        </td>
                    </tr>

                    <!-- FOOTER -->
                    <tr>
                        <td style=""padding:20px 30px 25px 30px;color:#999999;font-size:11px;line-height:1.5;text-align:center;border-top:1px solid #eeeeee;"">
                            Si no solicitaste este código, puedes ignorar este correo.<br />
                            © {DateTime.Now.Year} MercaUca - Todos los derechos reservados.
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            using (var clienteSmtp = new SmtpClient("smtp.gmail.com"))
            {
                clienteSmtp.Port = 587;
                clienteSmtp.EnableSsl = true;
                clienteSmtp.UseDefaultCredentials = false;

                clienteSmtp.Credentials = new NetworkCredential(
                    smtpUsuario,
                    smtpPassword
                );

                using (var correo = new MailMessage())
                {
                    correo.From = new MailAddress(smtpUsuario, "MercaUca");
                    correo.To.Add(correoDestino);
                    correo.Subject = asunto;
                    correo.Body = cuerpoCorreo;
                    correo.IsBodyHtml = true;  

                    await clienteSmtp.SendMailAsync(correo);
                }
            }
        }
    }
}
