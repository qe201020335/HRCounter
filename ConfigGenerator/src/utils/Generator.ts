import JSZip from "jszip";
import {FileSaver} from "./FileSaver";

const LATEST_LINK = "https://hrcounter.skyqe.workers.dev/latest"

async function generate(config: any) {
  console.log(config)

  const zip_get_res = await fetch(LATEST_LINK)
  if (zip_get_res.status !== 200) {
    return // TODO download the config json
  }

  const {name, data} = await zip_get_res.json();

  console.log(name)
  console.log(data.length)

  const zip = await JSZip.loadAsync(atob(data))
  if (config !== null) {
    zip.folder("UserData")!.file("HRCounter.json", JSON.stringify(config))
  }
  const modified = await zip.generateAsync({type: "blob"})
  FileSaver.saveBinary(modified, "application/octet-stream", name)
}

export default generate