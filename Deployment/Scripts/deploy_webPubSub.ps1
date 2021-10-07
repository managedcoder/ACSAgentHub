#Requires -Version 6

Param(
    [string] $hubName,
    [string] $resourceGroup,
    [string] $showCommands = "false",
    [string] $logfile = $(Join-Path $PSScriptRoot .. "deploy_log.txt")
 )

# Create Web PubSub
$webPubSubName = $hubName + "WPS"
Write-Host "Creating the Web PubSub that enables real-time update of ACS Agent Hub Portal" -NoNewline -ForegroundColor Green
if ($showCommands.ToLower() -eq "true") { 
  # Uncomment the following line and get rid of the line after that once "preview warning" goes away so it respect the -NoNewline switch
  #if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az webpubsub create -n ""$webPubSubName"" -g ""$resourceGroup"" --sku ""Standard_S1"" --unit-count ""2"" " -NoNewline }
  Write-Host ""; 
  Write-Host "az webpubsub create -n ""$webPubSubName"" -g ""$resourceGroup"" --sku ""Standard_S1"" --unit-count ""2"" " 
}

az webpubsub create `
  -n "$webPubSubName" `
  -g "$resourceGroup" `
  --sku "Standard_S1" `
  --unit-count "2" `
  2>> "$logFile" | Out-Null

Write-Host " - Done." -ForegroundColor Green

# Grab connection string 
if ($showCommands.ToLower() -eq "true") { 
  Write-Host "Grab connection string for the Agent Hub's Web PubSub service" -NoNewline -ForegroundColor Green
  #Write-Host ""; Write-Host "`$webPubSub = az webpubsub key show --name ""$webPubSubName"" -g ""$resourceGroup"" " -NoNewline }
  # Uncomment the following line and get rid of the line after that once "preview warning" goes away so it respect the -NoNewline switch
  Write-Host ""; 
  Write-Host "az webpubsub key show --name ""$webPubSubName"" -g ""$resourceGroup"" " 
}
$webPubSub = az webpubsub key show --name "$webPubSubName" --resource-group "$resourceGroup" | ConvertFrom-Json -Depth 10
Write-Host " - Done." -ForegroundColor Green

# Create return object
$result = [PSCustomObject]@{
    wpsName = $webPubSubName
    wpsConnectionString = $webPubSub.primaryConnectionString
}

if ($showCommands.ToLower() -eq "true") {
    Write-Host "Object returned by deploy_webPubSub.ps1 script:" -ForegroundColor Green
    ConvertTo-Json $result -Depth 10 | Write-Host -ForegroundColor Green
}

return $result | ConvertTo-Json
