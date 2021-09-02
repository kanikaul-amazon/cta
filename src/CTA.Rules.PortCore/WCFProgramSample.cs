
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace CTA.Rules.PortCore
{
    class WCFSampleProgram
    {
        static void WCFSample()
        {
            WebHost.CreateDefaultBuilder()
             .UseKestrel(options => { });
        }
    }
}
