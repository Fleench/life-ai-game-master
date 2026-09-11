#!/bin/bash
set -e

cd /run/media/glenn/shared-files/Projects/LIFE_AI_APPS/game-master

# Clean first to fix the Avalonia AVLN9999 error
echo "Cleaning..."
dotnet clean C#/GameMaster.sln

# Step 2
echo "Publishing App..."
dotnet publish C#/GameMaster.App/GameMaster.App.csproj -c Release -f net9.0-android

# Step 3
echo "Publishing CoinApp..."
dotnet publish C#/GameMaster.CoinApp/GameMaster.CoinApp.Android/GameMaster.CoinApp.Android.csproj -c Release

# Step 4
echo "Connecting to ADB..."
adb connect 192.168.1.119:43711

# Step 5
echo "Uninstalling old apps..."
adb uninstall com.gamemaster.app || true
adb uninstall com.CompanyName.GameMaster.CoinApp || true

# Step 6
echo "Installing new apps..."
APP_APK=$(find C#/GameMaster.App/bin/Release/net9.0-android -name "*-Signed.apk" | head -n 1)
COIN_APK=$(find C#/GameMaster.CoinApp/GameMaster.CoinApp.Android/bin/Release/net9.0-android -name "*-Signed.apk" | head -n 1)

adb install -r "$APP_APK"
adb install -r "$COIN_APK"

# Step 7
echo "Sending notification..."
curl -d "GameMaster Grid UI, SDK Auto-Launch, and visibility fixes are complete! APKs reinstalled!" ntfy.sh/agy_done

echo "Deployment complete!"
