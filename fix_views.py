import re

def fix_file(filepath, pattern):
    try:
        with open(filepath, 'r') as f:
            content = f.read()
        content = re.sub(pattern, '', content, flags=re.DOTALL)
        with open(filepath, 'w') as f:
            f.write(content)
    except:
        pass

# Fix MainView
with open("C#/GameMaster.App/Views/MainView.axaml", 'r') as f:
    c = f.read()
# <views:PendingPermissionsView ... />
c = re.sub(r'<views:PendingPermissionsView.*?\/>', '', c, flags=re.DOTALL)
with open("C#/GameMaster.App/Views/MainView.axaml", 'w') as f:
    f.write(c)

# Fix AppHubView
with open("C#/GameMaster.App/Views/AppHubView.axaml", 'r') as f:
    c = f.read()
c = re.sub(r'<DataTemplate DataType="vm:PermissionItemViewModel">.*?</DataTemplate>', '', c, flags=re.DOTALL)
with open("C#/GameMaster.App/Views/AppHubView.axaml", 'w') as f:
    f.write(c)

