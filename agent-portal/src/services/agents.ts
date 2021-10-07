import { getAgentHubBaseAddress } from '../utils/utils';
import { Agent, AgentStatus} from '../components/Models'

export async function getAgents(): Promise<Agent[]> {
    return await fetch(getAgentHubBaseAddress("api/agents"))
        .then(data => data.json())
}

export async function updateAgentStatus(agent: Agent, status: AgentStatus): Promise<void> {
    agent.status = status;
    await fetch(getAgentHubBaseAddress("api/agents"), {
        method: 'put',
        body: JSON.stringify(agent),
        headers: { 'Content-type': 'application/json' }
    });
}

export async function addAgentToThread(threadId: string, displayName: string): Promise<void> {
    await fetch(getAgentHubBaseAddress(`api/AddAgentToThread/${threadId}/${displayName}`), { method: 'POST' })
}
