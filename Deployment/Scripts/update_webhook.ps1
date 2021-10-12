<#
This scripts creates or updates an eventgrid event subscription for ACS messages depending on whether the 
event subscription already exists or not
#>
Param(
    [string] $hubName,
    [string] $endpoint,
    [string] $resourceGroup,
    [string] $showCommands = "false",
    [string] $logfile = $(Join-Path $PSScriptRoot .. "deploy_log.txt")
 )

# Reset log file
if (Test-Path $logFile) {
    Clear-Content $logFile -Force | Out-Null
}
else {
    New-Item -Path $logFile | Out-Null
}

if (!$resourceGroup) {
    $resourceGroup = $($hubName)
}

$eventSubscriptionName = $hubName + "EGS"
$acsServiceName = $hubName + "ACS"
$azureSubscriptionId = az account show --query id --output tsv

# Maybe best if we don't show this next command since we just use it to decide if we should run the update or create webhook call
#if ($showCommands.ToLower() -eq "true") { Write-Host "Checking for an existing eventgrid event-subscription" -ForegroundColor Green; write-host "az eventgrid event-subscription show --name ""$($eventSubscriptionName)"" --source-resource-id ""/subscriptions/$($azureSubscriptionId)/resourceGroups/$($resourceGroup)/providers/Microsoft.Communication/CommunicationServices/$($acsServiceName)"" --subscription ""$($azureSubscriptionId)"" " }
$result = az eventgrid event-subscription show `
  --name "$($eventSubscriptionName)" `
  --source-resource-id "/subscriptions/$($azureSubscriptionId)/resourceGroups/$($resourceGroup)/providers/Microsoft.Communication/CommunicationServices/$($acsServiceName)" `
  --subscription "$($azureSubscriptionId)" `
  2>> "$logFile"
#if ($showCommands.ToLower() -eq "true") { Write-Host " - Done." -ForegroundColor Green }

if ($result)
{
  write-host "Updating Agent Hub webhook for ACS message subscription named $($hubName)" -NoNewline -ForegroundColor Green
  if ($showCommands.ToLower() -eq "true") { Write-Host ""; write-host "az eventgrid event-subscription update --name ""$eventSubscriptionName"" --endpoint ""$endpoint"" --endpoint-type ""webhook"" --event-delivery-schema ""eventgridschema"" --included-event-types ""Microsoft.Communication.ChatMessageReceivedInThread"" --source-resource-id ""/subscriptions/$($azureSubscriptionId)/resourceGroups/$($resourceGroup)/providers/Microsoft.Communication/CommunicationServices/$($acsServiceName)"" --subscription ""$($azureSubscriptionId)"" "}
  az eventgrid event-subscription update `
    --name "$eventSubscriptionName" `
    --endpoint "$endpoint" `
    --endpoint-type "webhook" `
    --event-delivery-schema "eventgridschema" `
    --included-event-types "Microsoft.Communication.ChatMessageReceivedInThread" `
    --source-resource-id "/subscriptions/$($azureSubscriptionId)/resourceGroups/$($resourceGroup)/providers/Microsoft.Communication/CommunicationServices/$($acsServiceName)" `
    --subscription "$($azureSubscriptionId)" `
    2>> "$logFile" | Out-Null
  Write-Host " - Done." -ForegroundColor Green
 }
else
{
  write-host "Adding Agent Hub webhook for ACS message subscription named $($resourceGroup)" -NoNewline -ForegroundColor Green

  if ($showCommands.ToLower() -eq "true") { Write-Host ""; write-host "az eventgrid event-subscription create --name ""$($eventSubscriptionName)"" --endpoint ""$endpoint"" --endpoint-type ""webhook"" --event-delivery-schema ""eventgridschema"" --included-event-types ""Microsoft.Communication.ChatMessageReceivedInThread"" --source-resource-id ""/subscriptions/$($azureSubscriptionId)/resourceGroups/$($resourceGroup)/providers/Microsoft.Communication/CommunicationServices/$($acsServiceName)"" --subscription ""$($azureSubscriptionId)"" " }
  az eventgrid event-subscription create `
    --name "$eventSubscriptionName" `
    --endpoint "$endpoint" `
    --endpoint-type "webhook" `
    --event-delivery-schema "eventgridschema" `
    --included-event-types "Microsoft.Communication.ChatMessageReceivedInThread" `
    --source-resource-id "/subscriptions/$($azureSubscriptionId)/resourceGroups/$($resourceGroup)/providers/Microsoft.Communication/CommunicationServices/$($acsServiceName)" `
    --subscription "$($azureSubscriptionId)" `
    2>> "$logFile" | Out-Null
  Write-Host " - Done." -ForegroundColor Green
}


