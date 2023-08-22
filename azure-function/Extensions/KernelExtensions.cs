﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Text;

namespace AIPlugins.AzureFunctions.Extensions;

public static class KernelExtensions
{
    public static IDictionary<string, ISKFunction> ImportPromptsFromDirectory(
        this IKernel kernel, string pluginName, string promptDirectory)
    {
        const string CONFIG_FILE = "config.json";
        const string PROMPT_FILE = "skprompt.txt";

        var plugin = new Dictionary<string, ISKFunction>();

        string[] directories = Directory.GetDirectories(promptDirectory);
        foreach (string dir in directories)
        {
            var functionName = Path.GetFileName(dir);

            // Continue only if prompt template exists
            var promptPath = Path.Combine(dir, PROMPT_FILE);
            if (!File.Exists(promptPath)) { continue; }

            // Load prompt configuration. Note: the configuration is optional.
            var config = new PromptTemplateConfig();
            var configPath = Path.Combine(dir, CONFIG_FILE);
            if (File.Exists(configPath))
            {
                config = PromptTemplateConfig.FromJson(File.ReadAllText(configPath));
            }

            // Load prompt template
            var template = new PromptTemplate(File.ReadAllText(promptPath), config, kernel.PromptTemplateEngine);

            // Prepare lambda wrapping AI logic
            var functionConfig = new SemanticFunctionConfig(config, template);

            kernel.Logger.LogTrace("Registering function {0}.{1} loaded from {2}", pluginName, functionName, dir);
            plugin[functionName] = kernel.RegisterSemanticFunction(pluginName, functionName, functionConfig);
        }

        return plugin;
    }
}
