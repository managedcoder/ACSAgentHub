# ACS Agent Hub
This package integrates with a ready-made agent hub solution built on the Azure Communication Service that allows you to
add human handoff experiences to your bot. Super simple to use, just add a Send Handoff Activity to your dialog and the
ACSConnector takes care of the rest. Escalations will be sent to a ready-made agent hub that you clone from GitHub which
allows multiple agents to handle multiple simultaneous conversations with bot users. Because it�s built purely from Azure
Services, you can have an agent escalation capability up and working in 10 minutes or so.  Take a look at this short [ACS
Agent Hub overview video](https://aka.ms/acshub-overview) and if that sounds interesting then check out this
[getting started video](https://aka.ms/acshub-start) to see how easy it is to get this solution up and rnning.  You can
see a deeper dive [multi-agent demonstation video](https://aka.ms/acshub-v1demo) to get a sense of full capability of this solution.

### <a name="Prerequisites"></a>Prerequisites

#### Powershell 7
The deployment scripts require Powershell 7 which can be downloaded [here](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows?view=powershell-7.2)

#### Ngrok
The deployment scripts use ngrok to create public endpoint for the ACSAgentHub running locally on your development PC so
you'll need to install ngrok v3 following the instructions [here](https://ngrok.com/download) or to upgrade your existing 
ngrok version, follow the instructions [here](https://ngrok.com/docs/guides/upgrade-v2-v3).  The previous version of the 
deployment scripts used single dashes for ngrok command line flags which now fail in ngrok v3 per the "Breaking Changes" 
section on the ngrok site [here](https://ngrok.com/docs/guides/upgrade-v2-v3) so those deployment scripts have been 
updated to use double dash flags which may or may not work with older versions of
ngrok (e.g., c:\ngrok\ngrok http 7071 --host-header=localhost:7071)

#### Nuget
The deployment scripts use the nuget.exe to create a nuget package so grab the latest nuget commandline executable from
[here](https://www.nuget.org/downloads).

#### Azure CLI
The deployment scripts require Azure CLI version 2.22.0 or newer.  You can check which version of the Azure CLI you have
installed by running this command from command prompt:
```
az version
```
If you don't already have the Azure CLI installed, you can install it from [here](
https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?tabs=azure-cli).

#### Azure Communication Service CLI Extension

To install the Azure Communication Service extension, run the following from a command prompt:
```
az communication create
```
#### Web PubSub CLI Extension
To install the Web PubSub extension, run the following from a command prompt
```
az webpubsub
```
Note - The output of this command may be messy with a lot of diagnostics before it prompts to
install the extension so be aware of that.

#### Azure Functions Core Tools
The core agent hub logic lives in a set APIs in the ACSAgentHub Azure Function project and to quickly
test things out you can run it locally from the command line but you'll need to have the Azure 
Functions Core Tools v3.X installed.  Alternatively, you can avoid installing the Azure Functions Core
Tools if you use Visual Studio to run the ACSAgentHub Azure Function project.

To install Azure Function Core Tools v3.X you'll need to download and run the Core Tools installer, based on your version of Windows:

* [v3.x - Windows 64-bit](https://go.microsoft.com/fwlink/?linkid=2135274)
* [v3.x - Windows 32-bit](https://go.microsoft.com/fwlink/?linkid=2135275)

#### Install Bot Framework Composer
The ACS Agent Hub works with any kind of bot built on the Microsoft Conversational AI platform (PVA, Composer, 
Virtual Assistant Template, or SDK bot) but the quickest and easyiest way to get up and running is to use the
Composer sample included in this solution and to do that you'll need to install the Bot Framework Composer
[here](https://docs.microsoft.com/en-us/composer/install-composer?tabs=windows)

### <a name="GettingStarted"></a>Getting Started

Getting started couldn't be easier.  Simply clone the repo and run a deployment script and you're ready
to run the sample app and try out agent escalation.  You can take a look at this 
[getting started video](https://aka.ms/acshub-start) to see step-by-step instructions for getting this
solution up and running.  Please note that the deployment script in that video requires that you've taking
care of the [prerequisites](#Prerequisites) or it will fail.

#### 1) **Clone the ACS Agent Hub Repo**  
>Open PowerShell and change to the directory where you want the project folder to be created and
run the following command:  
&nbsp;  
```
git clone https://github.com/managedcoder/ACSAgentHub
```
#### 2) **Deploy ACS Agent Hub**  
>In PowerShell, change the current directory to the root solution folder (i.e., the one
that contains the ACSAgentHub.sln file). Then run the following script to deploy and
configure all the services required by the ACS Agent Hub:  
&nbsp;  
```
.\Deployment\Scripts\deploy_and_configure.ps1 -hubName <hub name> -resourceGroup <resource group> -ngrokFullPath <ngrok path> -location <location name> -NuGetFullPath <nuget path>
```
where:
- **\<hub name>** is the name of your agent hub which will also be used as part of the name of
other services created by this script
- **\<resource group>** is the name of the resource group that services will be deployed to
- **\<ngrok path>** is the full path to the ngrok.exe (e.g., c:\nuget\ngrok.exe)
- **\<location name>** is the Azure location name of the region that services will be deployed in (e.g., eastus)
- **\<nuget path>** is the full path to the nuget.exe (e.g., c:\nuget\nuget.exe)

#### 3) **<a name="ExploreEscalationScenarios"></a>Explore Escalation Scenarios**  
>To make it easy for you to quickly experiment with the ACS Agent Hub we've included sample applications that are all
ready to go.  All you need to do to try it out is to open the Bot Framework Composer and select the **Open** link on
the Home tab and open the ComposerExample that's in the root folder of the solution. This is a very simple bot with 
an Escalate trigger that you can look at to see how to code an escalation and then try it out by using the 
**Start bot** command.  After the bot starts, choose the **Open Web Chat** option in the flyout menu that appears
when the bot finishes it's startup processing.  Type, _**talk to human**_ to start an escalation experience.  There's
a short [demo video](http://aka.ms/acshub-start?t=119) you can watch that shows all these steps and how to demo the 
agent escalation scenario.  You can also take a look at a [fuller multi-agent demo](https://aka.ms/acshub-v1demo) if 
your interested.

### <a name="RestartingRuntime"></a>Restarting the ACSAgentHub runtime environment
In the future, to restart the ACSAgentHub runtime after it's been shutdown, you'll need to run this command in
PowerShell in the root solution folder (i.e., the one that contains the ACSAgentHub.sln file).  When it finishes,
the runtime environment will be ready to use just like it was after it was initially deployed.  
&nbsp;  
```
.\Deployment\Scripts\deploy_and_configure.ps1 -hubName <hub name> -resourceGroup <hub resource group> -ngrokFullPath <ngrok path> -configurationOnly true
```
where:
- **\<hub name>** is the name of the agent hub you provided when you initially deployed the ACSAgentHub
- **\<resource group>** is the name of the resource group you provided when you initially deployed the ACSAgentHub
- **\<ngrok path>** is the full path to the ngrok.exe (e.g., c:\nuget\ngrok.exe)

### <a name="AddingEscalationToYourBot"></a>Adding Escalation to Your Bot
The samples are nice for quickly experimenting with escalation and taking a look at how it's coded, but they don't 
address how to add escalation to bots.  The good news is that adding agent escalation to a Composer bot is very 
simple and only involves a few steps.  These following steps are shown in a very short video [here](http://adding-escalation-=video)
that you might find easier to follow.
#### 1) **Install Agent Hub Package**  
>First, create a local feed and set its URL property to the full path of the
.\ACSConnector\localFeed folder so Composer will know where find the package and second, add the ACSConnector package
to your bot as shown in the following screen shots, respectively<br>
![High-level Subsystem Overview](doc/CreatingLocalFeed.png)<br>
![High-level Subsystem Overview](doc/InstallingACSConnector.png)
#### 2) **Set App Setting**  
>Paste the following at the top of the bot's appsettings.json file:<br>
```
  "ACSConnector": {
    "acsAgentHubBaseAddress": "http://localhost:7071"
  },
```
![High-level Subsystem Overview](doc/AppSettings.png)

#### 3) **Add an Escalation Action**  
>Add a _**Send handoff activity**_ action to any dialog and at wherever point makes
sense for your bot<br> 
![High-level Subsystem Overview](doc/EscalateAction.png)
#### 4) **Set Escalation Action Properties**  
>Paste the following escalation payload into the **Context** property of escalation action and set **Transcription**
to ```=null```<br>
```
{
  "Skill": "offshore accounts",
  "Name": "Test Name",
  "CustomerType": "vip",
  "WhyTheyNeedHelp": "lost order"
}
```
The properties and values above are defined by the agent hub you're escalating to.  So, for example, the payload
is unique to LivePerson or Omnichannel and you'd check their documentation to find out what the payload should
look like for those agent hub solutions but, for the ACS Agent Hub you'll use the above payload and replace the
hardcoded sample values with the appropriate values (use the values above if you are just experimenting).  

Currently, 
the **Skill** and **CustomerType** properties are not being used by the ACS Agent Hub and can be set to whatever
you like or left empty.  The **Name** and **WhyTheyNeedHelp** properties are used in the agent-portal and the 
ComposerExample shows a nice way to gather and set those properties.  When you're finished, it should look like
this:<br>
![High-level Subsystem Overview](doc/EscalationProperties.png)

### <a name="AddingAgentEscalationToNewOrExistingBot"></a>Appendix A: Agent Escalation in a VA Template Bot
If you want to add agent escalation to a Virtual Assistant Template bot, you can explore
how that's done in the VATemplateExample sample project.  If you want to quickly test this
out, you can open PowerShell and change the current directory to the VATemplateExample 
project folder (i.e., the one that contains the VATemplateExample.csproj file).  The Run
the deploy.ps1 script using the following relative path:
```
.\Deployment\Scripts\deploy.ps1 -name <name of bot> -location <region> -appPassword <password> -createLuisAuthoring -luisAuthoringRegion westus
```
This script deploys and configures all the services required by the virtual assistant and you can use the
Bot Framework Emulator to explore the same esclation scenarios as shown with the Composer bot.