import './Answer.css';
import React, { FC } from 'react';
import AnswerIcon from './AnswerIcon.png';

import { Conversation, ConversationStatus, Agent, ConversationActionSize } from './Models';
import { updateConversationStatus } from '../services/conversations';
import { ApplicationContextConsumer, ApplicationContextType } from './ApplicationContext';

interface Props {
    Conversation: Conversation
}

const Answer: FC<Props> = (props: Props) => {
    return (
        <ApplicationContextConsumer>
            {appContext => appContext && (
                <div>
                    { console.log('Rendering Answer icon') }
                    <img src={AnswerIcon} alt="answer icon" height={ConversationActionSize} onClick={async (e): Promise<void> => {
                        // Assign ownership of the conversation to the current agent
                        props.Conversation.agentId = appContext!.agent!.id;
                        console.log(`props.Conversation.agent: ${props.Conversation.agentId}`);
                        if (appContext.conversations.length > 1)
                            console.log(`appContext.conversations[0].agent: ${appContext.conversations[0].agentId}, appContext.conversations[1].agent: ${appContext.conversations[1].agentId}`);

                        // Update the current conversation to the conversation that corresponds to the answer icon selected (it might be different than the currently selected conversation)
                        appContext.currentConversation = props.Conversation;

                        // Update this conversation status in UI model to reflect we are answering it
                        appContext.currentConversation.status = ConversationStatus.Accepted;
                        // Find the index of the conversation associated with this answer button
                        let selectedIndex = appContext.conversations.findIndex(c => c.threadId === props.Conversation.threadId);
                        // Select the conversation associated with this answer button
                        appContext.isSelected = appContext.isSelected.map((status, i) => (selectedIndex === i));
                        // Update the application Context
                        appContext.update({...appContext});
                        // Update the conversation status in the agent hub which will also notify bot so it can let user know escalation has been answered
                        await updateConversationStatus(props.Conversation);
                    }}/>
                </div>
            )}
        </ApplicationContextConsumer>
    );
};

export default Answer;
