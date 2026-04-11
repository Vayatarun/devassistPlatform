using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
namespace Analyzer.Core.Configuration
{
   

        public class RuleConfigLoader
        {
            public RuleConfig Load(string path)
            {
                if (!File.Exists(path))
                    return new RuleConfig(); // default config

                var json = File.ReadAllText(path);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<RuleConfig>(json, options)
                       ?? new RuleConfig();
            }
        }
    }

