<#
This scripts creates the appsettings object used to provide app settings to the agent portal app
#>

Param(
    [string] $acsConnectionString,
    [string] $wpsConnectionString,
    [string] $storageConnectionString,
    [string] $resourceGroup
 )

$acsAgentHubAppSettingFile = Join-Path $PSScriptRoot ..\..\ "ACSAgentHub\local.settings.json"
$acsAgentHubLocalSettings = Get-Content $acsAgentHubAppSettingFile -Encoding UTF8 | ConvertFrom-Json -Depth 10

$acsAgentHubLocalSettings.Values.agentHubStorageConnectionString = $storageConnectionString
$acsAgentHubLocalSettings.Values.acsConnectionString = $acsConnectionString
$acsAgentHubLocalSettings.Values.botBaseAddress = "http://localhost:3978/"
$acsAgentHubLocalSettings.Values.useACSManagedIdentity = "false"
$acsAgentHubLocalSettings.Values.webPusSubConnectionString = $wpsConnectionString
$acsAgentHubLocalSettings.Values.webPubSubHubName = "refreshConversations"

# Write updated appsettings to appsettings file
ConvertTo-Json $acsAgentHubLocalSettings -Depth 10 | Set-Content $acsAgentHubAppSettingFile -Encoding UTF8

$resolvedPath = Resolve-Path $acsAgentHubAppSettingFile
Write-Host "Updated " $resolvedPath -ForegroundColor Green

return $acsAgentHubLocalSettings | ConvertTo-Json
