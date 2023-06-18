import {createTheme} from "@mui/material/styles";

export const theme = createTheme({
    palette: {
        mode: 'dark',
        primary: {
            main: '#2084de',
        },
        secondary: {
            main: '#FB3D37',
        },
        text: {
            primary: 'rgb(143, 161, 178)',
            secondary: 'rgba(143, 161, 178, 0.7)',
            disabled: 'rgba(143, 161, 178, 0.5)',
        },
        divider: 'rgba(255,255,255,0.15)',
        background: {
            default: '#1b1b1b',
            paper: '#1f2831',
        },
    },
});