import './App.css';
import GeneratorMain from "./Components/Main";
import {Box, Card, CircularProgress, Link} from "@mui/material";
import {useEffect, useMemo, useRef, useState} from "react";
import {GameConfig} from "./models/GameConfig";
import {DataSource} from "./models/DataSource";
import {DataSourceConfig} from "./models/DataSourceConfig";
import generate from "./utils/Generator";
import {GameSettingsController} from "./utils/GameSettingsController";

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

    const initialConfigFromQuery = useMemo(() => {
        return getInitialGameConfigFromQuery(new URLSearchParams(window.location.hash.replace("#", "?")))
    }, [])

    const gameConnectionFromQuery = useMemo(() => {
        const query = new URLSearchParams(window.location.search);
        const address = query.get("game_ip") ?? "";
        let port = parseInt(query.get("game_port") ?? "0");
        if (isNaN(port)) {
            port = 0;
        }

        return {address, port}

    }, [])

    const gameSettingsController = useRef<GameSettingsController | null>(
        (gameConnectionFromQuery.address === "" || gameConnectionFromQuery.port === 0)
            ? null
            : new GameSettingsController(gameConnectionFromQuery.address, gameConnectionFromQuery.port)
    )

    const [isLoadingWithGame, setIsLoadingWithGame] = useState(gameSettingsController.current !== null)

    const [gameConfig, setGameConfig] = useState(initialConfigFromQuery)

    useEffect(() => {
        const controller = gameSettingsController.current;
        if (controller === null) {
            return
        }

        controller.getGameConfig().then((config) => {
            setIsLoadingWithGame(false)
            if (config === null) {
                // probably the game is not running or the connection is wrong
                // TODO: Show error message
                gameSettingsController.current = null
                return
            }
            if (initialConfigFromQuery.PulsoidToken !== "") {
                // A highly unlikely scenario, but just in case
                // If we have an oauth token in the query, use that instead of the one from the game
                config.PulsoidToken = initialConfigFromQuery.PulsoidToken
            }
            setGameConfig(config)
        })

    }, [gameSettingsController, gameConnectionFromQuery, initialConfigFromQuery]);

    async function onDataSourceSubmit(source: DataSourceConfig) {
        console.log(source)
        const controller = gameSettingsController.current;
        if (controller === null) {
            await generate(source) // download mod with config
        } else {
            // push config to game
            try {
                const config = await controller.pushDataSourceConfig(source)
                setGameConfig(config)
            } catch (e) {
                //TODO Show error message
                console.error(e)
            }
        }
    }

    return (
        <div className="App">
            <h1>HRCounter Easy Config Generator</h1>
            <h3>DO NOT USE THIS IF YOU ALREADY HAVE DATA SOURCE CONFIGURED</h3>
            <Card id="main-card" sx={{minWidth: 500}}>
                {isLoadingWithGame
                    ? <Box sx={{display: 'flex', justifyContent: 'center', alignItems: 'center', height: 150, gap: 4}}>
                        <CircularProgress/>
                        <span>Connecting to game at {gameSettingsController.current !== null ? `${gameSettingsController.current.address}:${gameSettingsController.current.port}` : "null"}</span>
                    </Box>
                    : <GeneratorMain gameConfig={gameConfig}
                                     initialSource={gameConfig.PulsoidToken === "" ? null : DataSource.Pulsoid}
                                     onSubmit={onDataSourceSubmit}/>
                }
            </Card>
            <div id="credits">
                There isn't a lot going on on this page. <Link href="https://github.com/qe201020335" target="_blank" underline="hover">@qe201020335</Link>
            </div>
        </div>
    )
}

export default App;
