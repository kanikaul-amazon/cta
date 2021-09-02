using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using Codelyzer.Analysis;
using Codelyzer.Analysis.Model;
using CTA.FeatureDetection.Common.Extensions;
using CTA.FeatureDetection.Common.WCFConfigUtils;
using CTA.Rules.Common.WebConfigManagement;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CTA.Rules.PortCore
{
    public class WCFServicePort
    {

        private static readonly string HTTP_PROTOCOL = "basichttpbinding";
        private static readonly string HTTPS_PROTOCOL = "basichttpsbinding";
        private static readonly string TCP_PROTOCOL = "nettcpbinding";

        private string _projectPath;
        private AnalyzerResult _analyzerResult;

        public WCFServicePort(string projectPath, AnalyzerResult analyzerResult)
        {
            _projectPath = projectPath;
            _analyzerResult = analyzerResult;
        }

        public bool isConfigBased()
        {
            string webConfigFile = Path.Combine(_projectPath, Rules.Config.Constants.WebConfig);
            string appConfigFile = Path.Combine(_projectPath, Rules.Config.Constants.AppConfig);

            if (File.Exists(webConfigFile) || File.Exists(appConfigFile))
            {
                var config = WebConfigManager.LoadWebConfigAsXDocument(_projectPath);

                var containsServiceModel = config.ContainsElement("config/system.servicemodel");

                if(containsServiceModel)
                {
                    return true;
                }
            }

            return false;
        }

        public SyntaxNode replaceProgramFile(SyntaxTree programTree)
        {

            Dictionary<string, int> transportPort = new Dictionary<string, int>();
            if (isConfigBased())
            {
                string projectDir = _analyzerResult.ProjectResult.ProjectRootPath;
                transportPort = getTransportAndPort(projectDir);
            }
            else
            {
                ProjectWorkspace projectDir = _analyzerResult.ProjectResult;
                transportPort = getTransportAndPort(projectDir);
            }

            if(transportPort.IsNullOrEmpty())
            {
                return programTree.GetRoot();
            }

            var newRoot = replaceProgramNode(transportPort, programTree);
            return newRoot;
        }

        public static SyntaxNode replaceProgramNode(Dictionary<string, int> transportPort, SyntaxTree programTree)
        {
            string httpListen = "{0}.ListenLocalHost({1});\n";
            string httpsListen = @"{0}.Listen(address: IPAddress.Loopback, {1}, listenOptions =>
                     {{
                         listenOptions.UseHttps(httpsOptions =>
                         {{
                             httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
                         }});
                     }});\n";
            string netTcpMethodExpression = @"UseNetTcp";

            var tree = programTree;
            var root = tree.GetRoot();

            var lambdaExpressionList = root.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>();

            if(lambdaExpressionList.IsNullOrEmpty())
            {
                return root;
            }

            var lambdaExpression = lambdaExpressionList.First();

            var block = lambdaExpression.Block;
            var newBlock = block;

            var parameter = lambdaExpression.Parameter;

            if (transportPort.ContainsKey(HTTP_PROTOCOL))
            {
                httpListen = String.Format(httpListen, parameter.Identifier.ValueText, transportPort.GetValueOrDefault(HTTP_PROTOCOL));            
                newBlock = block.AddStatements(SyntaxFactory.ParseStatement(httpListen));
            }

            if (transportPort.ContainsKey(HTTPS_PROTOCOL))
            {
                httpsListen = String.Format(httpsListen, parameter.Identifier.ValueText, transportPort.GetValueOrDefault(HTTPS_PROTOCOL));
                newBlock = newBlock.AddStatements(SyntaxFactory.ParseStatement(httpsListen));
            }

            var newLambdaExpression = lambdaExpression.ReplaceNode(block, newBlock);

            root = root.ReplaceNode(lambdaExpression, newLambdaExpression);

            var memberAccessExpressions = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToList();

            MemberAccessExpressionSyntax kestrelInvocationNode = null;

            foreach (MemberAccessExpressionSyntax memberAccessExpression in memberAccessExpressions)
            {
                if (memberAccessExpression.Name.Identifier.Text.Equals("UseStartup"))
                {
                    kestrelInvocationNode = memberAccessExpression;
                    break;
                }
            }

            if (transportPort.ContainsKey(TCP_PROTOCOL))
            {
                var netTCPExpression = SyntaxFactory
                            .MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            kestrelInvocationNode.Expression,
                            SyntaxFactory.IdentifierName(netTcpMethodExpression)
                            );
                var netTcpInvocation = SyntaxFactory.InvocationExpression(netTCPExpression, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] {
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(transportPort.GetValueOrDefault(TCP_PROTOCOL))))
               })));
               var kestrelInvocationWithNetTcp = kestrelInvocationNode.WithExpression(netTcpInvocation);
               root = root.ReplaceNode(kestrelInvocationNode, kestrelInvocationWithNetTcp);
            }

            return root;
        }


        public static Dictionary<string, int> getTransportAndPort(string projectDir)
        {
            Dictionary<string, int> transportPortMap = new Dictionary<string, int>();
            Dictionary<string, List<string>> bindingsTransportMap = new Dictionary<string, List<string>>();

            WCFBindingAndTransportUtil.configBasedCheck(projectDir, bindingsTransportMap);

            AddBinding(bindingsTransportMap, transportPortMap);

            return transportPortMap;
        }

        public static Dictionary<string, int> getTransportAndPort(ProjectWorkspace project)
        {
            Dictionary<string, int> transportPortMap = new Dictionary<string, int>();
            Dictionary<string, List<string>> bindingsTransportMap = new Dictionary<string, List<string>>();

            WCFBindingAndTransportUtil.codeBasedCheck(project, bindingsTransportMap);

            AddBinding(bindingsTransportMap, transportPortMap);

            return transportPortMap;
        }

        public static void AddBinding(Dictionary<string, List<string>> bindingsTransportMap, Dictionary<string, int> transportPortMap)
        {
            foreach (var binding in bindingsTransportMap.Keys)
            {
                if (binding == "basichttpbinding")
                {
                    transportPortMap.Add(binding, 8080);
                }
                else if (binding == "basichttpsbinding")
                {
                    transportPortMap.Add(binding, 8888);
                }
                else if (binding == "nettcpbinding")
                {
                    transportPortMap.Add(binding, 8000);
                }
            }
        }
    }
}
