using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace samane.Configuration
{
    public class RuntimeSettings
    {
        private readonly string directory;
        public RuntimeSettings(IHostingEnvironment environment)
        {
            directory = Path.Combine(environment.ContentRootPath, "wwwroot", "settings");
        }
        public string ApiBaseUrl
        {
            get {
                var value = File.ReadAllText(Path.Combine(directory, "ApIBaseUrl.txt")).Trim().Trim('\uFEFF');
                Uri uri;
                if (!Uri.TryCreate(value, UriKind.Absolute, out uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https") || !string.IsNullOrEmpty(uri.Query) ||
                    !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
                    throw new InvalidDataException(
                        "ApIBaseUrl.txt must contain an absolute HTTP(S) base URL " +
                        "without query, fragment or credentials.");
                return value.TrimEnd('/');
            }
        }
        public string[] ReadOrigins()
        {
            return File.ReadAllText(Path.Combine(directory, "CorseOrigins.txt"))
                .Trim('\uFEFF')
                .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0 && !x.StartsWith("#"))
                .Select(
                    x =>
                    {
                        Uri uri;
                        if (!Uri.TryCreate(x, UriKind.Absolute, out uri) ||
                            (uri.Scheme != "http" && uri.Scheme != "https") || uri.AbsolutePath != "/" ||
                            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
                            !string.IsNullOrEmpty(uri.UserInfo))
                            throw new InvalidDataException(
                                "CorseOrigins.txt must contain HTTP(S) origins without paths or wildcards.");
                        return uri.GetLeftPart(UriPartial.Authority);
                    })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
