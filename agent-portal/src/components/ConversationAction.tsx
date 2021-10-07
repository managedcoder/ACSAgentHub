import './ConversationAction.css';
import React, { FC } from 'react';

import { Conversation, ConversationStatus } from './Models';
import Answer from './Answer';
import HangUp from './HangUp';
import Closed from './Closed';

interface Props {
    Conversation: Conversation,
}

function GetAction(props: Props) {
    if (props.Conversation.status === ConversationStatus.Queued) {
        return <Answer Conversation={props.Conversation} />
    }
    else if (props.Conversation.status === ConversationStatus.Accepted) {
        return <HangUp Conversation={props.Conversation} />
    }
    else if (props.Conversation.status === ConversationStatus.Closed) {
        return <Closed Conversation={props.Conversation} />
    }
}

const ConversationAction: FC<Props> = (props: Props) => {
    return (
        <div>
            <td className="minimizePerimeter"> {GetAction(props)} </td>
        </div>
    );
};

export default ConversationAction;
