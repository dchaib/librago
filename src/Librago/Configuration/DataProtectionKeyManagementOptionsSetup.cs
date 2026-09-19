using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;

namespace Librago.Configuration;

public sealed class DataProtectionKeyManagementOptionsSetup(
    IOptions<LibragoOptions> libragoOptions,
    IWebHostEnvironment environment,
    ILoggerFactory loggerFactory) : IConfigureOptions<KeyManagementOptions>
{
    public void Configure(KeyManagementOptions options)
    {
        var configuredKeyPath = libragoOptions.Value.DataProtectionPath;
        var dataProtectionPath = Path.IsPathRooted(configuredKeyPath)
            ? configuredKeyPath
            : Path.Combine(environment.ContentRootPath, configuredKeyPath);

        Directory.CreateDirectory(dataProtectionPath);
        options.XmlRepository = new FileSystemXmlRepository(
            new DirectoryInfo(dataProtectionPath),
            loggerFactory);
    }
}
