<#
This scripts creates the appsettings object used to provide app settings to the agent portal app
#>

Param(
    [string] $acsConnectionString,
    [string] $wpsConnectionString,
    [string] $storageConnectionString,
    [string] $resourceGroup,
    [string] $showCommands = "false"
 )

$acsAgentHubAppSettingFile = Join-Path $PSScriptRoot ..\..\ "ACSAgentHub\local.settings.json"
$resolvedPath = Resolve-Path $acsAgentHubAppSettingFile

Write-Host "Updating " $resolvedPath -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host '$agentHubAppsettings = Invoke-Expression "& ''$(Join-Path '$PSScriptRoot' ''create_agent_hub_appsettings.ps1'')'' -resourceGroup '$resourceGroup' -acsConnectionString ""'$acsConnectionString'"" -wpsConnectionString ""'$wpsConnectionString'"" -storageConnectionString ""'$storageConnectionString'"" "'}
$acsAgentHubLocalSettings = Get-Content $acsAgentHubAppSettingFile -Encoding UTF8 | ConvertFrom-Json -Depth 10

$acsAgentHubLocalSettings.Values.agentHubStorageConnectionString = $storageConnectionString
$acsAgentHubLocalSettings.Values.acsConnectionString = $acsConnectionString
$acsAgentHubLocalSettings.Values.botBaseAddress = "http://localhost:3978/"
$acsAgentHubLocalSettings.Values.useACSManagedIdentity = "false"
$acsAgentHubLocalSettings.Values.webPusSubConnectionString = $wpsConnectionString
$acsAgentHubLocalSettings.Values.webPubSubHubName = "refreshConversations"

# Write updated appsettings to appsettings file
ConvertTo-Json $acsAgentHubLocalSettings -Depth 10 | Set-Content $acsAgentHubAppSettingFile -Encoding UTF8

Write-Host " - Done." -ForegroundColor Green

return $acsAgentHubLocalSettings | ConvertTo-Json
