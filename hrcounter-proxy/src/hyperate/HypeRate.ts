export type OnMessageHandler = (data: any) => void;
export type OnCloseHandler = (code?: number, reason?: string) => void;

export class HypeRate {

    private readonly id: string;
    private readonly ws: WebSocket;

    private keepAliveIntervalId?: number;

    constructor(id: string, token: string, onMessage: OnMessageHandler, onClose: OnCloseHandler) {
        console.log("Creating HypeRate connection for id", id);
        this.id = id;
        const ws = new WebSocket("wss://app.hyperate.io/socket/websocket?token=" + token);
        ws.addEventListener("open", () => {
            this.sendMessage(`{"topic": "hr:${id}","event": "phx_join","payload": {},"ref": 0}`);
            this.startKeepAlive();
        });
        ws.addEventListener("message", (e) => {
            console.debug("Received message", e.data);
            onMessage(e.data);
        });
        ws.addEventListener("close", (e) => {
            console.log("hyperate websocket closed");
            onClose(e.code, e.reason);
        });
        ws.addEventListener("error", (e) => {
            console.warn("hyperate websocket error:", e.message);
            try {
                this.ws.close();
            } catch (e) {
            }
            onClose(1011, e.message);
        });
        this.ws = ws;
    }

    private sendMessage(message: any) {
        console.debug("Sending message", message);
        if (this.ws.readyState == WebSocket.READY_STATE_OPEN) {
            this.ws.send(message);
            return true;
        }
        return false;
    }

    private startKeepAlive() {
        this.keepAliveIntervalId = setInterval(() => {
            const open = this.sendMessage(`{"topic": "phoenix","event": "heartbeat","payload": {},"ref": 0}`);
            if (!open) {
                this.close();
            }
        }, 9000);
    }

    close() {
        console.log("Closing HypeRate connection for id", this.id);
        if (this.keepAliveIntervalId !== undefined) {
            clearInterval(this.keepAliveIntervalId);
            this.keepAliveIntervalId = undefined;
        }
        try {
            this.ws.close();
        } catch (e) {
            console.error("Failed to close websocket", e);
        }
    }
}
