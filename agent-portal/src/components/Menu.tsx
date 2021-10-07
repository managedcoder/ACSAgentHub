import './Menu.css';
import React, { FC } from 'react';

import AgentSmallIcon from '../images/AgentSmallIcon.png';
import ConversationIcon from '../images/ConversationIcon.png';
import AnalyticsIcon from '../images/AnalyticsIcon.png';

import { BladeSelectorCallback } from './Models'

interface Props {
    onBladeSelected: BladeSelectorCallback
}

const Menu: FC<Props> = (props: Props) => {
    const conversationsBlade = "ConversationsBlade";
    const agentsBlade = "AgentsBlade";
    const analyticsBlade = "AnalyticsBlade";

    return (
        <div className="w3-bar-block bladeMenu minimizePerimeter" >
            <div className="bladeMenuList minimizePerimeter">
                <div className="menuLabel">
                    <img className="w3-bar-item w3-button " src={ConversationIcon} onClick={() => props.onBladeSelected(conversationsBlade)} />
                    <span>TALK</span>
                </div>
                <div className="menuLabel">
                    <img className="w3-bar-item w3-button " src={AgentSmallIcon} onClick={() => props.onBladeSelected(agentsBlade)} />
                    AGENTS
                </div>
                <div className="menuLabel">
                    <img className="w3-bar-item w3-button " src={AnalyticsIcon} onClick={() => props.onBladeSelected(analyticsBlade)} />
                    ANALYTICS
                </div>
            </div>
        </div>
    );
};

export default Menu;
