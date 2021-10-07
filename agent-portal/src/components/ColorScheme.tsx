import './ColorScheme.css';
import React, { FC } from 'react';


const ColorScheme: FC = () => {
    return (
        <div>
            <table  >
                <tr>
                    <td className="primary-lighter">primary lighter</td>
                    <td className="secondary-lighter">secondary lighter</td>
                </tr>
                <tr>
                    <td className="primary-light">primary light</td>
                    <td className="secondary-light">secondary light</td>
                </tr>
                <tr>
                    <td className="primary">primary</td>
                    <td className="secondary">secondary</td>
                </tr>
                <tr>
                    <td className="primary-dark">primary dark</td>
                    <td className="secondary-dark">secondary dark</td>
                </tr>
                <tr>
                    <td className="primary-darker">primary darker</td>
                    <td className="secondary-darker">secondary darker</td>
                </tr>
            </table>
        </div>
    );
};

export default ColorScheme;
