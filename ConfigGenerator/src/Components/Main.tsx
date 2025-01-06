import {useEffect, useRef, useState} from "react";

import "./Main.css"
import {GameConfig} from "../models/GameConfig";
import {DataSource} from "../models/DataSource";
import {
  Button,
  Caption1,
  Checkbox,
  Dropdown,
  Input,
  Link,
  Option,
  OptionOnSelectData,
  SelectionEvents,
  Spinner,
  Tooltip
} from "@fluentui/react-components";
import { EyeRegular, EyeOffRegular } from "@fluentui/react-icons";

const PULSOID_BRO_TOKEN = "https://pulsoid.net/ui/keys"

function Main(props: { gameConfig: GameConfig, gameConnected: boolean, onSubmit: (config: GameConfig, configOnly: boolean) => Promise<any>, onAuthorize: (source: DataSource) => any }) {

  const sourceInfoMap = useRef(new Map<string, string>(
        [[DataSource.Pulsoid, props.gameConfig.PulsoidToken], [DataSource.HypeRate, props.gameConfig.HypeRateSessionID]]
  ))

  const [source, setSource] = useState(props.gameConfig.DataSource);
  const [sourceInput, setSourceInput] = useState(props.gameConfig.getConfigForSource(source));

  const [showToken, setShowToken] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [downloadConfigOnly, setDownloadConfigOnly] = useState(false);

  useEffect(() => {
    console.debug("Main component mounted, updating state with props")
    console.debug(`props.gameConfig: ${JSON.stringify(props.gameConfig)}`)
    sourceInfoMap.current = new Map<string, string>([
            [DataSource.Pulsoid, props.gameConfig.PulsoidToken],
            [DataSource.HypeRate, props.gameConfig.HypeRateSessionID]
        ])
    setSourceInput(props.gameConfig.getConfigForSource(props.gameConfig.DataSource))
  }, [props.gameConfig])

  function onSourceChange(event: SelectionEvents, data: OptionOnSelectData) {
    const newSource = data.selectedOptions[0] as DataSource
    sourceInfoMap.current.set(source, sourceInput)
    const input = sourceInfoMap.current.get(newSource) || ""
    setSource(newSource)
    setSourceInput(input)
  }

  function onShowTokenClicked() {
    setShowToken(!showToken)
  }
  function onAuthorizeClicked() {
    props.onAuthorize(source)
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
        return (
            <p>
              If you don't have a token yet, click authorize to get one. <Button appearance="subtle" onClick={onAuthorizeClicked}>Authorize</Button>
              <br/>
              Or, you can use a manually generated token <Link href={PULSOID_BRO_TOKEN} target="_blank">here</Link> if you are a <strong>BRO</strong>.
            </p>
        )
      default:
        return ""
    }
  }

  const onclick = async () => {
    setIsSaving(true);
    const config = new GameConfig();
    config.DataSource = source
    switch (source) {
      case DataSource.Pulsoid:
        config.PulsoidToken = sourceInput
        break
      case DataSource.HypeRate:
        config.HypeRateSessionID = sourceInput
        break
    }

    await Promise.all([props.onSubmit(config, downloadConfigOnly), new Promise(r => setTimeout(r, 500))])
    setIsSaving(false);
  }

  return (
      <div>

        <div style={{margin: 4}}>
          <Dropdown aria-label="Data Source" value={source} onOptionSelect={onSourceChange} size="large" style={{ minWidth: 125 }}>
            <Option value={DataSource.Pulsoid}>Pulsoid</Option>
            <Option value={DataSource.HypeRate}>HypeRate</Option>
          </Dropdown>

          <Input aria-label={get_info(source)} placeholder={get_info(source)} value={sourceInput} size="large"
                 type={showToken ? "text" : "password"}
                 style={{marginLeft: 8, minWidth: 280}}
                 contentAfter={
                   sourceInput === "" ? null :
                   <Button
                       icon={showToken ? <EyeOffRegular /> : <EyeRegular />}
                       aria-label={showToken ? "Hide token" : "Show token"}
                       onClick={onShowTokenClicked}
                       shape="circular"
                       appearance="transparent"
                   />
                 }
                 onChange={(e) => setSourceInput(e.target.value)}
          />


        </div>
        <div id="data-source-hint"> {get_hint(source)} </div>

        <Tooltip relationship="description" positioning="above-end"
                 content={props.gameConnected ? "Config will be sent to game directly." : "If checked, only the config file will be downloaded. Useful for manual installation."}>
          <Checkbox id="show" label="Download config file only" disabled={props.gameConnected}
                    checked={downloadConfigOnly}
                    style={{marginBottom: 16}}
                    onChange={(_, data) => setDownloadConfigOnly(data.checked === true)}/>
        </Tooltip>

        <div style={{display: "flex", alignItems: "flex-end", gap: 8}}>
          <Button id="submit" size="large" appearance="primary" onClick={onclick} disabled={isSaving}
                  icon={isSaving ? <Spinner size="tiny"/> : null}>
            {props.gameConnected ? "Save" : "Generate!"}
          </Button>
          {
              props.gameConnected &&
              <Caption1>Config will be sent to game directly.</Caption1>
          }
        </div>
      </div>
  );
}

export default Main