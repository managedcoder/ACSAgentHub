<# 
This is a multi-line comment syntax
#>

#Requires -Version 6

Param(
    [string] $NuGetFullPath,
    [string] $connectorPackageVersion = "1.0.0",
    [string] $showCommands = "false",
    [string] $projDir = $(Get-Location),
    [string] $logFile = $(Join-Path $PSScriptRoot .. "deploy_log.txt" -Resolve)
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

if (-not (Test-Path (Join-Path $projDir 'ACSAgentHub.sln' -Resolve)))
{
    Write-Host "! Could not find the 'ACSAgentHub.sln' file in the current directory." -ForegroundColor Red
    Write-Host "+ Please re-run this script from root of the solution directory." -ForegroundColor Magenta
    Break
}

# Get mandatory parameters

if (-not $NuGetFullPath) {
    $NuGetFullPath = Read-Host "? Full path to nuget.exe (e.g., c:\nuget\nuget.exe):"
}

if (-not $connectorPackageVersion) {
    $connectorPackageVersion = Read-Host "? Package version (e.g., 1.0.0):"
}

if (-not $NuGetFullPath) {
    $NuGetFullPath = Read-Host "? Full path to nuget.exe"
}

# Get timestamp
$startTime = Get-Date

# Create local NuGet feed for ACSConnector
Write-Host "Creating local NuGet feed for ACSConnector" -NoNewline -ForegroundColor Green
$acsConnectorNuGetPackage = Join-Path $PSScriptRoot ..\..\ "ACSConnector\bin\Debug\ACSConnector.$connectorPackageVersion.nupkg" -Resolve
$acsAgentHubSDKNuGetPackage = Join-Path $PSScriptRoot ..\.. "ACSAgentHubSDK\bin\Debug\ACSAgentHubSDK.$connectorPackageVersion.nupkg" -Resolve
$acsConnectorLocalFeedFolder = join-Path $PSScriptRoot ..\..\ "ACSConnector" -Resolve
# First, create folder for local NuGet feed
if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host "mkdir -Force $acsConnectorLocalFeedFolder\localfeed"}
mkdir $acsConnectorLocalFeedFolder\localfeed

if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host "$NuGetFullPath delete ACSAgentHubSDK $connectorPackageVersion -Source $acsConnectorLocalFeedFolder\localfeed -NonInteractive " }
& $NuGetFullPath delete ACSAgentHubSDK $connectorPackageVersion -Source $acsConnectorLocalFeedFolder\localfeed -NonInteractive
if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host "$NuGetFullPath delete ACSConnector $connectorPackageVersion -Source $acsConnectorLocalFeedFolder\localfeed -NonInteractive " }
& $NuGetFullPath delete ACSConnector $connectorPackageVersion -Source $acsConnectorLocalFeedFolder\localfeed -NonInteractive 

# Next, clear all NuGet caches in case we are overwriting existing versions of existing NuGet packages (this can cause runtime startup issues in Composer)
#if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host "$NuGetFullPath locals all -clear" }
#& $NuGetFullPath locals all -clear

# Next, add ACSConnector and its dependencies to local NuGet feed
if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host "$NuGetFullPath add $acsAgentHubSDKNuGetPackage -Source $acsConnectorLocalFeedFolder\localfeed" }
& $NuGetFullPath add $acsAgentHubSDKNuGetPackage -Source $acsConnectorLocalFeedFolder\localfeed 
if ($showCommands.ToLower() -eq "true") {Write-Host ''; Write-Host "$NuGetFullPath add $acsConnectorNuGetPackage -Source $acsConnectorLocalFeedFolder\localfeed" }
& $NuGetFullPath add $acsConnectorNuGetPackage -Source $acsConnectorLocalFeedFolder\localfeed
