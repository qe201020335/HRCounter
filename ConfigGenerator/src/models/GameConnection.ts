export class GameConnection {
    address: string = "";
    port: number = 0;

    isValid(): boolean {
        return this.address !== "" && this.port !== 0;
    }

    static fromValues(address: any, port: any): GameConnection {
        try {
            const connection = new GameConnection();
            connection.address = address?.toString() ?? "";
            port = parseInt(port);
            if (isNaN(port)) {
                port = 0;
            }
            connection.port = port;
            return connection
        } catch (e) {
            console.error("Failed to parse game connection values");
            console.error(e);
        }

        return new GameConnection();
    }

    static fromJson(json: any): GameConnection | null {
        if (json === null || json === undefined) {
            return null;
        }

        return GameConnection.fromValues(json.address, json.port);
    }
}