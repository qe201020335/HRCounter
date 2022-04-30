'use strict';

const button = document.getElementById("submit")
const selector = document.getElementById("source-select")
const info_span = document.getElementById("source-info-span")
const info_input = document.getElementById("source-info-input")
const hint_div = document.getElementById("data-source-hint")

const LATEST_LINK = "https://hrcounter.skyqe.workers.dev/latest"
const PULSOID_TOKEN_LINK = "https://pulsoid.net/oauth2/authorize?response_type=token&client_id=0025a50e-9449-4aa5-9c68-36d2903cb6a5&redirect_uri=&scope=data:heart_rate:read&state=&response_mode=web_page"
const PULSOID_BRO_TOKEN = "https://pulsoid.net/ui/keys"
const PULSOID_HINT = `
<p>
    If you don't have a token yet, get a token <a href="${PULSOID_TOKEN_LINK}" target="_blank">here</a> <strong>FOR FREE</strong>!
<br>
    Or, you can use a manually generated token <a href="${PULSOID_BRO_TOKEN}" target="_blank">here</a> if you are a <strong>BRO</strong> .
</p>
    `;


function update_info_label() {
    info_input.value = "";
    switch (selector.value) {
        case "Pulsoid":
            info_span.innerText = "Pulsoid Token: "
            hint_div.innerHTML = PULSOID_HINT
            break
        case "HypeRate":
            info_span.innerText = "HypeRate Session ID: "
            hint_div.innerHTML = ""
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
    let input;
    if (info_input.value === "" || !info_input.value) {
        input = "NotSet"
    } else {
        input = info_input.value
    }
    // const input = info_input.value === "" || !info_input.value ? "NotSet" : info_input.value;
    switch (selector.value) {
        case "Pulsoid":
            config = {
                "DataSource": "Pulsoid Token",
                "PulsoidToken": input
            }
            break
        case "HypeRate":
            info_span.innerText = "HypeRate Session ID: "
            config = {
                "DataSource": "HypeRate",
                "HypeRateSessionID": input
            }
            break
        default:
            return
    }
    console.log(config)

    const zip_get_res = await fetch(LATEST_LINK)
    if (zip_get_res.status !== 200) {
        return
    }

    const {name, data} = await zip_get_res.json();

    console.log(name)
    // console.log(data)

    // load the zip file
    const zip = await JSZip.loadAsync(atob(data))
    await zip.folder("UserData").file("HRCounter.json", JSON.stringify(config), null)
    const modified = await zip.generateAsync({type:"blob"})
    saveAs(modified, name)
}
