import { HypeRate } from "./HypeRate";

const HYPERATE_ID_RE = /^[a-zA-Z0-9_\-]+$/;
const HYPERATE_ID_HEADER = "X-HypeRate-ID";

const handle: ExportedHandlerFetchHandler<Env> = async (request, env, ctx) => {
    console.log("handling hyperate request");
    // check id 
    const hyperateId = request.headers.get(HYPERATE_ID_HEADER);
    if (!hyperateId || !hyperateId.match(HYPERATE_ID_RE)) {
        console.log("Invalid HypeRate id");
        return new Response("Invalid HypeRate id", { status: 400 });
    }

    const upgradeHeader = request.headers.get("Upgrade");
    if (!upgradeHeader || upgradeHeader !== "websocket") {
        console.log("invalid ws upgrade header");
        return new Response("Expected Upgrade: websocket", { status: 426 });
    }

    console.debug("creating websocket pair");

    const webSocketPair = new WebSocketPair();
    const [client, server] = Object.values(webSocketPair);

    const closeWS = (code?: number, reason?: string) => {
        try {
            server.close(code, reason);
            client.close(code, reason);
        } catch (e) {
            console.error("Failed to close websocket", e);
        }
    };

    const sendMessage = (message: any) => {
        if (server.readyState == WebSocket.READY_STATE_OPEN) {
            server.send(message);
            return true;
        }
        return false;
    };

    // @ts-ignore
    const hyperate = new HypeRate(hyperateId, env["HYPERATE_TOKEN"] ?? "",
        message => {
            if (!sendMessage(message)) {
                closeWS();
                hyperate.close();
            }
        },
        (code, reason) => {
            closeWS(code, reason);
        }
    );

    server.addEventListener("close", () => {
        console.log("the client is closed");
        closeWS();
        hyperate.close();
    });

    server.addEventListener("error", (e) => {
        console.warn("hyperate proxy websocket error:", e.message);
        closeWS();
        hyperate.close();
    });

    server.accept();

    return new Response(null, {
        status: 101,
        webSocket: client,
    });
};

export default {
    handle,
};
