import './AnalyticsBlade.css';
import React, { FC } from 'react';

const AnalyticsBlade: FC = () => {
    return (
        <div>
            <div className="title">Interesting Analytics Should Go Here</div>
            <div className="subTitle">(Avg Q wait, Avg # msg per escalation, Drop off rate, etc.)</div>
        </div>
    );
};

export default AnalyticsBlade;
