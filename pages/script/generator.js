'use strict';

const button = document.getElementById("submit")
const selector = document.getElementById("source-select")
const info_span = document.getElementById("source-info-span")
const info_input = document.getElementById("source-info-input")

const LATEST_API_LINK = "https://api.github.com/repos/qe201020335/HRCounter/releases/latest"
const RELEASE_ZIP_NAME_REGEX = "^HRCounter-[\\d\\.]+-bs[\\d\\.]+-[0-9A-Fa-f]+\\.zip$"
const re = new RegExp(RELEASE_ZIP_NAME_REGEX)

function update_info_label() {
    info_input.value = "";
    switch (selector.value) {
        case "Pulsoid":
            info_span.innerText = "Pulsoid Token: "
            break
        case "HypeRate":
            info_span.innerText = "HypeRate Session ID: "
            break
        default:
            selector.value = "Pulsoid"
    }
}

selector.onchange = () => {
    update_info_label()
}

update_info_label()

button.onclick = async () => {
    let config;
    switch (selector.value) {
        case "Pulsoid":
            config = {
                "DataSource": "Pulsoid Token",
                "PulsoidToken": info_input.value
            }
            break
        case "HypeRate":
            info_span.innerText = "HypeRate Session ID: "
            config = {
                "DataSource": "HypeRate",
                "HypeRateSessionID": info_input.value
            }
            break
        default:
            return
    }
    console.log(config)

    // get the latest release first
    const latest = await (await fetch(LATEST_API_LINK)).json();

    // console.log(latest)

    // find the zip file
    const assets = latest["assets"];

    console.log(assets)

    const zip_asset = assets.find((cur) => {
        return re.test(cur["name"])
    })

    const zip_name = zip_asset["name"]
    const zip_url = zip_asset["browser_download_url"]
    console.log(zip_name)
    console.log(zip_url)

    const zip_get_res = await fetch(zip_url)
    if (zip_get_res.status !== 200) {
        return
    }

    // load the zip file
    const zip = await JSZip.loadAsync(await zip_get_res.blob())

    // const userdata = await
    await zip.folder("UserData").file("HRCounter.json", config.toJSON(), null)
    // userdata.file("HRCounter.json", config.toJSON(), null)

    const modified = await zip.generateAsync({type:"blob"})
    saveAs(modified, zip_name)
}
