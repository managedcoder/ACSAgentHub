import './AgentSignIn.css';
import React, { FC } from 'react';

import { Agent, AgentSignInCallback } from './Models'
import { ApplicationContextConsumer } from './ApplicationContext'

const AgentsSignIn: FC = () => {

    return (
        <ApplicationContextConsumer>
            {appContext => appContext && (
                <div>
                    <table>
                        <tr>
                            <th>Agent</th>
                            <th>Status</th>
                            <th>Skills</th>
                        </tr>
                        {appContext.agents.map((item, i) => (
                            <tr>
                                <td>{item.name}</td>
                                <button onClick={(e) => { console.log(`Signing in agent: ${item.name} agentId: ${item.id}`); appContext.update({ ...appContext, agent: item }); }} type="button">Sign In</button>
                            </tr>))}
                    </table>
                </div>
            )}
        </ApplicationContextConsumer>
    );
};

export default AgentsSignIn;
