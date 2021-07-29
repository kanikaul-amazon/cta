﻿namespace CTA.FeatureDetection.ProjectType
{
    public class Constants
    {
        public static readonly string[] NetCoreViewResultTypes = {
            "ViewComponentResult",
            "ViewResult",
            "PartialViewResult"
        };
        public const string ApiControllerAttributeType = "ApiControllerAttribute";
        public const string NetCoreMvcControllerOriginalDefinition = "Microsoft.AspNetCore.Mvc.Controller";
        public const string NetCoreMvcControllerBaseOriginalDefinition = "Microsoft.AspNetCore.Mvc.ControllerBase";
        public const string MvcControllerOriginalDefinition = "System.Web.Mvc.Controller";
        public const string WebApiControllerOriginalDefinition = "System.Web.Http.ApiController";
        public const string SystemWebReferenceIdentifier = "System.Web";
        public const string WebApiNugetReferenceIdentifier = "Microsoft.AspNet.WebApi";
        public const string WebApiReferenceIdentifier = "System.Web.Http";
        public const string MvcNugetReferenceIdentifier = "Microsoft.AspNet.Mvc";
        public const string MvcReferenceIdentifier = "System.Web.Mvc";
        public const string MvcViewsDirectory = "Views";

        public const string AspNetMvcFeatureName = "AspNetMvcFeature";
        public const string AspNetWebApiFeatureName = "AspNetWebApiFeature";
        public const string WebClassLibraryFeatureName = "WebClassLibraryFeature";
        public const string AspNetCoreMvcFeatureName = "AspNetCoreMvcFeature";
        public const string AspNetCoreWebApiFeatureName = "AspNetCoreWebApiFeature";

        internal const string SystemServiceModelElement = "system.serviceModel";
        internal const string WCFClientElement = "client";
        internal const string WCFServiceElement = "services";
        internal const string WCFServiceEndpoint = "endpoint";
        internal const string ServiceContractAttribute = "ServiceContractAttribute";
        internal const string OperationContractAttribute = "OperationContractAttribute";
        internal const string BindingsAttribute = "bindings";

        internal const string ConfigurationElement = "configuration";
        internal static readonly string WCFClientElementPath = $"{ConfigurationElement}/{SystemServiceModelElement}/{WCFClientElement}";
        internal static readonly string WCFServiceElementPath = $"{ConfigurationElement}/{SystemServiceModelElement}/{WCFServiceElement}";
        internal static readonly string WCFServiceEndpointElementPath = $"{ConfigurationElement}/{SystemServiceModelElement}/{WCFServiceEndpointElementPath}";
    }
}
