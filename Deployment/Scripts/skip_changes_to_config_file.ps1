<#
This script tells git to ignore changes to config files but not to ignore the file itself.  This is great
for config files that need to be part of source control and act as a template that defines the settings 
of the application with empty values or appropriate defaults. 
#>

Param(
    [switch] $undo # Turns tracking back on so you can edit config files and the check them back in and then turn tracking back off
)

$botAppSettings = $(Join-Path $PSScriptRoot ..\.. "VATemplateExample\VATemplateExample\appsettings.json" -Resolve)
$cogmodels = $(Join-Path $PSScriptRoot ..\.. "VATemplateExample\VATemplateExample\cognitivemodels.json" -Resolve)

$composerBotAppSettings = $(Join-Path $PSScriptRoot ..\.. "ComposerExample\settings\appsettings.json" -Resolve)

$agentHubSettings = $(Join-Path $PSScriptRoot ..\..\ "ACSAgentHub\local.settings.json" -Resolve)
$agentPortalSettings = $(Join-Path $PSScriptRoot ..\..\ "agent-portal\src\settings\appsettings.ts" -Resolve)

if ($undo) {
	$x = git update-index --no-skip-worktree $botAppSettings
	$y = git update-index --no-skip-worktree $cogmodels
	$z = git update-index --no-skip-worktree $agentHubSettings
	$q = git update-index --no-skip-worktree $agentPortalSettings
	$a = git update-index --no-skip-worktree $composerBotAppSettings

	Write-Host "Git has resumed tracking changes to the following configuration files:" -ForegroundColor Green
}
else {
	git update-index --skip-worktree $botAppSettings
	git update-index --skip-worktree $cogmodels
	git update-index --skip-worktree $agentHubSettings
	git update-index --skip-worktree $agentPortalSettings
	git update-index --skip-worktree $composerBotAppSettings

	Write-Host "Git is ignoring changes to the following configuration files:" -ForegroundColor Green
}

write-Host $botAppSettings
Write-Host $cogmodels
Write-Host $agentHubSettings
Write-Host $agentPortalSettings
Write-Host $composerBotAppSettings
