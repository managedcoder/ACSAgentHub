import './ConversationsPanel.css';

import React, { FC } from 'react';

import { getConversations } from '../services/conversations';
import ConversationAction from './ConversationAction';
import { truncate } from '../utils/utils';
import { Conversation } from '../components/Models'

import RefreshIcon from '../images/RefreshIcon.png';
import {ApplicationContextConsumer, ApplicationContextType} from './ApplicationContext'

const ConversationsPanel: FC = () => {

    async function refresh(ac: ApplicationContextType): Promise<void> {
        // Get conversations from ACS
        ac.conversations = await getConversations();
        // Create new isSelected array to match the amount of conversation fetched from ACS and initialize them to false
        ac.isSelected = ac.conversations.map(() => false);
        // Update the application context to reflect new state
        ac.update({ ...ac, currentConversation: null });
    }

    function filteredConversations(appContext: ApplicationContextType): Conversation[] {
        console.log(`appContext.conversations.length: ${appContext.conversations.length}`);

        let x = appContext.conversations.filter(function (conversation) {
            console.log(`conversation.agent: ${conversation.agentId}, appContext!.agent!.id: ${appContext!.agent!.id}`);
            return conversation.agentId == null || conversation.agentId == appContext!.agent!.id;
            //return true;
        });

        if (appContext.conversations.length > 1)
            console.log(`x.length: ${x.length}, first ${appContext.conversations[0].agentId}, second: ${appContext.conversations[1].agentId}`);

        return x;
    }


    return (
        <ApplicationContextConsumer>
            {appContext => appContext && (
                <div className="conversationPanel">
                    <div className="conversationCmdBar">
                        <img src={RefreshIcon} alt="refresh icon" height="20" title="Refresh" onClick={(e) => { refresh(appContext); }} />
                    </div>
                    <div className="conversationList">
                        <table className="tableStyle">
                            {filteredConversations(appContext).map((item, i) => {
                            //if (item.agent == null || item.agent === appContext!.agent!.id)
                                return <tr className="conversationRow">
                                    <td className="minimizePerimeter">
                                        <input onClick={() => {
                                            // toggles any checked selection to uncheck and checks the new selection
                                            appContext.isSelected = appContext.isSelected.map((status, j) => (i === j));
                                            // Update the app context to reflect new state
                                            appContext.update({ ...appContext, currentConversation: item });
                                        }} type="checkbox" checked={appContext.isSelected[i]} />
                                    </td>
                                    <td className="minimizePerimeter">
                                        <tr >
                                            <span className="userName">{getName(item.escalationContext.handoffContext.name)}</span>
                                        </tr>
                                        <tr >
                                            <span className="context">{truncate(item.escalationContext.handoffContext.whyTheyNeedHelp, 35)}</span>
                                        </tr>
                                    </td>
                                    <td className="minimizePerimeter">
                                        {<ConversationAction Conversation={item} />}
                                    </td>
                                </tr>
                            })}
                        </table>

                    </div>
                </div>
            )}
        </ApplicationContextConsumer>
    );
};

function getName(name: string): string {
    return (name === null || name === undefined) ? 'Anonymous' : name;
}

export default ConversationsPanel;
