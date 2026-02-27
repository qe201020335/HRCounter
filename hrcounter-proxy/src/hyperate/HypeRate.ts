export type OnMessageHandler = (data: any) => void;
export type OnCloseHandler = (code?: number, reason?: string) => void;

export class HypeRate {

    private readonly id: string;
    private readonly ws: WebSocket;

    private keepAliveIntervalId?: number;

    private closed: boolean = false;

    constructor(id: string, token: string, onMessage: OnMessageHandler, onClose: OnCloseHandler) {
        console.log("Creating HypeRate connection for id", id);
        this.id = id;
        const ws = new WebSocket("wss://app.hyperate.io/socket/websocket?token=" + token);
        ws.addEventListener("open", () => {
            try {
                this.sendMessage(`{"topic": "hr:${id}","event": "phx_join","payload": {},"ref": 0}`);
                this.startKeepAlive();
            } catch (e) {
                console.error("Error in HypeRate ws onOpen handler:", e);
            }
        });
        ws.addEventListener("message", (e) => {
            // console.debug("Received message", e.data);
            try {
                onMessage(e.data);
            } catch (e) {
                console.error("Error in HypeRate ws onMessage handler:", e);
            }
        });
        ws.addEventListener("close", (e) => {
            console.log("hyperate websocket closed");
            try {
                if (this.closed) {
                    return;
                }
                onClose(e.code, e.reason);
                this.cleanup();
            } catch (e) {
                console.log("Error in HypeRate ws onClose handler:", e);
            }
            
        });
        ws.addEventListener("error", (e) => {
            console.warn("hyperate websocket error:", e.message);
            try {
                onClose(1011, e.message);
            } catch (e) {
                console.error("Error in HypeRate ws onError handler:", e);
            }
        });
        this.ws = ws;
    }

    private sendMessage(message: any) {
        // console.debug("Sending message", message);
        try {
            if (this.ws.readyState == WebSocket.READY_STATE_OPEN) {
                this.ws.send(message);
            }
        } catch (e) {
            console.error("Error sending message to HypeRate ws:", e);
        }
    }

    private startKeepAlive() {
        this.keepAliveIntervalId = setInterval(() => {
            this.sendMessage(`{"topic": "phoenix","event": "heartbeat","payload": {},"ref": 0}`);
        }, 14500);
    }

    private cleanup() {
        console.log("Closing HypeRate connection for id", this.id);
        this.closed = true;
        if (this.keepAliveIntervalId !== undefined) {
            clearInterval(this.keepAliveIntervalId);
            this.keepAliveIntervalId = undefined;
        }
    }

    close() {
        this.cleanup();
        this.ws.close();
    }
}
