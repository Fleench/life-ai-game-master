using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaster.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class PermissionGuardAttribute : Attribute, IAsyncActionFilter
{
    private readonly Resource _resource;
    private readonly PermissionAction _action;

    public PermissionGuardAttribute(Resource resource, PermissionAction action)
    {
        _resource = resource;
        _action = action;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Items.TryGetValue("ConnectedApp", out var appObj) && appObj is ConnectedApp app)
        {
            var permissionsService = context.HttpContext.RequestServices.GetRequiredService<IPermissionsService>();
            
            var hasPermission = await permissionsService.CheckAsync(app.AppId, _resource, _action);
            if (!hasPermission)
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = 403,
                    Title = "Forbidden",
                    Detail = $"App does not have {_action} permission for {_resource}."
                })
                {
                    StatusCode = 403
                };
                return;
            }
        }
        else
        {
            // If app is not found in items, the auth middleware failed to set it, meaning it's unauthorized (or skipped if we messed up)
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
