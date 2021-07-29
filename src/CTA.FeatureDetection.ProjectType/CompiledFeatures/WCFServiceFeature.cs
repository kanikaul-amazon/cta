using System.IO;
using System.Linq;
using Codelyzer.Analysis;
using CTA.FeatureDetection.Common.Extensions;
using CTA.FeatureDetection.Common.Models.Features.Base;
using CTA.Rules.Common.WebConfigManagement;

namespace CTA.FeatureDetection.ProjectType.CompiledFeatures
{
    class WCFServiceFeature : WebConfigFeature
    {
        /// <summary>
        /// Determines if a project is a WCF Service based on the following :-
        ///     1) If it has a web.config or App.config based configuaration, and has a service tag 
        ///     in the nested configuration/system.serviceModel tag.
        ///     or
        ///     2) If there is no web.config or App.config file, but it has an interface with
        ///     ServiceContract Annotation and a method definition with ObjectContract Annotation,
        ///     and a class implementing this interface.
        /// </summary>
        /// <param name="analyzerResult"></param>
        /// <returns>Whether a project is a WCF Service or not</returns>
        public override bool IsPresent(AnalyzerResult analyzerResult)
        {
            string projectDir = analyzerResult.ProjectResult.ProjectRootPath;

            string webConfigFile = Path.Combine(projectDir, Rules.Config.Constants.WebConfig);
            string appConfigFile = Path.Combine(projectDir, Rules.Config.Constants.AppConfig);

            if (File.Exists(webConfigFile) || File.Exists(appConfigFile))
            {
                var config = WebConfigManager.LoadWebConfigAsXDocument(projectDir);
                return config.ContainsElement(Constants.WCFServiceElementPath);
            }
            else
            {
                var project = analyzerResult.ProjectResult;

                var interfaces = project.GetAllInterfaceDeclarations().ToList();
                var interfacesWithServiceContract = interfaces
                    .Where(i => i.HasAttribute(Constants.ServiceContractAttribute))
                    .ToList();

                var interfaceWithServiceContractMethods = interfacesWithServiceContract
                    .SelectMany(i => i.GetMethodDeclarations()).ToList();

                var serviceInterfaceMethodWithObjectContract = interfaceWithServiceContractMethods
                    .Where(m => m.HasAttribute(Constants.OperationContractAttribute))
                    .ToList();


                if (serviceInterfaceMethodWithObjectContract.Count != 0)
                {
                    var classes = project.GetAllClassDeclarations().ToList();
                    var classImplementingServiceContractInterface = interfacesWithServiceContract
                        .SelectMany(i => classes.Where(c => c.InheritsInterface(i.Identifier)))
                        .ToList();
                    if (classImplementingServiceContractInterface.Count != 0)
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
}
