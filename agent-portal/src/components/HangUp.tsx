import './HangUp.css';
import React, { FC } from 'react';
import HangUpIcon from '../images/HangUpIcon.png';

import { Conversation, ConversationStatus, ConversationActionSize } from './Models';
import { deleteAgentHubConversation, deleteConversation, sendConversationEvent } from '../services/conversations';
import { ApplicationContextConsumer } from './ApplicationContext'

interface Props {
    Conversation: Conversation
}

const HangUp: FC<Props> = (props: Props) => {
    return (
        <ApplicationContextConsumer>
            {appContext => appContext && (
                <div>
                    { console.log('Rendering Hang Up icon')}
                    <img src={HangUpIcon} alt="hang up icon" height={ConversationActionSize} onClick={async (e): Promise<void> => {
                        await sendConversationEvent(props.Conversation.threadId, ConversationStatus.Closed);
                        // Delete conversation in ACS
                        await deleteConversation(props.Conversation.threadId);
                        // Unselect isSelected
                        appContext.isSelected = appContext.isSelected.map((status, i) => (false));
                        // Update app context with a cleared current conversation and a new converaation list minus the conversation being deleted
                        appContext.update({ ...appContext, currentConversation: null, conversations: deleteAgentHubConversation(appContext.conversations, props.Conversation.threadId) });
                    }} />
                </div>
            )}
        </ApplicationContextConsumer>
    );
};

export default HangUp;
