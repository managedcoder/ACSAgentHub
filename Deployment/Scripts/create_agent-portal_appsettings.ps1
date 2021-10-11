<#
This scripts creates the appsettings object used to provide app settings to the agent portal app
#>

Param(
    [string] $webPubSubName,
    [string] $resourceGroup,
    [string] $showCommands = "false"
 )

$settingFile =  $(Join-Path -Path $PSScriptRoot -ChildPath "..\..\agent-portal\src\settings\appsettings.ts")
$resolvedPath = Resolve-Path $settingFile

# Add -NoNewline back in when "preview warning goes away"
Write-Host "Updating " $resolvedPath -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") {Write-Host '$agentPortalAppsettings = Invoke-Expression "& ''$(Join-Path '$PSScriptRoot' ''create_agent-portal_appsettings.ps1'')'' -webPubSubName ""'$webPubSubName'"" -resourceGroup '$resourceGroup' -Encoding UTF8"'}

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

Write-Host " - Done." -ForegroundColor Green

return $appsettings | ConvertTo-Json


