using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codelyzer.Analysis;
using Codelyzer.Analysis.Model;
using CTA.FeatureDetection.Common.Extensions;
using CTA.FeatureDetection.Common.Models.Features.Base;
using CTA.FeatureDetection.Common.WCFConfigUtils;
using CTA.Rules.Common.WebConfigManagement;

namespace CTA.FeatureDetection.ProjectType.CompiledFeatures
{
    class CoreWCFCompatibleServiceFeature : WebConfigFeature
    {
        public override bool IsPresent(AnalyzerResult analyzerResult)
        {
            // Check If WCF Service
            if (!IsWCFService(analyzerResult))
            {
                return false;
            }

            Dictionary<string, List<string>> bindingsTransportMap = WCFBindingAndTransportUtil.getBindingAndTransport(analyzerResult);

            bool hasCoreWCFSupport = false;

            foreach(var binding in bindingsTransportMap)
            {
                var bindingName = binding.Key;

                //Variables assigned but not used, can be used as a metric
                var unsupportedBindings = new List<string>();
                var unsupportedModes = new Dictionary<string, List<string>>();

                if (CoreWCFBindings.CORE_WCF_BINDINGS.Keys.Contains(bindingName))
                {
                    var modes = bindingsTransportMap[bindingName];
                    var supportedModes = CoreWCFBindings.CORE_WCF_BINDINGS[bindingName];
                    foreach (string mode in modes) 
                    {
                        if (!supportedModes.Contains(mode))
                        {
                            if(!unsupportedModes.ContainsKey(bindingName))
                            {
                                unsupportedModes.Add(bindingName, new List<string>());
                            }
                            unsupportedModes[bindingName].Add(mode);
                        }
                        
                        //If even one transport with mode is supported on CoreWCF set the flag.
                        else
                        {
                            hasCoreWCFSupport = true;
                        }
                    }
                }
                else
                {
                    unsupportedBindings.Add(bindingName);
                }
            }

            return hasCoreWCFSupport;
        }

        public bool IsWCFService(AnalyzerResult analyzerResult)
        {
            string projectDir = analyzerResult.ProjectResult.ProjectRootPath;

            string webConfigFile = Path.Combine(projectDir, Rules.Config.Constants.WebConfig);
            string appConfigFile = Path.Combine(projectDir, Rules.Config.Constants.AppConfig);

            // For Config based look for <services> element.
            if (File.Exists(webConfigFile) || File.Exists(appConfigFile))
            {
                var config = WebConfigManager.LoadWebConfigAsXDocument(projectDir);
                if (config.ContainsElement(Constants.WCFServiceElementPath))
                {
                    return true;
                }
            }

            // For Code Based, look for Service Interface

            var project = analyzerResult.ProjectResult;

            var interfaces = project.GetAllInterfaceDeclarations()?.ToList();
            if (interfaces.IsNullOrEmpty()) { return false; }

            var interfacesWithServiceContract = interfaces
                .Where(i => i.HasAttribute(Constants.ServiceContractAttribute))
                ?.ToList();
            if (interfacesWithServiceContract.IsNullOrEmpty()) { return false; }

            var interfaceWithServiceContractMethods = interfacesWithServiceContract
                .SelectMany(i => i.GetMethodDeclarations())?.ToList();
            if (interfaceWithServiceContractMethods.IsNullOrEmpty()) { return false; }

            var serviceInterfaceMethodWithObjectContract = interfaceWithServiceContractMethods
                .Where(m => m.HasAttribute(Constants.OperationContractAttribute))
                ?.ToList();

            if (!serviceInterfaceMethodWithObjectContract.IsNullOrEmpty())
            {
                var classes = project.GetAllClassDeclarations()?.ToList();
                if(classes.IsNullOrEmpty()) { return false; }

                var classImplementingServiceContractInterface = interfacesWithServiceContract
                    .SelectMany(i => classes.Where(c => c.InheritsInterface(i.Identifier)))
                    .ToList();
                if (!classImplementingServiceContractInterface.IsNullOrEmpty())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
