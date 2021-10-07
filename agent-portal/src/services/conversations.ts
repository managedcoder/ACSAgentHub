import { getAgentHubBaseAddress } from '../utils/utils';
import { Conversation, ConversationStatus } from '../components/Models'
import { Console } from 'console';

export async function getConversations(): Promise<Conversation[]> {
    return await fetch(getAgentHubBaseAddress("api/Conversations"))
        .then(data => data.json())
}

export async function updateConversationStatus(conversation: Conversation): Promise<void> {
    await fetch(getAgentHubBaseAddress("api/conversations"), {
        method: 'put',
        body: JSON.stringify(conversation),
        headers: { 'Content-type': 'application/json' }
    });
}

export async function deleteConversation(threadId: string): Promise<void> {
    await fetch(getAgentHubBaseAddress(`api/conversations/${threadId}`), { method: 'DELETE' })
}

export function deleteAgentHubConversation(conversations: Conversation[], threadId: string): Conversation[] {
    return conversations.filter(conversation => {
        return conversation.threadId !== threadId;
    });
}

export async function sendConversationEvent(threadId: string, status: ConversationStatus): Promise<void> {
    console.log(`EventToBot threadId: ${threadId}, status: ${status}`)
    await fetch(getAgentHubBaseAddress(`api/agenthub/${threadId}/eventToBot`), {
        method: 'post',
        body: JSON.stringify(status),
        headers: { 'Content-type': 'application/json' }
    });
}
