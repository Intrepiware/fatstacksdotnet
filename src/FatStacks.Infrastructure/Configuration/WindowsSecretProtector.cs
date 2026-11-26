using System.Security.Cryptography;
using System.Text;
using FatStacks.Core.Contracts;
namespace FatStacks.Infrastructure.Configuration;

public sealed class WindowsSecretProtector : ISecretProtector
{
    public string Protect(string value)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Saved credentials require Windows user protection.");
        return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
    }
    public string Unprotect(string value)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Saved credentials require Windows user protection.");
        return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(value), null, DataProtectionScope.CurrentUser));
    }
}
