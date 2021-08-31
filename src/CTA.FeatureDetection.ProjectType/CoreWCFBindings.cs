using System;
using System.Collections.Generic;
using System.Text;

namespace CTA.FeatureDetection.ProjectType
{
    class CoreWCFBindings
    {
        public static readonly Dictionary<string, List<string>> CORE_WCF_BINDINGS =
            new Dictionary<string, List<string>>
            {
                { "nettcpbinding", new List<string> { "none", "transportwithmessagecredential" } },
                { "wshttpbinding", new List<string> { "none" } },
                { "basichttpbinding", new List<string> { "none", "transportwithmessagecredential" } }
            };
    }
}
