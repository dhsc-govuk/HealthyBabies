import React from 'react';
import ReactDOM from 'react-dom/client';
import { AppInsightsContext } from '@microsoft/applicationinsights-react-js';
import App from './components/App';
import { reactPlugin } from './telemetry';
import reportAccessibility from './report-accessibility';

const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement);
root.render(
  <React.StrictMode>
    <AppInsightsContext.Provider value={reactPlugin}>
      <App />
    </AppInsightsContext.Provider>
  </React.StrictMode>
);

reportAccessibility(React);
