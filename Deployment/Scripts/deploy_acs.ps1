#Requires -Version 6

Param(
    [string] $hubName,
    [string] $resourceGroup,
    [string] $showCommands = "false",
    [string] $logfile = $(Join-Path $PSScriptRoot .. "deploy_log.txt" -Resolve)
 )

# Create Azure Communication Service
$communicationServerName = $hubName + "ACS"
Write-Host "Creating the Azure Communication Service that provides the Chat Thread agents use to speak to users" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az communication create --name ""$communicationServerName"" --resource-group ""$resourceGroup"" --location ""Global"" --data-location ""United States"" " -NoNewline }
az communication create `
  --name "$communicationServerName" `
  --resource-group "$resourceGroup" `
  --location "Global" `
  --data-location "United States" `
  2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

$acsCommunicationService = az communication list-key --name "$communicationServerName" --resource-group "$resourceGroup" | ConvertFrom-Json -Depth 10

# Create return object
$result = [PSCustomObject]@{
  acsName = $communicationServerName
  acsConnectionString = $acsCommunicationService.primaryConnectionString
}

if ($showCommands.ToLower() -eq "true") {
    Write-Host "Object returned by deploy_acs.ps1 script:" -ForegroundColor Green
    ConvertTo-Json $result -Depth 10 | Write-Host -ForegroundColor Green
}

return $result | ConvertTo-Json
