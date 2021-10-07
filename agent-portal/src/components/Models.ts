export interface BladeSelectorCallback { (threadId: string): void }
export interface AgentSignInCallback { (agent: Agent): void }

export interface Agent {
    id: string,
    name: string,
    status: number,
    skills: string[]
}

export interface Conversation {
    threadId: string,
    escalationContext: EscalationContext,
    status: ConversationStatus,
    agentId: string,
    disposition: Disposition,
    cStat: CStat
}

export interface EscalationContext {
    conversationReference: string,
    handoffContext: HandoffContext
}

export interface HandoffContext {
    skill: string,
    name: string,
    customerType: string,
    whyTheyNeedHelp: string
}

export enum AgentStatus {
    Unavailable,
    Available,
    Maxed,
    Away
}

export enum Disposition {
    Pending,
    AgentClosed,
    UserClosed
}

export enum CStat {
    Unknown,
    ThumbsUp,
    ThumbsDown
}

export enum ConversationStatus {
    Queued,
    Accepted,
    Closed
}

export const ConversationActionSize = 25;

export interface ACSAccessContext {
    userId: string,
    token: string,
    endPoint: string
}
