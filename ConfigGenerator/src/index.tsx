import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import App from './App';
import reportWebVitals from "./reportWebVitals";
import {FluentProvider, useThemeClassName} from "@fluentui/react-components";
import {darkTheme} from "./theme";


const root = ReactDOM.createRoot(
    document.getElementById('root') as HTMLElement
);

/**
 * Apply the theme class to the body element.
 * This is a workaround for the Fluent UI issue where
 * it doesn't have an equivalent to MUI's CssBaseline
 * https://github.com/microsoft/fluentui/issues/23626
 */
function ApplyThemeToBody() {
    const classes = useThemeClassName();

    React.useEffect(() => {
        const classList = classes.split(" ");
        document.body.classList.add(...classList);

        return () => document.body.classList.remove(...classList);
    }, [classes]);

    return null;
}

root.render(
    <React.StrictMode>
        <FluentProvider theme={darkTheme}>
            <ApplyThemeToBody/>
            <App/>
        </FluentProvider>
    </React.StrictMode>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();