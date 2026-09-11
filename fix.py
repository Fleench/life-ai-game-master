import re

with open("C#/GameMaster.App/Android/Binder/GameMasterBinderService.cs", "r") as f:
    content = f.read()

# Remove GrantPermissionAsync
content = re.sub(r'case "GrantPermissionAsync":.*?break;', '', content, flags=re.DOTALL)
# Remove RevokePermissionAsync
content = re.sub(r'case "RevokePermissionAsync":.*?break;', '', content, flags=re.DOTALL)
# Remove RequestPermissionAsync
content = re.sub(r'case "RequestPermissionAsync":.*?break;', '', content, flags=re.DOTALL)
# Remove GetMyPermissionsAsync case
content = re.sub(r'case "GetMyPermissionsAsync":\s*result = await GetMyPermissions\(\);\s*break;', '', content)
# Remove GetMyPermissions method
content = re.sub(r'public async Task<IEnumerable<AppPermission>> GetMyPermissions\(\).*?}', '', content, flags=re.DOTALL)

with open("C#/GameMaster.App/Android/Binder/GameMasterBinderService.cs", "w") as f:
    f.write(content)
