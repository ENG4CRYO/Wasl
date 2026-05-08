using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Interfaces.Infrastructure
{

        public interface ITemplateService
        {
            Task<string> GetTemplateAsync(string templateName, Dictionary<string, string> placeholders);
        
    }
}
