import {BrandVariants, createDarkTheme, createLightTheme, Theme} from "@fluentui/react-components";

const theme: BrandVariants = {
    10: "#010307",
    20: "#08182B",
    30: "#00274A",
    40: "#00335E",
    50: "#004072",
    60: "#004D88",
    70: "#005A9E",
    80: "#0068B4",
    90: "#0076CC",
    100: "#1F84DE",
    110: "#3C92ED",
    120: "#56A0F8",
    130: "#72AEFF",
    140: "#91BCFF",
    150: "#ABCAFF",
    160: "#C4D9FF"
};

const lightTheme: Theme = {
    ...createLightTheme(theme),
};

const darkTheme: Theme = {
    ...createDarkTheme(theme),
};

darkTheme.colorBrandForeground1 = theme[110];
darkTheme.colorBrandForeground2 = theme[120];

export {darkTheme, lightTheme};