import './App.css';
import GeneratorMain from "./Components/Main";
import {useEffect, useMemo, useRef, useState} from "react";
import {GameConfig} from "./models/GameConfig";
import {DataSource} from "./models/DataSource";
import generate from "./utils/Generator";
import {GameSettingsController} from "./utils/GameSettingsController";
import {CryptHelper} from "./utils/CryptHelper";
import {GeneratorState} from "./models/GeneratorState";
import {GameConnection} from "./models/GameConnection";
import {EncodingHelper} from "./utils/EncodingHelper";
import {PulsoidOAuthResponse} from "./models/PulsoidOAuthResponse";
import {Card, Link, Spinner} from "@fluentui/react-components";
import MessageBox from "./Components/MessageBox";
import {CheckmarkCircle48Regular, Warning48Regular} from '@fluentui/react-icons';

const PULSOID_OAUTH_LINK_BASE = "https://pulsoid.net/oauth2/authorize?response_type=token&client_id=a81a9e16-2960-487d-a741-92e22b757c85&redirect_uri=https://hrcounter.skyqe.net&scope=data:heart_rate:read&state="

//example state: eyJub25jZSI6IjY4VWlCZ1FYaU5lZE9nQXNNemFpMkE9PSIsImdhbWUiOnsiYWRkcmVzcyI6ImxvY2FsaG9zdCIsInBvcnQiOjY1MzAyfX0=
//example game params: game_ip=localhost&game_port=65302
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

enum LoadingState {
    Loading,
    CannotConnectGame,
    FailedToPushToken,
    TokenPushed,
    Loaded
}



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

    const [loadingState, setLoadingState] = useState(LoadingState.Loading)

    const [gameConfig, setGameConfig] = useState(new GameConfig())

    const [gameConnection, setGameConnection] = useState(gameConnectionFromQuery)

    useEffect(() => {
        const processInitData = async (): Promise<[LoadingState, GameConfig | null]> => {
            console.log("Processing init data")
            const hasPulsoidOAuth = pulsoidOAuthResponse !== null && pulsoidOAuthResponse.accessToken !== "";

            if (!hasPulsoidOAuth && !gameConnectionFromQuery.isValid()) {
                //nothing to do here
                return [LoadingState.Loaded, null];
            }

            const gameConfig = new GameConfig();
            const gameConnection = new GameConnection()
            gameConnection.address = gameConnectionFromQuery.address;
            gameConnection.port = gameConnectionFromQuery.port;

            if (hasPulsoidOAuth) {
                // deal with pulsoid oauth response and fill in our data
                gameConfig.PulsoidToken = pulsoidOAuthResponse.accessToken;
                gameConfig.DataSource = DataSource.Pulsoid;
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
                return [LoadingState.Loaded, gameConfig];
            }

            const settingsController = new GameSettingsController(gameConnection);
            setGameConnection(gameConnection);
            let gameConnected = false;
            let loadingState: LoadingState;
            if (gameConfig.PulsoidToken !== "") {
                // Try sending the token to game
                console.log("Trying to push pulsoid token to game")
                try {
                    const config = await settingsController.pushDataSourceConfig(gameConfig);
                    gameConfig.copyFrom(config);
                    gameConnected = true;
                    loadingState = LoadingState.TokenPushed;
                } catch (e) {
                    loadingState = LoadingState.FailedToPushToken;
                    console.error("Failed to push pulsoid token to game")
                    console.error(e)
                }
            } else {
                // No pulsoid token, just get the game config from game
                console.log("No Pulsoid oauth response token, trying to get game config")
                const config = await settingsController.getGameConfig();
                if (config === null) {
                    // probably the game is not running or the connection is wrong
                    loadingState = LoadingState.CannotConnectGame;
                } else {
                    gameConfig.copyFrom(config);
                    gameConnected = true;
                    loadingState = LoadingState.Loaded;
                }
            }

            gameSettingsController.current = gameConnected ? settingsController : null;
            return [loadingState, gameConfig];
        }
        processInitData().then((value: [LoadingState, GameConfig | null]) => {
            const [loadingState, gameConfig] = value;
            console.log("Init data processed");
            setLoadingState(loadingState);
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

    async function onDataSourceSubmit(config: GameConfig, configOnly: boolean) {
        console.log(config)
        const controller = gameSettingsController.current;
        if (controller === null) {
            await generate(config, configOnly) // download mod with config
            // TODO show error message when zip generation fails and only config is downloaded
        } else {
            // push config to game
            try {
                config = await controller.pushDataSourceConfig(config)
            } catch (e) {
                //TODO Show error message
                console.error("Failed to push config to game")
                console.error(e)
                await generate(config, true)  // download config file
            }
        }
        setGameConfig(config)
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

    function switchContentForLoadingState(state: LoadingState) {
        switch (state) {
            case LoadingState.Loading:
                const text = gameConnection.isValid()
                    ? `Connecting to game at ${gameConnection.address}:${gameConnection.port}`
                    : `Loading...`
                return <div
                    style={{display: 'flex', justifyContent: 'center', alignItems: 'center', height: 150, gap: 4}}>
                    <Spinner labelPosition="after"
                             label={text}/>
                </div>
            case LoadingState.Loaded:
                return <GeneratorMain gameConfig={gameConfig}
                                      gameConnected={gameSettingsController.current !== null}
                                      onSubmit={onDataSourceSubmit}
                                      onAuthorize={onAuthorizeThirdParty}/>
            case LoadingState.CannotConnectGame:
                return <MessageBox message={`Cannot connect to game at ${gameConnection.address}:${gameConnection.port}`}
                                   buttonText="OK"
                                   icon={<Warning48Regular color="#ff9966"/>}
                                   action={() => setLoadingState(LoadingState.Loaded)}/>
            case LoadingState.FailedToPushToken:
                return <MessageBox message={`Failed to push the token to game at ${gameConnection.address}:${gameConnection.port}`}
                                   buttonText="OK"
                                   icon={<Warning48Regular color="#ff9966"/>}
                                   action={() => setLoadingState(LoadingState.Loaded)}/>
            case LoadingState.TokenPushed:
                return <MessageBox message={"Game config updated. You can close this page now."}
                                   buttonText={null}
                                   icon={<CheckmarkCircle48Regular color="#339900"/>}
                                   action={() => setLoadingState(LoadingState.Loaded)}/>
            default:
                return <div>Place Holder</div>
        }
    }

    return (
        <div className="App">
            <h1>HRCounter Easy Config Generator</h1>
            <h3>DO NOT USE THIS IF YOU ALREADY HAVE DATA SOURCE CONFIGURED</h3>
            <Card id="main-card" size="large" appearance="filled-alternative">
                {switchContentForLoadingState(loadingState)}
            </Card>
            <div id="credits">
                There isn't a lot going on on this page. <Link href="https://github.com/qe201020335" target="_blank">@qe201020335</Link>
            </div>
        </div>
    )
}

export default App;
