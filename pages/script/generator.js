'use strict';

const button = document.getElementById("submit")
const selector = document.getElementById("source-select")
const info_span = document.getElementById("source-info-span")
const info_input = document.getElementById("source-info-input")

const LATEST_LINK = "https://hrcounter.skyqe.workers.dev/latest"

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

    const zip_get_res = await fetch(LATEST_LINK, {mode: "cors"})
    if (zip_get_res.status !== 200) {
        return
    }

    const {name, data} = await zip_get_res.json();

    console.log(name)
    console.log(data)

    // load the zip file
    const zip = await JSZip.loadAsync(atob(data))

    // const userdata = await
    await zip.folder("UserData").file("HRCounter.json", JSON.stringify(config), null)
    // userdata.file("HRCounter.json", config.toJSON(), null)

    const modified = await zip.generateAsync({type:"blob"})
    saveAs(modified, name)
}
