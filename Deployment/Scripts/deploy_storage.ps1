#Requires -Version 6

Param(
    [string] $hubName,
    [string] $resourceGroup,
    [string] $location,
    [string] $showCommands = "false",
    [string] $logfile = $(Join-Path $PSScriptRoot .. "deploy_log.txt" -Resolve)
 )

# Create Storage account
# Storage names must be between 3 and 24 characters long and use numbers and lower-case letters only
$storageAccountName = $hubName.ToLower()
Write-Host "Creating the Storage Account used by the ACS Agent Hub" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az storage account create -n ""$storageAccountName"" -l ""$location"" -g ""$resourceGroup"" --sku ""Standard_LRS"" " -NoNewline }
az storage account create `
  -n "$storageAccountName" `
  -l "$location" `
  -g "$resourceGroup" `
  --sku "Standard_LRS" `
  2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

# Grab connection string 
if ($showCommands.ToLower() -eq "true") { 
  Write-Host "Grab connection string for the Azure Storage" -NoNewline -ForegroundColor Green
  Write-Host "az storage account show-connection-string -g ""$resourceGroup"" -n ""$storageAccountName"" " 
}
$acsAgentHubStorage = az storage account show-connection-string -g "$resourceGroup" -n "$storageAccountName" | ConvertFrom-Json -Depth 10
if ($showCommands.ToLower() -eq "true") { Write-Host " - Done." -ForegroundColor Green }

# Create return object
$result = [PSCustomObject]@{
  storageAccountName = $storageAccountName
  storageConnectionString = $acsAgentHubStorage.connectionString
}

if ($showCommands.ToLower() -eq "true") {
    Write-Host "Object returned by deploy_storage.ps1 script:" -ForegroundColor Green
    ConvertTo-Json $result -Depth 10 | Write-Host -ForegroundColor Green
}

return $result | ConvertTo-Json
