<#
This scripts creates the appsettings object used to provide app settings to the agent portal app
#>

Param(
    [string] $hubName,
    [string] $resourceGroup
 )

$webPubSubName = $hubName + "WPS"
$settingFile =  $(Join-Path -Path $PSScriptRoot -ChildPath "..\..\agent-portal\src\settings\appsettings.ts")

$appsettings = @{    
  agentHubBaseAddress = 'http://localhost:7071' # Default to local settings so we can run local out of the box
  webPubSubConnectionString = $null
  webPubSubHubName = 'refreshConversations' # PubSub hub name agent-hub will be listening to
}

# Clear content of appsetting file which effectively overwrites the file contents with the new settings
if (Test-Path $settingFile) {
    Clear-Content $settingFile -Force | Out-Null
}

# Grab the Web PubSub's key information so we can dig out its connection string
$keyInfo = az webpubsub key show --name "$webPubSubName" --resource-group "$resourceGroup" | ConvertFrom-Json
$appsettings.webPubSubConnectionString = $keyInfo.primaryConnectionString

Add-Content -Path $settingFile -Value "// App settings for the agent-portal"
Add-Content -Path $settingFile -Value "export const appsettings = {"
Add-Content -Path $settingFile -Value "    agentHubBaseAddress: '$($appsettings.agentHubBaseAddress)',"
Add-Content -Path $settingFile -Value "    webPusSubConnectionString: '$($appsettings.webPubSubConnectionString)',"
Add-Content -Path $settingFile -Value "    webPubSubHubName: '$($appsettings.webPubSubHubName)'"
Add-Content -Path $settingFile -Value "};"

$resolvedPath = Resolve-Path $settingFile
Write-Host "Updated " $resolvedPath -ForegroundColor Green

return $appsettings | ConvertTo-Json


