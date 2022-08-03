import './ChatPanel.css';

import React, { FC } from 'react';
import { useEffect, useState, useContext } from 'react';
import { ChatAdapter, ChatComposite, createAzureCommunicationChatAdapter } from '@azure/communication-react';
import { AzureCommunicationTokenCredential } from '@azure/communication-common';

import { Conversation, ConversationStatus, Agent } from './Models';
import { addAgentToThread } from '../services/agents';

import { getAgentAccessContext } from '../services/accessContext';
import { ApplicationContext, ApplicationContextConsumer } from './ApplicationContext';

const ChatPanel: FC = () => {
    // Creating an adapter is asynchronous.
    // An update to `config` triggers a new adapter creation, via the useEffect block.
    // When the adapter becomes ready, the state update triggers a re-render of the ChatComposite.
    const [adapter, setAdapter] = useState<ChatAdapter>();
    let applicationContext = useContext(ApplicationContext);

    function showChat(conversation: Conversation): boolean {
        if (conversation != null) {
            return conversation!.status! === ConversationStatus.Accepted || conversation!.status! === ConversationStatus.Closed;
        }

        return false;
    }

    useEffect(() => {
        // ToDo: Look into this...If I don't reset the adaptor to undefined, everything works but
        // I don't see the chat history in the control.Setting it to undefined cause history to render
        setAdapter(undefined);
        if (showChat(applicationContext!.currentConversation!) == true) {
            addAgentToThread(applicationContext!.currentConversation!.threadId!, applicationContext!.agent!.name).then(() => {
                getAgentAccessContext().then(agentAccessContext => {
                    createAzureCommunicationChatAdapter({
                        endpoint: agentAccessContext.endPoint,
                        displayName: applicationContext!.agent!.name,
                        threadId: applicationContext!.currentConversation!.threadId,
                        userId: { communicationUserId: applicationContext!.currentConversation!.agentId },
                        credential: new AzureCommunicationTokenCredential(agentAccessContext.token)
                    }).then(adapter => { 
                        setAdapter(adapter);
                    });
                });
            });
        }
    }, [applicationContext]);

    return (
        <ApplicationContextConsumer>
            {appContext => appContext && (
                <div className="chatPanel">
                    { (showChat(appContext!.currentConversation!)) ? adapter ? <ChatComposite adapter={adapter} /> : <div className="loadingPrompt">Loading...</div> : <div></div>}
                </div>
            )}
        </ApplicationContextConsumer>
    );
};

export default ChatPanel;
