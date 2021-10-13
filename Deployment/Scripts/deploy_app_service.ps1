#Requires -Version 6

Param(
    [string] $hubName,
    [string] $resourceGroup,
    [string] $showCommands = "false", 
    [string] $logfile = $(Join-Path $PSScriptRoot .. "deploy_log.txt" -Resolve)
 )

# Create App Service Plan for Azure Function
$appServicePlanName = $hubName + "ASP"
Write-Host "Creating a dedicated App Service Plan used by the ACS Agent Hub Function App so its always on" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az functionapp plan create --name ""$appServicePlanName"" --resource-group ""$resourceGroup --location ""$location"" --sku B1 " -NoNewline }
az functionapp plan create `
  --name "$appServicePlanName" `
  --resource-group "$resourceGroup" `
  --location "$location" `
  --sku B1 `
  2>> "$logFile" | Out-Null
  Write-Host " - Done." -ForegroundColor Green

# Create return object
$result = [PSCustomObject]@{
  appServicePlanName = $appServicePlanName
}

if ($showCommands.ToLower() -eq "true") {
    Write-Host "Object returned by deploy_app_service.ps1 script:" -ForegroundColor Green
    ConvertTo-Json $result -Depth 10 | Write-Host -ForegroundColor Green
}

return $result | ConvertTo-Json
