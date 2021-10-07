import { getAgentHubBaseAddress } from '../utils/utils';
import { ACSAccessContext } from '../components/Models'

export async function getAgentAccessContext(): Promise<ACSAccessContext> {
    return await fetch(getAgentHubBaseAddress("api/agentAccessContext"))
        .then(data => data.json())
}