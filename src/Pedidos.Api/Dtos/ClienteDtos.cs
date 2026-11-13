using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Pedidos.Api.Dtos;

public class ClienteDto
{
    public int IdCliente { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public class CrearClienteDto
{
    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Los nombres deben tener entre 3 y 200 caracteres.")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es obligatoria.")]
    [RegularExpression("^\\d{10}$", ErrorMessage = "La cédula debe tener exactamente 10 dígitos.")]
    [CedulaEcuatoriana]
    public string Cedula { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string Correo { get; set; } = string.Empty;
}

public class ActualizarClienteDto
{
    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Los nombres deben tener entre 3 y 200 caracteres.")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es obligatoria.")]
    [RegularExpression("^\\d{10}$", ErrorMessage = "La cédula debe tener exactamente 10 dígitos.")]
    [CedulaEcuatoriana]
    public string Cedula { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string Correo { get; set; } = string.Empty;
}

public sealed class CedulaEcuatorianaAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string texto || string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        return CedulaEcuatorianaValidator.EsValida(texto.Trim());
    }

    public override string FormatErrorMessage(string name)
    {
        return "La cédula ecuatoriana es inválida.";
    }
}

public static class CedulaEcuatorianaValidator
{
    public static bool EsValida(string cedula)
    {
        if (string.IsNullOrWhiteSpace(cedula) || cedula.Length != 10 || !cedula.All(char.IsDigit))
        {
            return false;
        }

        var provincia = int.Parse(cedula.Substring(0, 2));
        if (provincia < 1 || provincia > 24)
        {
            return false;
        }

        var coeficientes = new[] { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
        var suma = 0;

        for (var i = 0; i < 9; i++)
        {
            var digito = int.Parse(cedula[i].ToString());
            var producto = digito * coeficientes[i];
            if (producto > 9)
            {
                producto -= 9;
            }

            suma += producto;
        }

        var digitoVerificador = (10 - (suma % 10)) % 10;
        return digitoVerificador == int.Parse(cedula[9].ToString());
    }
}
