import './Header.css';
import React, { FC } from 'react';

import HeaderImage from '../images/ACSAgentHubHeader.png';
import { ApplicationContextConsumer } from './ApplicationContext'

const Header: FC = () => {
    return (
        <ApplicationContextConsumer>
            {appContext => appContext && (
                <div className="minimizePerimeter" >
                    <table className="minimizePerimeter">
                        <tr className="minimizePerimeter">
                            <td className="headerCommandBar minimizePerimeter">
                                {(appContext.agent) ? appContext.agent.name : ""} {appContext.conversations.length} conversations
                            </td>
                        </tr>
                        <tr className="headerImageRow minimizePerimeter">
                            <td className="minimizePerimeter">
                                <img className="headerImage" src={HeaderImage} />
                            </td>
                        </tr>
                    </table>
                </div>
            )}
        </ApplicationContextConsumer>
    );
};

export default Header;
