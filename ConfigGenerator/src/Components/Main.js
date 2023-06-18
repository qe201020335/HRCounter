import {useRef, useState} from "react";

import {Box, Button, Card, FormControl, InputLabel, Link, MenuItem, Select, TextField} from "@mui/material";
import "./Main.css"
import generate from "../utils/Generator";

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

function Main() {

  const sourceInfoMap = useRef(new Map())
  const queryParameters = useRef(new URLSearchParams(window.location.hash.replace("#", "?")))

  const [source, setSource] = useState("Pulsoid");
  const [sourceInput, setSourceInput] = useState(queryParameters.current.get("access_token") || "")


  function onSourceChange(e) {
    const newSource = e.target.value
    sourceInfoMap.current.set(source, sourceInput)
    const input = sourceInfoMap.current.get(newSource) || ""
    setSource(newSource)
    setSourceInput(input)
  }

  function get_info(source) {
    switch (source) {
      case "Pulsoid":
        return "Pulsoid Token"
      case "HypeRate":
        return "HypeRate Session ID"
      default:
        return ""
    }
  }

  function get_hint(source) {
    switch (source) {
      case "Pulsoid":
        return PULSOID_HINT
      default:
        return ""
    }
  }

  const onclick = async () => {
    let config;
    switch (source) {
      case "Pulsoid":
        config = {
          "DataSource": "Pulsoid", "PulsoidToken": sourceInput
        }
        break
      case "HypeRate":
        config = {
          "DataSource": "HypeRate", "HypeRateSessionID": sourceInput
        }
        break
      default:
        config = null
    }
    if (sourceInput === "") {
      config = null
    }

    await generate(config)
  }

  return (
      <Card id="main" sx={{ minWidth: 500 }}>

        <Box sx={{'& > :not(style)': {m: 1},}} noValidate autoComplete="off">
          <TextField select label="Data Source" value={source} onChange={onSourceChange} size="small" sx={{ minWidth: 125 }}>
            <MenuItem value="Pulsoid">Pulsoid</MenuItem>
            <MenuItem value="HypeRate">HypeRate</MenuItem>
          </TextField>
          <TextField label={get_info(source)} size="small" value={sourceInput} onChange={(e) => setSourceInput(e.target.value)}/>
        </Box>
        <div id="data-source-hint"> {get_hint(source)} </div>
        <Button id="submit" variant="contained" color="primary" onClick={onclick}>Generate!</Button>
        
      </Card>
  );
}

export default Main