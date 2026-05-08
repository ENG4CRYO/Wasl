using Wasl.Application.Interfaces.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Wasl.Infrastructure.Services
{
    public class TemplateService : ITemplateService
    {
        public async Task<string> GetTemplateAsync(string templateName, Dictionary<string, string> placeholders)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", $"{templateName}.html");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The template file {templateName}.html was not found at {filePath}");


            var templateContent = await File.ReadAllTextAsync(filePath);


            if (placeholders != null)
            {
                foreach (var placeholder in placeholders)
                {
                    templateContent = templateContent.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
                }
            }

            return templateContent;
        }
    }
}