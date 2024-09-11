import {GameConfig} from "../models/GameConfig";
import {DataSourceConfig} from "../models/DataSourceConfig";
import {GameConnection} from "../models/GameConnection";

export class GameSettingsController {
    readonly address: string;

    readonly port: number;

    private readonly link: string;

    constructor(gameConnection: GameConnection) {
        this.address = gameConnection.address;
        this.port = gameConnection.port;
        this.link = `http://${this.address}:${this.port}/config`;
    }

    getGameConnectionInfo(): GameConnection {
        const result = new GameConnection();
        result.address = this.address;
        result.port = this.port;
        return result;
    }

    async pushDataSourceConfig(config: DataSourceConfig): Promise<GameConfig> {
        const body = JSON.stringify(config);
        console.log(`pushing config: ${body}`);
        const res = await fetch(this.link, {
            method: "POST",
            body: body,
            headers: {
                "Content-Type": "application/json"
            },
            mode: "cors"
        });
        const json = await res.json();
        console.log(json);
        const newConfig = new GameConfig();
        newConfig.populate(json);
        return newConfig;
    }

    async getGameConfig(): Promise<GameConfig | null> {
        console.log(`getting config from ${this.link}`);
        try {
            const res = await fetch(this.link, {mode: "cors"});
            const config = await res.json();
            console.log(config);
            const result = new GameConfig();
            result.populate(config);
            return result
        } catch (e) {
            console.error(e);
            return null;
        }
    }
}