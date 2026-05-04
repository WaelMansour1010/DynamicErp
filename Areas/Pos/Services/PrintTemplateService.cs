using MyERP.Areas.Pos.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;

namespace MyERP.Areas.Pos.Services
{
    // File-based storage for print templates. Templates live under
    // ~/App_Data/PrintTemplates/{name}.json and their background scans
    // under ~/App_Data/PrintTemplates/Backgrounds/{name}{ext}. App_Data
    // is never directly served by ASP.NET, so users hit them through
    // PrintTemplateController.Background.
    public class PrintTemplateService
    {
        private const string TemplatesFolderRel = "~/App_Data/PrintTemplates";
        private const string BackgroundsFolderRel = "~/App_Data/PrintTemplates/Backgrounds";

        private static readonly string[] AllowedImageExtensions = new[]
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp"
        };

        public PrintTemplate Load(string name)
        {
            var safeName = SafeName(name);
            var path = ResolvePath(TemplatesFolderRel, safeName + ".json");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(path);
                var template = JsonConvert.DeserializeObject<PrintTemplate>(json);
                if (template != null)
                {
                    template.Name = safeName;
                    template.Fields = template.Fields ?? new List<PrintTemplateField>();
                }
                return template;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public void Save(string name, PrintTemplate template)
        {
            if (template == null)
            {
                throw new ArgumentNullException("template");
            }

            var safeName = SafeName(name);
            template.Name = safeName;
            template.Fields = template.Fields ?? new List<PrintTemplateField>();

            EnsureFolder(TemplatesFolderRel);
            var path = ResolvePath(TemplatesFolderRel, safeName + ".json");
            var json = JsonConvert.SerializeObject(template, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public IEnumerable<string> ListNames()
        {
            var folder = ResolveFolder(TemplatesFolderRel);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return Enumerable.Empty<string>();
            }

            return Directory
                .GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(n => n)
                .ToList();
        }

        public string SaveBackground(string name, byte[] imageBytes, string originalFileName)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new ArgumentException("Empty background image");
            }

            var ext = Path.GetExtension(originalFileName ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
            {
                throw new ArgumentException("Unsupported image type. Allowed: " +
                    string.Join(", ", AllowedImageExtensions));
            }

            var safeName = SafeName(name);
            EnsureFolder(BackgroundsFolderRel);
            var fileName = safeName + ext;
            var path = ResolvePath(BackgroundsFolderRel, fileName);
            File.WriteAllBytes(path, imageBytes);
            return fileName;
        }

        public byte[] LoadBackground(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            var safeFile = Path.GetFileName(fileName);
            var path = ResolvePath(BackgroundsFolderRel, safeFile);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            return File.ReadAllBytes(path);
        }

        public string GetBackgroundContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            switch (ext)
            {
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".bmp": return "image/bmp";
                default: return "application/octet-stream";
            }
        }

        public static string SafeName(string name)
        {
            var trimmed = (name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return "default";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(trimmed.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            // Avoid path traversal entirely: keep only the bare file name part.
            return Path.GetFileNameWithoutExtension(clean);
        }

        private static string ResolveFolder(string virtualPath)
        {
            try
            {
                if (HostingEnvironment.IsHosted)
                {
                    return HostingEnvironment.MapPath(virtualPath);
                }
            }
            catch
            {
                // ignore - return null below
            }
            return null;
        }

        private static string ResolvePath(string virtualFolder, string fileName)
        {
            var folder = ResolveFolder(virtualFolder);
            return string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, fileName);
        }

        private static void EnsureFolder(string virtualPath)
        {
            var folder = ResolveFolder(virtualPath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
    }
}
