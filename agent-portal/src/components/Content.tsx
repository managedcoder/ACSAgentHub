import './Content.css';
import React, { FC, useState } from 'react';
import Menu from './Menu';
import ConversationsBlade from './ConversationsBlade'
import AgentsBlade from './AgentsBlade'
import AnalyticsBlade from './AnalyticsBlade'

const Content: FC = () => {
    const conversationsBlade = "ConversationsBlade";
    const agentsBlade = "AgentsBlade";
    const analyticsBlade = "AnalyticsBlade";
    const [blade, setBlade] = useState<string>(conversationsBlade);
    let bladeComponent;

    const selectBlade = (blade: string) => {
        setBlade(blade);
    }

    if (blade === conversationsBlade) {
        bladeComponent = <ConversationsBlade />
    }
    else if (blade === agentsBlade) {
        bladeComponent = <AgentsBlade />
    }
    else if (blade === analyticsBlade) {
        bladeComponent = <AnalyticsBlade />
    }

    return (
        <div className="content">
            <div className="content-grid-container">
                <Menu onBladeSelected={selectBlade} />
                {bladeComponent}
            </div>
        </div>
    );
};

export default Content;
