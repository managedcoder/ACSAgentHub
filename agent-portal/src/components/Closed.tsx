import './Closed.css';
import React, { FC } from 'react';
import ClosedIcon from '../images/ClosedIcon.png';
import { Conversation, ConversationActionSize } from './Models';
import { deleteConversation, deleteAgentHubConversation } from '../services/conversations';
import { ApplicationContextConsumer } from './ApplicationContext'

interface Props {
    Conversation: Conversation,
}

const Closed: FC<Props> = (props: Props) => {
    return (
        <ApplicationContextConsumer>
            {appContext => appContext && (
                <div>
                    <img src={ClosedIcon} alt="closed conversation icon" height={ConversationActionSize} title="Close" onClick={async (e): Promise<void> => {
                        // Delete conversation in ACS
                        await deleteConversation(props.Conversation.threadId);
                        // Delete the conversation in app context
                        appContext.conversations = deleteAgentHubConversation(appContext.conversations, props.Conversation.threadId);
                        // Resize and reset isSelected array to match reduced number of conversations
                        appContext.isSelected = appContext.conversations.map(() => false);
                        appContext.currentConversation = null;
                        // Update app context with a cleared current conversation and a new converaation list minus the conversation being deleted
                        appContext.update({ ...appContext });
                    }}/>
                </div>
            )}
        </ApplicationContextConsumer>
    );
};

export default Closed;
