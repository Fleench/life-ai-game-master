using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameMaster.Core.Models;
namespace GameMaster.Core.Services;
public interface IPermissionsService {
    Task GrantAsync(Guid appId, Resource resource, PermissionAction action);
    Task RevokeAsync(Guid appId, Resource resource, PermissionAction action);
    Task<bool> CheckAsync(Guid appId, Resource resource, PermissionAction action);
    Task<IEnumerable<AppPermission>> GetPermissionsAsync(Guid appId);
}
