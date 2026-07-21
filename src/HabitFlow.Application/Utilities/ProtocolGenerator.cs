using System.Security.Cryptography;

namespace HabitFlow.Application;

public sealed class ProtocolGenerator
{
    public string Generate(string prefix = "HF") =>
        $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(100000, 999999)}";
}
