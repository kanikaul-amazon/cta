﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using Codelyzer.Analysis;
using CoreWCF.Configuration;
using CTA.Rules.Common.WebConfigManagement;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace CTA.Rules.Update
{
    public class WCFServicePort
    {

        private static readonly string HTTP_PROTOCOL = "http";
        private static readonly string HTTPS_PROTOCOL = "https";
        private static readonly string TCP_PROTOCOL = "nettcp";

        private string _projectPath;
        private AnalyzerResult _analyzerResult;

        public WCFServicePort(string projectPath, AnalyzerResult analyzerResult)
        {
            _projectPath = projectPath;
            _analyzerResult = analyzerResult;
        }

        public void portWCFService()
        {
            if(isConfigBased())
            {

            }
        }

        public bool isConfigBased()
        {
            string webConfigFile = Path.Combine(_projectPath, Rules.Config.Constants.WebConfig);
            string appConfigFile = Path.Combine(_projectPath, Rules.Config.Constants.AppConfig);

            if (File.Exists(webConfigFile) || File.Exists(appConfigFile))
            {
                var config = WebConfigManager.LoadWebConfigAsXDocument(_projectPath);

                var containsServiceModel = config.ContainsElement("config/system.servicemodel");

                return true;
            }

            return true;
        }


        public void generateStartUpClass()
        {

        }

        public void generateProgramFile()
        {

        }

        public void portWCFService(string configFilePath)
        {
            Dictionary<string, int> transportPort = getTransportAndPort(configFilePath);


            string programClass1 = @"class Program {
            static void Main(string[] args)
            {
                WebHost.CreateDefaultBuilder(args)
                 .UseKestrel(options => {
                     options.ListenLocalhost(httpPort);
                     options.Listen(address: IPAddress.Loopback, httpsPort, listenOptions =>
                     {
                         listenOptions.UseHttps(httpsOptions =>
                         {
#if NET472
                          httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
#endif // NET472
                      });
                         if (Debugger.IsAttached)
                         {
                             listenOptions.UseConnectionLogging();
                         }
                     });
                 })
                 .UseNetTcp(tcpPort)
                 .UseStartup<Startup>();

            }";

            string programClass = @"

class Program {
            static void Main(string[] args)
            {
                WebHost.CreateDefaultBuilder(args)
                 .UseKestrel(options => {})
                 .UseStartup<Startup>();

            }";

            replaceProgram(transportPort, programClass);
        }

        public static SyntaxNode replaceProgram(Dictionary<string, int> transportPort, string programClassAsString)
        {
            string httpListen = "{0}.ListenLocalHost({1});\n";
            string httpsListen = @"{0}.Listen(address: IPAddress.Loopback, {1}, listenOptions =>
                     {
                         listenOptions.UseHttps(httpsOptions =>
                         {
                             httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
                         });
                     });\n";

            string netTcpMethodExpression = @"UseNetTcp";

            var optionsBlock = "";

            var tree = CSharpSyntaxTree.ParseText(programClassAsString);

            var root = tree.GetRoot();

            var lambdaExpression = root.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().First();

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
            

            var parameter = lambdaExpression.Parameter;

            if (transportPort.ContainsKey(HTTP_PROTOCOL))
            {
                httpListen = String.Format(httpListen, parameter.Identifier.ValueText, transportPort.GetValueOrDefault(HTTP_PROTOCOL));
                optionsBlock += httpListen;
            }

            if (transportPort.ContainsKey(HTTPS_PROTOCOL))
            {
                httpsListen = String.Format(httpsListen, parameter.Identifier.ValueText, transportPort.GetValueOrDefault(HTTPS_PROTOCOL));
                optionsBlock += httpsListen;
            }

            var block = lambdaExpression.Block;
   
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

            return root;
        }

            public Dictionary<string, int> getTransportAndPort(string configFilePath)
        {
            Configuration configuration = ConfigurationManager
                .OpenMappedMachineConfiguration(new ConfigurationFileMap(configFilePath));

            Dictionary<string, int> transportPortMap = new Dictionary<string, int>();

            var section = ServiceModelSectionGroup.GetSectionGroup(configuration);
            string transport = "";
            int transportPort = 0; //CHANGE, CHECK BEST PRACTICE

            foreach (ServiceElement serviceElement in section.Services?.Services)
            {
                foreach (BaseAddressElement baseAddress in serviceElement.Host?.BaseAddresses)
                {
                    if (!String.IsNullOrEmpty(baseAddress.BaseAddress))
                    {
                        Uri url = new Uri(baseAddress.BaseAddress);
                        if (url.Scheme == Uri.UriSchemeHttps)
                        {
                            transportPortMap.Add(HTTPS_PROTOCOL, url.Port);
                        }
                        else if (url.Scheme == Uri.UriSchemeNetTcp)
                        {
                            transportPortMap.Add(TCP_PROTOCOL, url.Port);
                        }
                        else if (url.Scheme == Uri.UriSchemeHttp)
                        {
                            transportPortMap.Add(HTTP_PROTOCOL, url.Port);
                        }
                    }
                }
            }

            return transportPortMap;
        }
    }
}
