<# 
This is a multi-line comment syntax
#>

#Requires -Version 6

Param(
    [string] $hubName,
    [string] $resourceGroup,
    [string] $location,
    [string] $NuGetFullPath,
    [string] $showCommands = "false",
    [string] $projDir = $(Get-Location),
    [string] $logFile = $(Join-Path $PSScriptRoot .. "deploy_log.txt")
)

# Reset log file
if (Test-Path $logFile) {
    Clear-Content $logFile -Force | Out-Null
}
else {
    New-Item -Path $logFile | Out-Null
}

# Check for AZ CLI and confirm version
if (Get-Command az -ErrorAction SilentlyContinue) {
    $azcliversionoutput = az -v
    [regex]$regex = '(\d{1,3}.\d{1,3}.\d{1,3})'
    [version]$azcliversion = $regex.Match($azcliversionoutput[0]).value
    [version]$minversion = '2.2.0'

    if ($azcliversion -ge $minversion) {
        $azclipassmessage = "AZ CLI passes minimum version. Current version is $azcliversion"
        Write-Debug $azclipassmessage
        $azclipassmessage | Out-File -Append -FilePath $logfile
    }
    else {
        $azcliwarnmessage = "You are using an older version of the AZ CLI, `
    please ensure you are using version $minversion or newer. `
    The most recent version can be found here: http://aka.ms/installazurecliwindows"
        Write-Warning $azcliwarnmessage
        $azcliwarnmessage | Out-File -Append -FilePath $logfile
    }
}
else {
    $azclierrormessage = 'AZ CLI not found. Please install latest version.'
    Write-Error $azclierrormessage
    $azclierrormessage | Out-File -Append -FilePath $logfile
}

if (-not (Test-Path (Join-Path $projDir 'ACSAgentHub.sln')))
{
    Write-Host "! Could not find the 'ACSAgentHub.sln' file in the current directory." -ForegroundColor Red
    Write-Host "+ Please re-run this script from root of the solution directory." -ForegroundColor Magenta
    Break
}

# Get mandatory parameters
if (-not $hubName) {
    $hubName = Read-Host "? Bot Name (used as default name for resource group and deployed resources)"
}

if (-not $resourceGroup) {
    $resourceGroup = $hubName
}

if (-not $location) {
    $location = Read-Host "? Azure resource group region"
}

# Get timestamp
$startTime = Get-Date

# Create resource group
Write-Host "Creating resource group for the ACS Agent Hub" -ForegroundColor Green -NoNewline
if ($showCommands.ToLower() -eq "true") { Write-Host ""; Write-Host "az group create --name ""$resourceGroup"" --location ""$location"" --output ""json"" " -NoNewline }
az group create `
  --name "$resourceGroup" `
  --location "$location" `
  --output "json" `
  2>> "$logFile" | Out-Null
Write-Host " - Done." -ForegroundColor Green

$appServicePlan = Invoke-Expression "& '$(Join-Path $PSScriptRoot 'deploy_app_service.ps1')' -hubName ""$hubName"" -resourceGroup ""$resourceGroup"" -showCommands ""$showCommands"" -Encoding UTF8" | ConvertFrom-Json

$appInsights = Invoke-Expression "& '$(Join-Path $PSScriptRoot 'deploy_app_insights.ps1')' -hubName ""$hubName"" -resourceGroup ""$resourceGroup"" -showCommands ""$showCommands"" -Encoding UTF8" | ConvertFrom-Json

$agentHubStorage = Invoke-Expression "& '$(Join-Path $PSScriptRoot 'deploy_storage.ps1')' -hubName ""$hubName"" -resourceGroup ""$resourceGroup"" -location ""$location"" -showCommands ""$showCommands"" -Encoding UTF8" | ConvertFrom-Json

$functionApp = Invoke-Expression "& '$(Join-Path $PSScriptRoot 'deploy_function_app.ps1')' -hubName ""$hubName"" -resourceGroup ""$resourceGroup"" -storageAccountName ""$($agentHubStorage.storageAccountName)"" -functionAppServicePlanName ""$($appServicePlan.appServicePlanName)"" -appInsightsName ""$($appInsights.appInsightsName)"" -showCommands ""$showCommands"" -Encoding UTF8" | ConvertFrom-Json

$acs = Invoke-Expression "& '$(Join-Path $PSScriptRoot 'deploy_acs.ps1')' -hubName ""$hubName"" -resourceGroup ""$resourceGroup"" -showCommands ""$showCommands"" -Encoding UTF8" | ConvertFrom-Json

$wps = Invoke-Expression "& '$(Join-Path $PSScriptRoot 'deploy_webPubSub.ps1')' -hubName ""$hubName"" -resourceGroup ""$resourceGroup"" -showCommands ""$showCommands"" -Encoding UTF8" | ConvertFrom-Json

$eventGrid = Invoke-Expression "& '$(Join-Path $PSScriptRoot 'deploy_eventgrid.ps1')' -hubName ""$hubName"" -resourceGroup ""$resourceGroup"" -communicationServerName ""$($acs.acsName)"" -showCommands ""$showCommands"" -Encoding UTF8" | ConvertFrom-Json

$agentPortalAppsettings = Invoke-Expression "& '$(Join-Path $PSScriptRoot 'create_agent-portal_appsettings.ps1')' -webPubSubName ""$($wps.wpsName)"" -resourceGroup $resourceGroup -showCommands ""$showCommands"" -Encoding UTF8"

$agentHubAppsettings = Invoke-Expression "& '$(Join-Path $PSScriptRoot 'create_agent_hub_appsettings.ps1')' -resourceGroup $resourceGroup -acsConnectionString ""$($acs.acsConnectionString)"" -wpsConnectionString ""$($wps.wpsConnectionString)"" -storageConnectionString ""$($agentHubStorage.storageConnectionString)"" -showCommands ""$showCommands"" "

# Build ACSConnector NuGet package
Write-Host "Building ACSConnector" -NoNewline -ForegroundColor Green
$acsConnectorProjectFile = Join-Path $PSScriptRoot ..\..\ "ACSConnector\ACSConnector.csproj"
if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host "dotnet build $acsConnectorProjectFile -c Debug"}
dotnet build $acsConnectorProjectFile -c Debug 2>> "$logFile" | Out-Null

Write-Host " - Done." -ForegroundColor Green

# Create local NuGet feed for ACSConnector
Write-Host "Creating local NuGet feed for ACSConnector" -NoNewline -ForegroundColor Green
$acsConnectorNuGetPackage = Join-Path $PSScriptRoot ..\..\ "ACSConnector\bin\Debug\ACSConnector.1.0.0.nupkg"
$acsAgentHubSDKNuGetPackage = Join-Path $PSScriptRoot ..\.. "ACSAgentHubSDK\bin\Debug\ACSAgentHubSDK.1.0.0.nupkg"
$acsConnectorLocalFeedFolder = join-Path $PSScriptRoot ..\..\ "ACSConnector\localFeed"
# First, create folder for local NuGet feed
if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host "mkdir -Force $acsConnectorLocalFeedFolder"}
mkdir -Force $acsConnectorLocalFeedFolder

# Next, add ACSConnector and its dependencies to local feed
if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host "$NuGetFullPath add $acsConnectorNuGetPackage -Source $acsConnectorLocalFeedFolder" }
& $NuGetFullPath add $acsConnectorNuGetPackage -Source $acsConnectorLocalFeedFolder
if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host "$NuGetFullPath add $acsAgentHubSDKNuGetPackage -Source $acsConnectorLocalFeedFolder" }
& $NuGetFullPath add $acsAgentHubSDKNuGetPackage -Source $acsConnectorLocalFeedFolder

Write-Host " - Done." -ForegroundColor Green

$endTime = Get-Date
$duration = New-TimeSpan $startTime $endTime
Write-Host "deploy_acs_agent_hub.ps1 took to  $($duration.minutes) minutes finish"

