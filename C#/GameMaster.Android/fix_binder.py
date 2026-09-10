import re

with open('GameMasterBinderService.cs', 'r') as f:
    content = f.read()

# Replace Resource with CoreResource
content = content.replace('using GameMaster.Core.Models;', 'using GameMaster.Core.Models;\nusing CoreResource = GameMaster.Core.Models.Resource;')
content = re.sub(r'\bResource\b', 'CoreResource', content)

# Fix app.Id to app.AppId
content = content.replace('app.Id', 'app.AppId')

# Fix pointsService calls
# GetBalanceAsync returns CurrencyPool?. We want int? So we get .Balance
content = content.replace('await pointsService.GetBalanceAsync(CoreResource.ExpPoints)', '(await pointsService.GetBalanceAsync(CoreResource.ExpPoints.ToString()))?.Balance ?? 0')
content = content.replace('await pointsService.GetBalanceAsync(CoreResource.Coins)', '(await pointsService.GetBalanceAsync(CoreResource.Coins.ToString()))?.Balance ?? 0')
content = content.replace('pointsService.AwardAsync(resource, amount, app.AppId.ToString())', 'pointsService.AwardPointsAsync(resource.ToString(), amount)')
content = content.replace('pointsService.SpendAsync(resource, amount, app.AppId.ToString())', 'pointsService.SpendPointsAsync(resource.ToString(), amount)')

# Fix inventory calls
content = content.replace('ListItemsAsync()', 'GetItemsAsync()')

# Fix permissions call
content = content.replace('GetAppPermissionsAsync', 'GetPermissionsAsync')

with open('GameMasterBinderService.cs', 'w') as f:
    f.write(content)
