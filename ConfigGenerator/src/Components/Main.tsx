import {ChangeEvent, useEffect, useRef, useState} from "react";

import {Box, Button, IconButton, Link, MenuItem, TextField} from "@mui/material";
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import "./Main.css"
import {GameConfig} from "../models/GameConfig";
import {DataSource} from "../models/DataSource";
import {DataSourceConfig} from "../models/DataSourceConfig";

const PULSOID_TOKEN_LINK = "https://pulsoid.net/oauth2/authorize?response_type=token&client_id=a81a9e16-2960-487d-a741-92e22b757c85&redirect_uri=https://hrcounter.skyqe.net&scope=data:heart_rate:read&state=a52beaeb-c491-4cd3-b915-16fed71e17a8"
const PULSOID_BRO_TOKEN = "https://pulsoid.net/ui/keys"
const PULSOID_HINT = (
    <p>
      If you don't have a token yet, click authorize to get one. <a href={PULSOID_TOKEN_LINK}><Button>Authorize</Button></a>
      <br/>
      Or, you can use a manually generated token <Link href={PULSOID_BRO_TOKEN} target="_blank" underline="hover">here</Link> if you are
      a <strong>BRO</strong>.
    </p>
)

//https://hrcounter.skyqe.net/#token_type=bearer&access_token=xxxxxxxx&expires_in=2522880000&scope=data:heart_rate:read&state=yyyyyyy

function Main(props: { gameConfig: GameConfig, initialSource: DataSource | null, onSubmit: (config: DataSourceConfig) => Promise<any> }) {

  const sourceInfoMap = useRef(new Map<string, string>(
        [[DataSource.Pulsoid, props.gameConfig.PulsoidToken], [DataSource.HypeRate, props.gameConfig.HypeRateSessionID]]
  ))

  const [source, setSource] = useState(props.initialSource ?? DataSource.Pulsoid);
  const [sourceInput, setSourceInput] = useState(props.gameConfig.getConfigForSource(source));

  const [showToken, setShowToken] = useState(false);

  useEffect(() => {
    console.debug("Main component mounted, updating state with props")
    console.debug(`props.initialSource: ${props.initialSource}, props.gameConfig: ${JSON.stringify(props.gameConfig)}`)
    const source = props.initialSource ?? DataSource.Pulsoid
    sourceInfoMap.current = new Map<string, string>([
            [DataSource.Pulsoid, props.gameConfig.PulsoidToken],
            [DataSource.HypeRate, props.gameConfig.HypeRateSessionID]
        ])
    setSource(source)
    setSourceInput(props.gameConfig.getConfigForSource(source))
  }, [props.gameConfig, props.initialSource])

  function onSourceChange(e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) {
    const newSource = e.target.value as DataSource
    sourceInfoMap.current.set(source, sourceInput)
    const input = sourceInfoMap.current.get(newSource) || ""
    setSource(newSource)
    setSourceInput(input)
  }

  function onShowTokenClicked() {
    setShowToken(!showToken)
  }

  function get_info(source: DataSource) {
    switch (source) {
      case DataSource.Pulsoid:
        return "Pulsoid Token"
      case DataSource.HypeRate:
        return "HypeRate Session ID"
      default:
        return ""
    }
  }

  function get_hint(source: DataSource) {
    switch (source) {
      case DataSource.Pulsoid:
        return PULSOID_HINT
      default:
        return ""
    }
  }

  const onclick = async () => {
    const config = new DataSourceConfig();
    config.DataSource = source
    switch (source) {
      case DataSource.Pulsoid:
        config.PulsoidToken = sourceInput
        break
      case DataSource.HypeRate:
        config.HypeRateSessionID = sourceInput
        break
    }

    await props.onSubmit(config)
  }

  return (
      <div>

        <Box sx={{'& > :not(style)': {m: 1},}}>
          <TextField select label="Data Source" value={source} onChange={onSourceChange} size="small" sx={{ minWidth: 125 }}>
            <MenuItem value={DataSource.Pulsoid}>Pulsoid</MenuItem>
            <MenuItem value={DataSource.HypeRate}>HypeRate</MenuItem>
          </TextField>
          <TextField
              label={get_info(source)}
              size="small"
              value={sourceInput}
              onChange={(e) => setSourceInput(e.target.value)}
              type={showToken ? 'text' : 'password'}
              autoComplete='off'
          />

          <IconButton aria-label="toggle token visibility" onClick={onShowTokenClicked} edge="end">
            {showToken ? <VisibilityOff /> : <Visibility />}
          </IconButton>


        </Box>
        <div id="data-source-hint"> {get_hint(source)} </div>
        <Button id="submit" variant="contained" color="primary" onClick={onclick}>Generate!</Button>

      </div>
  );
}

export default Main