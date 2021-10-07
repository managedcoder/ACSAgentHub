#Requires -Version 6

Param(
    [string] $hubName,
    [string] $resourceGroup,
    [string] $showCommands = "false",
    [string] $logfile = $(Join-Path $PSScriptRoot .. "deploy_log.txt")
 )

# Create App Insights instance for the Azure Function
$appInsightsName = $hubName + "AI"
Write-Host "Creating the App Insights instance used by the ACS Agent Hub Function App" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az resource create -g ""$resourceGroup"" -n ""$appInsightsName"" --resource-type ""Microsoft.Insights/components"" --properties '{\"Application_Type\":\"web\"}' " -NoNewline }
az resource create `
  -g "$resourceGroup" `
  -n "$appInsightsName" `
  --resource-type "Microsoft.Insights/components" `
  --properties '{\"Application_Type\":\"web\"}' `
  2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

# Create return object
$result = [PSCustomObject]@{
  appInsightsName = $appInsightsName
}

if ($showCommands.ToLower() -eq "true") {
    Write-Host "Object returned by deploy_app_insights.ps1 script:" -ForegroundColor Green
    ConvertTo-Json $result -Depth 10 | Write-Host -ForegroundColor Green
}

return $result | ConvertTo-Json
