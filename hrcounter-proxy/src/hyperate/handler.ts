import { HypeRate } from "./HypeRate";

const HYPERATE_ID_RE = /^[a-zA-Z0-9_\-]+$/;
const HYPERATE_ID_HEADER = "X-HypeRate-ID";

export const handleHypeRate: ExportedHandlerFetchHandler<Env> = async (request, env, ctx) => {
    console.log("handling hyperate request");
    // check id 
    const hyperateId = request.headers.get(HYPERATE_ID_HEADER);
    if (!hyperateId || !hyperateId.match(HYPERATE_ID_RE)) {
        // console.log("Invalid HypeRate id");
        return new Response("Invalid HypeRate id", { status: 400 });
    }

    const upgradeHeader = request.headers.get("Upgrade");
    if (!upgradeHeader || upgradeHeader !== "websocket") {
        // console.log("invalid ws upgrade header");
        return new Response("Expected Upgrade: websocket", { status: 426 });
    }

    // console.debug("creating websocket pair");

    const webSocketPair = new WebSocketPair();
    const [client, server] = Object.values(webSocketPair);

    const sendMessage = (message: any) => {
        try {
            if (server.readyState == WebSocket.READY_STATE_OPEN) {
                server.send(message);
            } else if (server.readyState != WebSocket.CONNECTING) {
                hyperate.close();
            }
        } catch (e) {
            console.error("Error sending message to client ws:", e);
        }
    };

    let hyperate: HypeRate;
    try {
        // @ts-ignore
        hyperate = new HypeRate(hyperateId, env["HYPERATE_TOKEN"] ?? "",
            sendMessage,
            (code, reason) => {
                server.close(code, reason);
            }
        );
    } catch (e) {
        console.error("Failed to create HypeRate client", e);
        return new Response("Internal Server Error", { status: 500 });
    }

    server.addEventListener("close", () => {
        console.log("client ws closed");
        try {
            server.close();
            hyperate.close();
        } catch (e) {
            console.error("Error in client ws onClose handler:", e);
        }
    });

    server.addEventListener("error", (e) => {
        console.error("client websocket error:", e);
        try {
            hyperate.close();
        } catch (e) {
            console.error("Error in client ws onError handler:", e);
        }
    });

    server.accept();

    return new Response(null, {
        status: 101,
        webSocket: client,
    });
};
