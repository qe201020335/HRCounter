import JSZip from "jszip";
import {FileSaver} from "./FileSaver";
import {GameConfig} from "../models/GameConfig";
import {DataSourceSetting} from "../models/gameApi/DataSourceSetting";

const LATEST_LINK = "https://hrcounter.skyqe.workers.dev/latest"
const CONFIG_NAME = "HRCounter.json"
const CONFIG_DIR = "UserData"

async function generate(configData: GameConfig, configOnly: boolean): Promise<boolean> {
  if (configData === null) return true;

  const setting = DataSourceSetting.fromGameConfig(configData)
  console.log(setting)

  const configJson = JSON.stringify(setting)

  if (configOnly)
  {
    FileSaver.saveText(configJson, CONFIG_NAME);
    return true;
  }

  const zip_get_res = await fetch(LATEST_LINK)
  if (zip_get_res.status !== 200) {
    FileSaver.saveText(configJson, CONFIG_NAME);
    return false;
  }

  const {name, data} = await zip_get_res.json();

  console.log(name)
  console.log(data.length)

  const zip = await JSZip.loadAsync(atob(data))
  zip.folder(CONFIG_DIR)!.file(CONFIG_NAME, configJson)
  const modified = await zip.generateAsync({type: "blob"})
  FileSaver.saveBinary(modified, "application/octet-stream", name)
  return true;
}

export default generate