using System.ComponentModel.DataAnnotations;

namespace Inventario.Api.Models;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DecimalScaleAttribute(int scale) : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is null || value is decimal number && decimal.Round(number, scale) == number;

    public override string FormatErrorMessage(string name) =>
        $"El campo {name} admite como máximo {scale} decimales.";
}
