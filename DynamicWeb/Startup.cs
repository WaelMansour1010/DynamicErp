using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MyERP.Startup))]
namespace MyERP
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
