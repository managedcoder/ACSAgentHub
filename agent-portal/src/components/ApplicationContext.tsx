import React from "react";
import { Agent, Conversation } from './Models'

export interface AppContextUpdateCallback { (ac: ApplicationContextType): void };
export interface ApplicationContextType {
    update: AppContextUpdateCallback;
    agent: Agent | null;
    agents: Agent[];
    currentConversation: Conversation | null;
    conversations: Conversation[];
    isSelected: boolean[];
}

export const ApplicationContext = React.createContext<ApplicationContextType | null>(null);
export const ApplicationContextProvider = ApplicationContext.Provider;
export const ApplicationContextConsumer = ApplicationContext.Consumer;
