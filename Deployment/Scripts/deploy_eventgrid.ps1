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

#$eventSubscriptionName = $hubName + "EGS"
#Write-Host "Creating Event Subscription that listens for ACS Messages" -ForegroundColor Green
#if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az eventgrid event-subscription create --name $eventGridName" -NoNewline }
#az eventgrid event-subscription create `
#  --name $eventGridName `
#  --type webhook `
#  --endpoint https://contoso.azurewebsites.net/api/f1?code=code
 
# Create custom topic
#$customTopicName = $hubName + "ACS"
#Write-Host "Creating the Event Grid Message-Reveived Topic" -ForegroundColor Green
#az eventgrid topic create --resource-group $resourceGroup --name $customTopicName --location $location

# Retrieve endpoint and key to use when publishing to the topic
#endpoint=$(az eventgrid topic show --name $customTopicName -g $resourceGroup --query "endpoint" --output tsv)
#key=$(az eventgrid topic key list --name $customTopicName -g $resourceGroup --query "key1" --output tsv)

#echo $endpoint
#echo $key

# Create return object
$result = [PSCustomObject]@{
  eventGridName = $eventGridName
}

if ($showCommands.ToLower() -eq "true") {
    Write-Host "Object returned by deploy_eventgrid.ps1 script:" -ForegroundColor Green
    ConvertTo-Json $result -Depth 10 | Write-Host -ForegroundColor Green
}

return $result | ConvertTo-Json
