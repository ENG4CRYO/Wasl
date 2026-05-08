using Microsoft.AspNetCore.Builder;

namespace Wasl.API.Extensions
{
    public static class PolicyCollection
    {
        public static HeaderPolicyCollection policyCollection(WebApplication app)
        {
            var policy = new HeaderPolicyCollection()
                .AddDefaultSecurityHeaders()
                 .RemoveServerHeader()
                .AddPermissionsPolicy(builder =>
                {
                     builder.AddCamera().None();
                     builder.AddMicrophone().None();
                     builder.AddGeolocation().None();
                });

            if (app.Environment.IsProduction())
            {
                policy.AddStrictTransportSecurity(31536000,true ,true)
                      .AddContentSecurityPolicy(csp =>
                      {
                          csp.AddDefaultSrc().Self();
                          csp.AddScriptSrc().Self();
                          csp.AddStyleSrc().Self();
                          csp.AddImgSrc().Self().From("data:");
                      });
            }


            return policy;

        }
    }
}
