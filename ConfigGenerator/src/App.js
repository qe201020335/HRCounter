import './App.css';
import JSZip from "jszip";
import {useState} from "react";
import download from "./helpers";

const LATEST_LINK = "https://hrcounter.skyqe.workers.dev/latest"
const PULSOID_TOKEN_LINK = "https://pulsoid.net/oauth2/authorize?response_type=token&client_id=0025a50e-9449-4aa5-9c68-36d2903cb6a5&redirect_uri=&scope=data:heart_rate:read&state=&response_mode=web_page"
const PULSOID_BRO_TOKEN = "https://pulsoid.net/ui/keys"
const PULSOID_HINT = (
    <p>
      If you don't have a token yet, get a token <a href={PULSOID_TOKEN_LINK} target="_blank">here</a> <strong>FOR
      FREE</strong>!
      <br/>
      Or, you can use a manually generated token <a href={PULSOID_BRO_TOKEN} target="_blank">here</a> if you are
      a <strong>BRO</strong> .
    </p>
)


function App() {

  const [source, setSource] = useState("Pulsoid");
  const [source_input, set_source_input] = useState("")

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
          "DataSource": "Pulsoid Token", "PulsoidToken": source_input
        }
        break
      case "HypeRate":
        config = {
          "DataSource": "HypeRate", "HypeRateSessionID": source_input
        }
        break
      default:
        config = null
    }
    if (source_input === "") {
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
            <span>Data Source</span>
            <select name="Data Source" id="source-select" defaultValue={source}
                    onChange={(e) => setSource(e.target.value)}>
              <option value="Pulsoid">Pulsoid Token</option>
              <option value="HypeRate">HypeRate</option>
            </select>
          </label>
          <br/>
          <label>
            <span id="source-info-span"> {get_info(source)} </span>
            <input id="source-info-input" size="25" value={source_input}
                   onChange={(e) => set_source_input(e.target.value)}/>
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
