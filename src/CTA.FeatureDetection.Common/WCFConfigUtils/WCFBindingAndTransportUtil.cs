using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Codelyzer.Analysis;
using Codelyzer.Analysis.Model;
using CTA.FeatureDetection.Common.Extensions;
using CTA.Rules.Common.WebConfigManagement;

namespace CTA.FeatureDetection.Common.WCFConfigUtils
{
    public class WCFBindingAndTransportUtil
    {
        public static Dictionary<string, List<string>> getBindingAndTransport(AnalyzerResult analyzerResult)
        {
            string projectDir = analyzerResult.ProjectResult.ProjectRootPath;

            string webConfigFile = Path.Combine(projectDir, Rules.Config.Constants.WebConfig);
            string appConfigFile = Path.Combine(projectDir, Rules.Config.Constants.AppConfig);

            var bindingsTransportMap = new Dictionary<string, List<string>>();

            if (File.Exists(webConfigFile) || File.Exists(appConfigFile))
            {
                configBasedCheck(projectDir, bindingsTransportMap);
            }

            var projectWorkspace = analyzerResult.ProjectResult;

            codeBasedCheck(projectWorkspace, bindingsTransportMap);

            return bindingsTransportMap;
        }

        public static void configBasedCheck(string projectDir, Dictionary<string, List<string>> bindingsTransportMap)
        {
            var config = WebConfigManager.LoadWebConfigAsXDocument(projectDir);

            var containsBindingElement = config.ContainsElement(Constants.WCFBindingElementPath);

            if (containsBindingElement)
            {
                bindingTagCheck(config, bindingsTransportMap);
            }

            var containsProtocolMappingElement = config.ContainsElement(Constants.ProtocolMappingAttribute);

            if (containsProtocolMappingElement)
            {
                protocolTagCheck(config, bindingsTransportMap);
            }
        }

        /// <summary>
        /// Given XML Config with <bindings> element, check for binding and transport security.
        /// </summary>
        /// <param name="config">XML object for config</param>
        public static void bindingTagCheck(WebConfigXDocument config, Dictionary<string, List<string>> bindingsTransportMap)
        {
            var bindingsElement = config.GetElementByPath(Constants.WCFBindingElementPath);

            var bindingsList = bindingsElement.Elements();

            foreach (var binding in bindingsList)
            {
                var bindingName = binding.Name.ToString().ToLower();

                var bindingElements = binding.Elements();
                foreach (var bindingElement in bindingElements)
                {
                    var securityList = bindingElement.Descendants(Constants.SecurityElement);

                    if (securityList.IsNullOrEmpty())
                    {
                        bindingsTransportMap.AddKeyValue(bindingName, Constants.NoneMode);
                    }

                    foreach (var securityElement in securityList)
                    {
                        var modeName = securityElement.Attribute(Constants.ModeAttribute);

                        if (modeName != null)
                        {
                            bindingsTransportMap.AddKeyValue(bindingName, modeName.Value.ToLower());
                        }
                        else
                        {
                            bindingsTransportMap.AddKeyValue(bindingName, Constants.NoneMode);
                        }
                    }
                }
            }
        }

        public static void protocolTagCheck(WebConfigXDocument config, Dictionary<string, List<string>> bindingsTransportMap)
        {
            var protocolElement = config.GetElementByPath(Constants.ProtocolMappingAttribute);

            var addProtocolElementsList = protocolElement.Elements(Constants.AddElement);
            foreach (var addProtocolElement in addProtocolElementsList)
            {
                var binding = addProtocolElement.Attribute(Constants.BindingAttribute);

                if (binding != null)
                {
                    var bindingName = binding.Value.ToLower();
                    bindingsTransportMap.AddKeyValue(bindingName, Constants.NoneMode);
                }
            }
        }

        public static void codeBasedCheck(ProjectWorkspace project, Dictionary<string, List<string>> bindingsTransportMap)
        {
            IEnumerable<InvocationExpression> addEnpointInvocations = project.GetInvocationExpressionsByMethodName(Constants.AddServiceEndpointType);

            foreach (var addEnpointInvocation in addEnpointInvocations)
            {
                var argumentCount = addEnpointInvocation.Arguments.Count();

                if (argumentCount == 1)
                {
                    var endpointIdentifier = addEnpointInvocation.Arguments.First();

                    IEnumerable<ObjectCreationExpression> serviceEndpointObjectExpressions = project.GetObjectCreationExpressionBySemanticClassType(Constants.ServiceEndpointClass);

                    var endpointArgumentObjects = serviceEndpointObjectExpressions.
                        SelectMany(s => s.GetObjectCreationExpressionBySemanticNamespace(Constants.SystemServiceModelClass));

                    var bindingArgumentObjects = endpointArgumentObjects.Where(e => e.SemanticClassType != Constants.EndpointAddressType);

                    var bindingNames = bindingArgumentObjects.Select(b => b.SemanticClassType);

                    foreach (var bindingName in bindingNames)
                    {
                        var bindingNameFormatted = bindingName.ToString().ToLower();
                        bindingsTransportMap.AddKeyValue(bindingName, Constants.NoneMode);
                    }
                }

                var objectDeclarations = addEnpointInvocation.GetObjectCreationExpressionBySemanticNamespace(Constants.SystemServiceModelClass);
                if(objectDeclarations.IsNullOrEmpty())
                {
                    break;
                }

                var objectDeclaration = objectDeclarations.First();

                if (objectDeclarations != null)
                {
                    var bindingName = objectDeclaration.SemanticClassType.ToLower();

                    bindingsTransportMap.AddKeyValue(bindingName, Constants.NoneMode);
                }
            }
        }
    }
}
