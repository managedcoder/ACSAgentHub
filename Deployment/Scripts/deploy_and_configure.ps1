<# 
Run this script from the ACSAgentHub project folder (i.e., the one that has ACSAgentHub.csproj)
#>

#Requires PoserShell 7

# Usage:
# The following command deploys and configures an agent hub named TestHubRHW21 to run locally
# .\Deployment\Scripts\deploy_and_configure.ps1 -hubName TestHubRHW21 -resourceGroup JustTesting21 -location eastus -NuGetFullPath c:\nuget\nuget.exe
# 
# The following command just configures the agent hub named TestHubRHW21 to run locally but does not deploy any services. Use this command
# re-setup you local environment when you are resuming work on your project.  It restarts all the necessary services and to configure the 
# Event Grid Message subscription with the ngrok endpoint to the Function App that was started by this script and is running locally
# .\Deployment\Scripts\deploy_and_configure.ps1 -hubName TestHubRHW21 -resourceGroup JustTesting21 -configurationOnly true

Param(
    [string] $hubName,
    [string] $resourceGroup,
    [string] $configurationOnly = "false", # Just configure things to run locally, but don't deploy (remaining params are not needed with -restart command)
    [string] $location,
    [string] $NuGetFullPath,
    [string] $connectorPackageVersion = "1.0.0",
    [string] $showCommands = "false",
    [string] $projDir = $(Get-Location),
    [string] $logFile = $(Join-Path $PSScriptRoot .. "deploy_log.txt")
)

# Get timestamp
$startTime = Get-Date

# Reset log file
if (Test-Path $logFile) {
    Clear-Content $logFile -Force | Out-Null
}
else {
    New-Item -Path $logFile | Out-Null
}

# Get mandatory parameters
if (-not $hubName) {
    $hubName = Read-Host "? Bot Name (used as default name for resource group and deployed resources)"
}

if (-not $resourceGroup) {
    $resourceGroup = $hubName
}

if (-not $location -and $configurationOnly.ToLower() -eq "false") {
    $location = Read-Host "? Azure resource group region"
}

# Get timestamp
$timestamp = Get-Date -Format MMddyyyyHHmmss
$startTime = Get-Date

$solutionRoot = Get-Location
$currentDir = $solutionRoot
$agentPortalPath = ".\agent-portal"
$ACSAgentHubPath = ".\ACSAgentHub"
$errCnt = 0

# These steps follow the steps outlined in this solutions README in the "Manual Installation and Configuration" section
# Step 1 - When manually deploying the ACSAgentHub, the first step is to clone this repo so
# nothing to do in this step

# Step 2 - Deploy ACS Agent Hub
# No need for Write-Host progress update since deploy_acs_agent_hub.ps1 reports its own status
if ($configurationOnly.ToLower() -eq "false") {
  .\Deployment\Scripts\deploy_acs_agent_hub.ps1 -hubName $hubName -location $location -resourceGroup $resourceGroup -NuGetFullPAth $NuGetFullPath -connectorPackageVersion $connectorPackageVersion -showCommands $showCommands
}

# Step 3 - Start Agent Hub Service
Write-Host "Starting Agent Hub service" -NoNewline -ForegroundColor Green
# First, change working directory to the ACSAgentHub project folder
Set-Location $ACSAgentHubPath
start -FilePath "func" -ArgumentList "start" -WindowStyle Minimized
# Sleep before continuing to ensure Function App time is fully up and running or else Create Agent Account step will fail
if ($configurationOnly.ToLower() -eq "false") {
  Start-Sleep -s 120
} else {
  Start-Sleep -s 15 # We'll try a shorter sleep when restarting services and see show that goes but service needs to be up before Web PubSub initialization
}

if ($?) {Write-Host " - Done." -ForegroundColor Green} else {Write-Host " - Failed" -ForegroundColor Green; $errCnt++} 

Set-Location $solutionRoot

# Step 4 - Create Agent Accounts
if ($configurationOnly.ToLower() -eq "false") {
  Write-Host "Creating agent account for agent 1"  -NoNewline -ForegroundColor Green
  if ($showCommands.ToLower() -eq "true") { Write-Host ''; Write-Host 'curl -X POST http://localhost:7071/api/agents -H \"Content-Type:application/json\" -d ''{ \"id\": \"1\", \"name\": \"Agent 1\", \"status\": 1, \"skills\": [ \"skill 1\", \"skill 2\", \"skill 3\" ] }''' }
  curl -X POST http://localhost:7071/api/agents `
      -H "Content-Type:application/json" `
      -d '{
      \"id\": \"1\",
      \"name\": \"Agent 1\",
      \"status\": 1,
      \"skills\": [ \"skill 1\", \"skill 2\", \"skill 3\" ] 
      }'

  if ($?) {Write-Host " - Done." -ForegroundColor Green} else {Write-Host " - Failed" -ForegroundColor Green; $errCnt++} 

  Write-Host "Creating agent account for agent 2"  -NoNewline -ForegroundColor Green
  if ($showCommands.ToLower() -eq "true") { Write-Host ''; Write-Host 'curl -X POST http://localhost:7071/api/agents -H \"Content-Type:application/json\" -d ''{ \"id\": \"2\", \"name\": \"Agent 2\", \"status\": 1, \"skills\": [ \"skill 1\", \"skill 2\", \"skill 3\" ] }''' }
  curl -X POST http://localhost:7071/api/agents `
      -H "Content-Type:application/json" `
      -d '{
      \"id\": \"2\",
      \"name\": \"Agent 2\",
      \"status\": 1,
      \"skills\": [ \"skill 1\", \"skill 2\", \"skill 3\" ] 
      }'

  if ($?) {Write-Host " - Done." -ForegroundColor Green} else {Write-Host " - Failed" -ForegroundColor Green; $errCnt++} 
}

# Step 5 - Create Tunnel to Agent Hub
Write-Host "Creating Tunnel to Agent Hub"  -NoNewline -ForegroundColor Green
start -FilePath "c:\ngrok\ngrok" -ArgumentList "http 7071 -host-header=localhost:7071" -WindowStyle Minimized 

if ($?) {Write-Host " - Done." -ForegroundColor Green} else {Write-Host " - Failed" -ForegroundColor Green; $errCnt++} 

# Sleep for a short bit while ngrok starts so we can ping it to get https endpoint
Start-Sleep -s 15 2>> "$logFile" | Out-Null

# Step 6 - Subscribe to ACS Message Event
# No need for Write-Host progress update since update_webhook.ps1 reports its own status
# Frist, query ngrok and turn JSON result into object that we can use to get https endpoint
$ngrokResult = curl http://127.0.0.1:4040/api/tunnels 2>> "$logFile"
$ngrok = ConvertFrom-Json $ngrokResult 

# Next, loop through results find https endpoint (I don't want to assume it's always at the same index in results)
for($i = 0; $i -lt $ngrok.tunnels.length; $i++)
{
    if ($ngrok.tunnels[$i].proto -eq 'https') 
    {
        $httpsEndpoint = $ngrok.tunnels[$i].public_url
    } 
}

# Finally, if we were able to grab the https endpoint then use it to configure Event Grid to subscribe to message events 
if ($httpsEndpoint)
{
  if ($showCommands.ToLower() -eq "true") { Write-Host ''; Write-Host ".\Deployment\Scripts\update_webhook.ps1 -hubName $hubName -endpoint $httpsEndpoint/api/agenthub/messagewebhook -resourceGroup $resourceGroup -showCommands $showCommands" }
  .\Deployment\Scripts\update_webhook.ps1 -hubName $hubName -endpoint $httpsEndpoint"/api/agenthub/messagewebhook" -resourceGroup $resourceGroup -showCommands $showCommands
}
else
{
    Write-Host "Subscribing to ACS Message Event - Failed" -ForegroundColor Green; $errCnt++
}

# Switch to agent-portal folder
Set-Location $agentPortalPath

# Step 7 - Install npm Packages
if ($configurationOnly.ToLower() -eq "false") {
  Write-Host "Installing npm Packages... this step takes awhile"  -NoNewline -ForegroundColor Green

  # Next, install npm packages
  npm install 2>> "$logFile" | Out-Null

  Write-Host " - Done." -ForegroundColor Green 
}

# Step 8 - Launch Agent-Portal
# Launch agent-portal so its ready to use
Write-Host "Launching the agent portal in the browser. Note - initial app startup takes awhile" -ForegroundColor Green
if ($configurationOnly.ToLower() -eq "false") {
  Start-Sleep -s 10 # don't sleep when restarting since packages will already have been deployed
}
start -FilePath "npm" -ArgumentList "start" -WindowStyle Minimized

# Fainally, switch to agent-hub project folder
Set-Location $solutionRoot

$endTime = Get-Date
$duration = New-TimeSpan $startTime $endTime
Write-Host "deploy_and_configure.ps1 took to $($duration.minutes) minutes finish"

Write-Host "Deployment and configuration completed with $errCnt errors"