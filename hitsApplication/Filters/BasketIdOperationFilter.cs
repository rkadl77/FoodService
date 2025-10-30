using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace hitsApplication.Filters
{
    public class BasketIdOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var methodName = context.MethodInfo.Name.ToLower();

            var hasBasketIdParameter = context.MethodInfo.GetParameters()
                .Any(p => p.Name?.ToLower() == "basketid");

            if (hasBasketIdParameter)
            {
                if (methodName.Contains("createorderfromcart"))
                {
                    AddAuthorizationSecurity(operation);
                }
                return;
            }

            var methodsRequiringBasketId = new[]
            {
                "addtocart", "updatequantity", "removefromcart", "clearcart",
                "getcartsummary", "isincart", "debugcart", "createorderfromcart"
            };

            if (methodsRequiringBasketId.Any(x => methodName.Contains(x)))
            {
                if (operation.Parameters == null)
                    operation.Parameters = new List<OpenApiParameter>();

                if (!operation.Parameters.Any(p => p.Name == "Basket-Id"))
                {
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = "Basket-Id",
                        In = ParameterLocation.Header,
                        Required = true,
                        Description = "Unique basket identifier",
                        Schema = new OpenApiSchema { Type = "string" }
                    });
                }
            }

            if (methodName.Contains("createorderfromcart"))
            {
                AddAuthorizationSecurity(operation);
            }
        }

        private void AddAuthorizationSecurity(OpenApiOperation operation)
        {
            if (operation.Security == null)
                operation.Security = new List<OpenApiSecurityRequirement>();

            var hasBearerSecurity = operation.Security.Any(sec =>
                sec.Any(s => s.Key.Reference?.Id == "Bearer"));

            if (!hasBearerSecurity)
            {
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            }
        }
    }
}