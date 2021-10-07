import './ConversationsBlade.css';
import React, { FC } from 'react';

import ConversationsPanel from './ConversationsPanel';
import ChatPanel from './ChatPanel';

const ConversationsBlade: FC = () => {
    return (
        <div className="conversationBlade-grid-container">
            <ConversationsPanel />
            <ChatPanel />
        </div>
    );
};

export default ConversationsBlade;
