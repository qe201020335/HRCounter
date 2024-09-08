import './App.css';
import GeneratorMain from "./Components/Main";
import {Link} from "@mui/material";
import {useRef, useState} from "react";
import {GameConfig} from "./models/GameConfig";
import {DataSource} from "./models/DataSource";
import {DataSourceConfig} from "./models/DataSourceConfig";
import generate from "./utils/Generator";

function getPulsoidOAuthToken(queryParameters: URLSearchParams): string {
    const token = queryParameters.get("access_token")
    if (token === null || token === "") {
        return ""
    }

    return token
}

function getInitialGameConfigFromQuery(queryParameters: URLSearchParams): GameConfig {
    const gameConfig = new GameConfig()
    gameConfig.PulsoidToken = getPulsoidOAuthToken(queryParameters)
    return gameConfig
}

function App() {

    const queryParameters = useRef(new URLSearchParams(window.location.hash.replace("#", "?")))

    const [gameConfig, setGameConfig] = useState(getInitialGameConfigFromQuery(queryParameters.current))

    async function onDataSourceSubmit(source: DataSourceConfig) {
        console.log(source)
        await generate(source)
    }

    return (
        <div className="App" >
            <h1>HRCounter Easy Config Generator</h1>
            <h3>DO NOT USE THIS IF YOU ALREADY HAVE DATA SOURCE CONFIGURED</h3>

            <GeneratorMain gameConfig={gameConfig} initialSource={gameConfig.PulsoidToken === "" ? null : DataSource.Pulsoid} onSubmit={onDataSourceSubmit}/>

            <div id="credits">
                There isn't a lot going on on this page. <Link href="https://github.com/qe201020335" target="_blank" underline="hover">@qe201020335</Link>
            </div>
        </div>
    )
}

export default App;
