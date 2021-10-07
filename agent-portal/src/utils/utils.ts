import { appsettings } from '../settings/appsettings';
import { ConversationStatus, AgentStatus } from '../components/Models';

export function GetConversationStatusName(conversationStatus: ConversationStatus): string {
    switch (conversationStatus) {
        case ConversationStatus.Accepted:
            return 'Accepted';
        case ConversationStatus.Closed:
            return 'Closed';
        case ConversationStatus.Queued:
            return 'Queued';
        default:
            return 'Unknown';
    }
}

export function getAgentHubBaseAddress(service: string): string {
    var isSlashNeeded = appsettings.agentHubBaseAddress.length > 0 && appsettings.agentHubBaseAddress[appsettings.agentHubBaseAddress.length - 1] !== '/';
    var fullServiceUrl = null;

    if (isSlashNeeded) {
        fullServiceUrl = appsettings.agentHubBaseAddress + "/" + service;
    }
    else {
        fullServiceUrl = appsettings.agentHubBaseAddress + service;
    }

    return fullServiceUrl;
}

export function getAgentStatusName(statusCode: AgentStatus): string {
    var statusName = "Bad Status Code";

    if (statusCode === AgentStatus.Unavailable) {
        statusName = "Unavailable";
    } else if (statusCode === AgentStatus.Available) {
        statusName = "Available";
    } else if (statusCode === AgentStatus.Maxed) {
        statusName = "Maxed";
    } else if (statusCode === AgentStatus.Away) {
        statusName = "Away";
    }

    return statusName;
}

export function truncate(str: string, n: number): string {
    return (str != null && str.length > n) ? str.substr(0, n - 1) + '...' : str;
};