using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Swagger
{
    /// <summary>
    /// The webhook action reads the request by hand and has no bound parameters, so Swagger UI has nothing
    /// to render and sends an EMPTY body (that is the "0 chars" seen when testing from Swagger).
    /// This filter documents the two shapes a webhook can have, which makes Swagger UI show real inputs:
    ///   application/x-www-form-urlencoded : data + signature   (LiqPay, exactly like the real callback)
    ///   application/json                  : free-form JSON      (Stripe, WayForPay)
    /// </summary>
    public sealed class WebhookRequestBodyOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var path = context.ApiDescription.RelativePath;

            if (path is null ||
                !path.StartsWith("api/Payments/webhook/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = false,
                Description =
                    "LiqPay: choose application/x-www-form-urlencoded and fill 'data' and 'signature'. " +
                    "Stripe / WayForPay: choose application/json and paste the signed JSON " +
                    "(Stripe also needs the Stripe-Signature header, so use the Stripe CLI for it).",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/x-www-form-urlencoded"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["data"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "LiqPay: base64 encoded JSON of the payment result."
                                },
                                ["signature"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "LiqPay: base64(sha1(private_key + data + private_key))."
                                }
                            }
                        }
                    },
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            AdditionalPropertiesAllowed = true
                        }
                    }
                }
            };
        }
    }
}
