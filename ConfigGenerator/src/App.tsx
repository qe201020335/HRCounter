import './App.css';
import GeneratorMain from "./Components/Main";
import {useEffect, useMemo, useRef, useState} from "react";
import {GameConfig} from "./models/GameConfig";
import {DataSource} from "./models/DataSource";
import {DataSourceConfig} from "./models/DataSourceConfig";
import generate from "./utils/Generator";
import {GameSettingsController} from "./utils/GameSettingsController";
import {CryptHelper} from "./utils/CryptHelper";
import {GeneratorState} from "./models/GeneratorState";
import {GameConnection} from "./models/GameConnection";
import {EncodingHelper} from "./utils/EncodingHelper";
import {PulsoidOAuthResponse} from "./models/PulsoidOAuthResponse";
import {Card, Link, Spinner} from "@fluentui/react-components";

const PULSOID_OAUTH_LINK_BASE = "https://pulsoid.net/oauth2/authorize?response_type=token&client_id=a81a9e16-2960-487d-a741-92e22b757c85&redirect_uri=https://hrcounter.skyqe.net&scope=data:heart_rate:read&state="

//example state: eyJub25jZSI6IjY4VWlCZ1FYaU5lZE9nQXNNemFpMkE9PSIsImdhbWUiOnsiYWRkcmVzcyI6ImxvY2FsaG9zdCIsInBvcnQiOjY1MzAyfX0=
//https://hrcounter.skyqe.net/#token_type=bearer&access_token=xxxxxxxx&expires_in=2522880000&scope=data:heart_rate:read&state=yyyyyyy
function getPulsoidOAuthResponse(queryParameters: URLSearchParams): PulsoidOAuthResponse | null {
    const token = queryParameters.get("access_token")
    if (token === null || token === "") {
        return null
    }
    const response = new PulsoidOAuthResponse()
    response.accessToken = token
    const state = queryParameters.get("state")
    if (state === null || state === "") {
        response.state = null
    }
    try {
        const rawState = JSON.parse(EncodingHelper.base64DecodeString(state!))
        response.state = GeneratorState.fromJson(rawState)
        return response
    } catch (e) {
        console.error("Failed to parse state from pulsoid oauth response")
        console.error(e)
        response.state = null
    }

    return response
}

function getGameConnectionFromQuery(query: URLSearchParams): GameConnection {
    const address = query.get("game_ip") ?? "";
    let port = query.get("game_port") ?? "0";
    return GameConnection.fromValues(address, port);
}

// function getInitialGameConfigFromQuery(queryParameters: URLSearchParams): GameConfig {
//     const gameConfig = new GameConfig()
//     gameConfig.PulsoidToken = getPulsoidOAuthToken(queryParameters)
//     return gameConfig
// }



function App() {

    // const initialConfigFromQuery = useMemo(() => {
    //     return getInitialGameConfigFromQuery(new URLSearchParams(window.location.hash.replace("#", "?")))
    // }, [])

    const pulsoidOAuthResponse = useMemo(() => {
        return getPulsoidOAuthResponse(new URLSearchParams(window.location.hash.replace("#", "?")))
    }, [])

    const gameConnectionFromQuery = useMemo(() => {
        return getGameConnectionFromQuery(new URLSearchParams(window.location.search));
    }, [])

    const gameSettingsController = useRef<GameSettingsController | null>(null)

    const [isLoadingWithGame, setIsLoadingWithGame] = useState(true)

    const [gameConfig, setGameConfig] = useState(new GameConfig())

    const [gameConnection, setGameConnection] = useState(gameConnectionFromQuery)

    useEffect(() => {
        const processInitData = async (): Promise<GameConfig | null> => {
            console.log("Processing init data")
            const hasPulsoidOAuth = pulsoidOAuthResponse !== null && pulsoidOAuthResponse.accessToken !== "";

            if (!hasPulsoidOAuth && !gameConnectionFromQuery.isValid()) {
                //nothing to do here
                return null;
            }

            const gameConfig = new GameConfig();
            const gameConnection = new GameConnection()
            gameConnection.address = gameConnectionFromQuery.address;
            gameConnection.port = gameConnectionFromQuery.port;

            if (hasPulsoidOAuth) {
                // deal with pulsoid oauth response and fill in our data
                gameConfig.PulsoidToken = pulsoidOAuthResponse.accessToken;
                const state = pulsoidOAuthResponse.state;
                // do we need to care about csrf?
                if (state !== null && state.nonce !== "" && state.game !== null && state.game.isValid()) {
                    // It is highly unlikely that game connection info exists both in query and in pulsoid's oauth response
                    // In that case, use what we got from pulsoid
                    // TODO: is this a good idea?
                    gameConnection.address = state.game.address;
                    gameConnection.port = state.game.port;
                }
            }

            if (!gameConnection.isValid()) {
                return gameConfig;
            }

            const settingsController = new GameSettingsController(gameConnection);
            setGameConnection(gameConnection);
            let succeeded = false;
            if (gameConfig.PulsoidToken !== "") {
                // Try sending the token to game
                console.log("Trying to push pulsoid token to game")
                try {
                    const dataSourceConfig = new DataSourceConfig();
                    dataSourceConfig.DataSource = DataSource.Pulsoid;
                    dataSourceConfig.PulsoidToken = gameConfig.PulsoidToken;
                    const config = await settingsController.pushDataSourceConfig(dataSourceConfig);
                    // TODO show we successfully pushed the token
                    gameConfig.PulsoidToken = config.PulsoidToken;
                    gameConfig.HypeRateSessionID = config.HypeRateSessionID;
                    succeeded = true;
                } catch (e) {
                    // TODO: Show error message
                    console.error("Failed to push pulsoid token to game")
                    console.error(e)
                }
            } else {
                // No pulsoid token, just get the game config from game
                console.log("No Pulsoid oauth response token, trying to get game config")
                const config = await settingsController.getGameConfig();
                if (config === null) {
                    // probably the game is not running or the connection is wrong
                    // TODO: Show error message
                } else {
                    gameConfig.PulsoidToken = config.PulsoidToken;
                    gameConfig.HypeRateSessionID = config.HypeRateSessionID;
                    succeeded = true;
                }
            }

            gameSettingsController.current = succeeded ? settingsController : null;
            return gameConfig;
        }
        processInitData().then((gameConfig: GameConfig | null) => {
            console.log("Init data processed");
            setIsLoadingWithGame(false);
            if (gameConfig !== null) {
                setGameConfig(gameConfig);
            }
        })
    }, [pulsoidOAuthResponse, gameConnectionFromQuery]);

    function getStateForOAuth(): GeneratorState {
        const state = new GeneratorState();
        state.nonce = CryptHelper.generateNonce();
        if (gameSettingsController.current !== null) {
            state.game = gameSettingsController.current.getGameConnectionInfo();
        }
        return state
    }

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

    function onAuthorizeThirdParty(source: DataSource) {
        switch (source) {
            case DataSource.Pulsoid:
                const state = getStateForOAuth()
                window.location.href = PULSOID_OAUTH_LINK_BASE + EncodingHelper.base64EncodeString(JSON.stringify(state))
                break;
            default:
                console.error(`There is currently no third party authorization for ${source}`)
        }
    }

    return (
        <div className="App">
            <h1>HRCounter Easy Config Generator</h1>
            <h3>DO NOT USE THIS IF YOU ALREADY HAVE DATA SOURCE CONFIGURED</h3>
            <Card id="main-card" size="large" appearance="filled-alternative">
                {isLoadingWithGame
                    ? <div style={{display: 'flex', justifyContent: 'center', alignItems: 'center', height: 150, gap: 4}}>
                        <Spinner labelPosition="after" label={gameConnection.isValid() ? `Connecting to game at ${gameConnection.address}:${gameConnection.port}` : `Loading...`}/>
                    </div>
                    : <GeneratorMain gameConfig={gameConfig}
                                     initialSource={gameConfig.PulsoidToken === "" ? null : DataSource.Pulsoid}
                                     onSubmit={onDataSourceSubmit}
                                     onAuthorize={onAuthorizeThirdParty}/>
                }
            </Card>
            <div id="credits">
                There isn't a lot going on on this page. <Link href="https://github.com/qe201020335" target="_blank">@qe201020335</Link>
            </div>
        </div>
    )
}

export default App;
