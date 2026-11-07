using System.Security.Cryptography;
using MarkingCodes.Server.Services.Interfaces;

namespace MarkingCodes.Server.Services;

public sealed class MarkingCodeGenerator : IMarkingCodeGenerator
{
    private const string GtinBody = "0460123456789";

    public string Generate()
    {
        var gtin = GenerateGtin();
        var serialNumber = GenerateSerialNumber();

        return $"(01){gtin}(21){serialNumber}";
    }

    private static string GenerateGtin()
    {
        var sum = 0;
        var weight = 3;

        for (var i = GtinBody.Length - 1; i >= 0; i--)
        {
            sum += (GtinBody[i] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }

        var checkDigit = (10 - sum % 10) % 10;

        return $"{GtinBody}{checkDigit}";
    }

    private static string GenerateSerialNumber()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(10));
    }
}