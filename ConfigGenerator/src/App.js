import './App.css';
import JSZip from "jszip";
import {useRef, useState} from "react";
import download from "./helpers";

const LATEST_LINK = "https://hrcounter.skyqe.workers.dev/latest"
const PULSOID_TOKEN_LINK = "https://pulsoid.net/oauth2/authorize?response_type=token&client_id=a81a9e16-2960-487d-a741-92e22b757c85&redirect_uri=https://hrcounter.skyqe.net&scope=data:heart_rate:read&state=a52beaeb-c491-4cd3-b915-16fed71e17a8"
const PULSOID_BRO_TOKEN = "https://pulsoid.net/ui/keys"
const PULSOID_HINT = (
    <p>
      If you don't have a token yet, click authorize to get one. <a href={PULSOID_TOKEN_LINK}><button>Authorize</button></a>
      <br/>
      Or, you can use a manually generated token <a href={PULSOID_BRO_TOKEN} target="_blank">here</a> if you are
      a <strong>BRO</strong> .
    </p>
)

//https://hrcounter.skyqe.net/#token_type=bearer&access_token=xxxxxxxx&expires_in=2522880000&scope=data:heart_rate:read&state=yyyyyyy

function App() {
  const sourceInfoMap = useRef(new Map())
  const queryParameters = useRef( new URLSearchParams(window.location.hash.replace("#", "?")))

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
        return "Pulsoid Token:"
      case "HypeRate":
        return "HypeRate Session ID: "
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
          "DataSource": "Pulsoid Token", "PulsoidToken": sourceInput
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
    console.log(config)

    const zip_get_res = await fetch(LATEST_LINK)
    if (zip_get_res.status !== 200) {
      return
    }

    const {name, data} = await zip_get_res.json();

    console.log(name)
    console.log(data.length)

    const zip = await JSZip.loadAsync(atob(data))
    if (config !== null) {
      await zip.folder("UserData").file("HRCounter.json", JSON.stringify(config), null)
    }
    const modified = await zip.generateAsync({type: "blob"})
    download(modified, "application/octet-stream", name)
  }

  return (
      <div className="App">
        <h1>HRCounter Easy Config Generator</h1>
        <h3>DO NOT USE THIS IF YOU ALREADY HAVE DATA SOURCE CONFIGURED</h3>
        <div id="main">
          <label>
            <span>Data Source: </span>
            <select name="Data Source" id="source-select" defaultValue={source} onChange={onSourceChange}>
              <option value="Pulsoid">Pulsoid Token</option>
              <option value="HypeRate">HypeRate</option>
            </select>
          </label>
          <br/>
          <label>
            <span id="source-info-span"> {get_info(source)} </span>
            <input id="source-info-input" size="25" value={sourceInput} onChange={(e) => setSourceInput(e.target.value)}/>
          </label>
          <div id="data-source-hint"> {get_hint(source)} </div>
          <button id="submit" onClick={onclick}>Generate!</button>
        </div>

        <div id="credits">
          <p>
            There isn't a lot going on on this page. It doesn't even look good.
            <br/>
            <strong>But most importantly it gets the works done.</strong>
            <a href="https://github.com/qe201020335" target="_blank">@qe201020335</a>
          </p>
        </div>
      </div>
  );
}

export default App;
