#Requires -Version 6

Param(
    [string] $hubName,
    [string] $resourceGroup,
    [string] $communicationServerName,
    [string] $showCommands = "false",
    [string] $logfile = $(Join-Path $PSScriptRoot .. "deploy_log.txt")
 )

 $azureSubscriptionId = az account show --query id --output tsv

# Create Event Grid
$eventGridName = $hubName + "EG"
Write-Host "Creating Event Grid that brokers ACS messages to bot" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az eventgrid system-topic create --name ""$eventGridName"" --resource-group ""$resourceGroup"" --source ""/subscriptions/$azureSubscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Communication/CommunicationServices/$communicationServerName"" --topic-type ""Microsoft.Communication.CommunicationServices"" --location ""Global"" " -NoNewline }
az eventgrid system-topic create `
  --name "$eventGridName" `
  --resource-group "$resourceGroup" `
  --source "/subscriptions/$azureSubscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Communication/CommunicationServices/$communicationServerName" `
  --topic-type "Microsoft.Communication.CommunicationServices" `
  --location "Global" `
  2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

# Create return object
$result = [PSCustomObject]@{
  eventGridName = $eventGridName
}

if ($showCommands.ToLower() -eq "true") {
    Write-Host "Object returned by deploy_eventgrid.ps1 script:" -ForegroundColor Green
    ConvertTo-Json $result -Depth 10 | Write-Host -ForegroundColor Green
}

return $result | ConvertTo-Json
