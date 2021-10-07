import './App.css';

import ColorScheme from './components/ColorScheme'
import Header from './components/Header';
import Content from './components/Content';
import Footer from './components/Footer';
import { getConversations, deleteAgentHubConversation } from './services/conversations';
import { getAgents } from './services/agents';
import { Agent, AgentStatus } from './components/Models'

import { useEffect, useState } from 'react';
import AgentsSignIn from './components/AgentSignIn';
import { ApplicationContextType, ApplicationContextProvider } from './components/ApplicationContext';
import { subscribeToRefreshEvents } from './utils/pubsub';
import { appsettings } from './settings/appsettings';

var globalCtx: ApplicationContextType;

function App() {
    const updateAppCtx = (ac: ApplicationContextType): void => {
        globalCtx = ac;
        setAppCtx(ac);
    };
    const [appCtx, setAppCtx] = useState<ApplicationContextType>({ update: updateAppCtx, agent: null, agents: [], currentConversation: null, conversations: [], isSelected: [] });

    useEffect(() => {
        let mounted = true;

        getConversations()
            .then(allConversations => {
                if (mounted) {
                    getAgents()
                        .then(allAgents => {
                            if (mounted) {
                                // ToDo: remove or comment out this next line when done testing so that Agent isn't asked to sign in when app first loads
                                //let agent = { id: '1', name: 'Russ Williams', status: AgentStatus.Available, skills: ['Service', 'Admin']  };

                                updateAppCtx({ ...appCtx, agent: null, agents: allAgents, conversations: allConversations, isSelected: allConversations.map(() => false) });
                            }
                        });
                }
            });

        // Subscribe to Web Socket events that will be published by the ACSAgentHub when conversation or 
        // agent status changes.  This provides real-time UX refreshes to happen as multiple end users
        // escalate and interact with agents or agent status changes.
        subscribeToRefreshEvents(appsettings.webPusSubConnectionString, appsettings.webPubSubHubName,
            async function (messageEvent): Promise<void> {
                //var wsMsg = messageEvent.data;
                
                var wsEvent = JSON.parse(messageEvent.data);
                console.log(`wsMsgJson: ${wsEvent}`);

                console.log(`wsMsgJson.eventType: ${wsEvent.eventType} wsMsgJson.threadId: ${wsEvent.threadId}`);

                console.log(`Received this WebSocket message on ${appsettings.webPubSubHubName}: ${JSON.stringify(wsEvent)} at ${Date.now}`);

                // If this is not the conversation that published the event
                if (wsEvent.threadId !== globalCtx.currentConversation?.threadId) {
                    // Sync with updated conversations
                    // Note: We need to use the global context variable in this Web Socket event handler since it runs on a separate
                    // thread and, when executing, the local appCtx variable is null.  The global and local are kept synched in the
                    // updateAppCtx function
                    globalCtx.conversations = await getConversations();

                    // Filter conversations to include only ones owned by this agent or ones that are unassigned
                    globalCtx.conversations = globalCtx.conversations.filter((conversation) => { return conversation.agentId === globalCtx.agent?.id || !conversation.agentId || conversation.agentId === undefined });

                    // Create a new isSelected array from a filtered list of conversations for this agent and
                    // flag the active conversation, if there is one
                    globalCtx.isSelected = globalCtx.conversations.map((item) => {
                        // If this is the active conversation, then set it to true, other set it to false
                        return globalCtx.currentConversation?.threadId === item.threadId;
                    });

                    // Get updated list of agents and their current availability status
                    globalCtx.agents = await getAgents();

                    // Update the application context to reflect new state
                    globalCtx.update({ ...globalCtx });

                    if (messageEvent.data.indexOf("error") > 0) {
                        console.log(`error: ${messageEvent.data.error}`);
                    }
                }
            });

        return () => { mounted = false };
    }, []);

    return (
        <ApplicationContextProvider value={appCtx}>
            <Header />
            {(appCtx && (!appCtx.agent || appCtx.agent === undefined)) ?
                <AgentsSignIn /> :
                <Content /> }
            <Footer />
        </ApplicationContextProvider>
  );
}

export default App;
