import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import App from './App';
import {CssBaseline} from "@mui/material";
import {ThemeProvider} from '@mui/material/styles';
import {theme} from "./theme";
import {DevSupport} from "@react-buddy/ide-toolbox";
import {ComponentPreviews, useInitial} from "./dev";


const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
    <React.StrictMode>
        <ThemeProvider theme={theme}>
            <CssBaseline/>
            <DevSupport ComponentPreviews={ComponentPreviews} useInitialHook={useInitial}>
                <App/>
            </DevSupport>
        </ThemeProvider>
    </React.StrictMode>
);
