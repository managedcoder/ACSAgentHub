import './AgentsBlade.css';

import { Agent, AgentStatus } from './Models'
import { getAgentStatusName } from '../utils/utils';
import { updateAgentStatus } from '../services/agents';
import { ApplicationContextConsumer } from './ApplicationContext'

const AgentsBlade = () => {

    async function updateStatus(agent: Agent, status: AgentStatus): Promise<void> {
        await updateAgentStatus(agent, status);
    }

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
                        {appContext.agents.map((agent, i) => (
                            <tr>
                                <td>{agent.name}</td>
                                <td>
                                    {(agent.id === appContext.agent?.id) ?
                                        <div className="dropdown">
                                            <button className="btn btn-default dropdown-toggle" type="button" id="statusMenu" data-toggle="dropdown">
                                                {getAgentStatusName(agent.status)}&nbsp;&nbsp;<span className="caret"></span>
                                            </button>
                                            <ul className="dropdown-menu" role="menu" aria-labelledby="statusMenu">
                                                <li role="presentation" className="list-item"><button role="menuitem" type="button" className="link-button" onClick={async (e): Promise<void> => { await updateStatus(agent, AgentStatus.Available); appContext.update({ ...appContext }); }}>Available</button></li>
                                                <li role="presentation" className="list-item"><button role="menuitem" type="button" className="link-button" onClick={async (e): Promise<void> => { await updateStatus(agent, AgentStatus.Unavailable); appContext.update({ ...appContext }); }} >Unavailable</button></li>
                                                <li role="presentation" className="list-item"><button role="menuitem" type="button" className="link-button" onClick={async (e): Promise<void> => { await updateStatus(agent, AgentStatus.Maxed); appContext.update({ ...appContext }); }} >Maxed</button></li>
                                                <li role="presentation" className="list-item"><button role="menuitem" type="button" className="link-button" onClick={async (e): Promise<void> => { await updateStatus(agent, AgentStatus.Away); appContext.update({ ...appContext }); }} >Away</button></li>
                                            </ul>
                                        </div>
                                        :
                                        <div>{ getAgentStatusName(agent.status) }</div>
                                    }
                                </td>
                                <td>{agent.skills.map((s, j) => <span key={j}>{j > 0 && ", "}{s}</span>)}</td>
                            </tr>
                        ))}
                    </table>
                </div>
            )}
        </ApplicationContextConsumer>
    );
};

export default AgentsBlade;
