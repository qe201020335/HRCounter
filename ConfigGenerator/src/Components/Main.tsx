import {useEffect, useRef, useState} from "react";

import "./Main.css"
import {GameConfig} from "../models/GameConfig";
import {DataSource} from "../models/DataSource";
import {DataSourceConfig} from "../models/DataSourceConfig";
import {Button, Dropdown, Input, Link, Option, OptionOnSelectData, SelectionEvents} from "@fluentui/react-components";
import { EyeRegular, EyeOffRegular } from "@fluentui/react-icons";

const PULSOID_BRO_TOKEN = "https://pulsoid.net/ui/keys"

function Main(props: { gameConfig: GameConfig, initialSource: DataSource | null, onSubmit: (config: DataSourceConfig) => Promise<any>, onAuthorize: (source: DataSource) => any }) {

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
        <Button id="submit" size="large" appearance="primary" onClick={onclick}>Generate!</Button>

      </div>
  );
}

export default Main