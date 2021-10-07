#Requires -Version 6

Param(
    [string] $hubName,
    [string] $resourceGroup,
    [string] $storageAccountName,
    [string] $functionAppServicePlanName,
    [string] $appInsightsName,
    [string] $showCommands = "false"
 )

# Create Azure Function
$functionAppName = $hubName + "FA"
Write-Host "Creating the ACS Agent Hub Function App" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az functionapp create --name ""$functionAppName"" --resource-group ""$resourceGroup"" --storage-account ""$storageAccountName"" --plan ""$functionAppServicePlanName"" --app-insights ""$appInsightsName"" --functions-version ""2"" " -NoNewline }
az functionapp create `
  --name "$functionAppName" `
  --resource-group "$resourceGroup" `
  --storage-account "$storageAccountName" `
  --plan "$functionAppServicePlanName" `
  --app-insights "$appInsightsName" `
  --functions-version "2" `
  2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

# Added this to see if it gets rid of "WARNING: Setting SCM_DO_BUILD_DURING_DEPLOYMENT to false"
# See this link for details: https://docs.microsoft.com/en-us/azure/azure-functions/functions-deployment-technologies#remote-build-on-linux
#$env:ENABLE_ORYX_BUILD = "true"
#$env:SCM_DO_BUILD_DURING_DEPLOYMENT = "true"

# Publish ACS Agent Hub code
$projectDirectoryForACSAgentHub = Join-Path -Path $PSScriptRoot -ChildPath "..\..\ACSAgentHub" -Resolve
$publishFolder = "$projectDirectoryForACSAgentHub\bin\Release\netcoreapp3.1\publish"

Write-Host "Publishing ACS Agent Hub code to local folder so it can be zipped" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "dotnet publish -c Release ""$projectDirectoryForACSAgentHub"" " -NoNewline }
dotnet publish `
  -c Release `
  "$projectDirectoryForACSAgentHub" `
  2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

# Create ACS Agent Hub deployment zip file
$publishZip = "ACSAgentHubPublish.zip"
Write-Host "Creating ACS Agent Hub code deployment zip file" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "[io.compression.zipfile]::CreateFromDirectory(""$publishFolder"", ""$publishZip"")" -NoNewline }

if (Test-path $publishZip) { 
    Remove-item "$publishZip" 2>> "$logFile" | Out-Null 
}

Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory("$publishFolder", "$publishZip") `
  2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

# Deploy ACS Agent Hub zipped package
Write-Host "Deploying ACS Agent Hub zipped package" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az functionapp deployment source config-zip -g ""$resourceGroup"" -n ""$functionAppName"" --src ""$publishZip"" " -NoNewline }
az functionapp deployment source config-zip `
 -g "$resourceGroup" `
 -n "$functionAppName" `
 --src "$publishZip" `
 2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

# Create ACS Agent Hub application settings in Function App
Write-Host "Creating ACS Agent Hub application settings in Function App" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az functionapp config appsettings set -n ""$functionAppName"" -g ""$resourceGroup"" --settings ""agentHubStorageConnectionString= "" ""acsConnectionString= "" ""botBaseAddress= "" ""useACSManagedIdentity=false"" ""webPusSubConnectionString= "" ""webPubSubHubName= "" " -NoNewline }
az functionapp config appsettings set -n $functionAppName -g $resourceGroup --settings `
    "agentHubStorageConnectionString= " `
    "acsConnectionString= " `
    "botBaseAddress= " `
    "useACSManagedIdentity=false" `
    "webPusSubConnectionString= " `
    "webPubSubHubName= " `
    2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

# Set a development usage quota limit (in GB's) to address DOS attacks or run-away compute
Write-Host "Setting a development usage quota on ACS Agent Hub Function App - remove or modify this in production" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az functionapp update -g ""$resourceGroup"" -n ""$functionAppName"" --set ""dailyMemoryTimeQuota=50000"" " -NoNewline }
az functionapp update `
  -g "$resourceGroup" `
  -n "$functionAppName" `
  --set "dailyMemoryTimeQuota=50000" `
  2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

# Create return object
$result = [PSCustomObject]@{
  functionAppName = $functionAppName
}

if ($showCommands.ToLower() -eq "true") {
    Write-Host "Object returned by deploy_function_app.ps1 script:" -ForegroundColor Green
    ConvertTo-Json $result -Depth 10 | Write-Host -ForegroundColor Green
}

return $result | ConvertTo-Json
