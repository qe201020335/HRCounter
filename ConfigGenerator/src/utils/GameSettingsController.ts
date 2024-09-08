import {GameConfig} from "../models/GameConfig";
import {DataSourceConfig} from "../models/DataSourceConfig";

export class GameSettingsController {
    readonly address: string;

    readonly port: number;

    private readonly link: string;

    constructor(address: string, port: number) {
        this.address = address;
        this.port = port;
        this.link = `http://${address}:${port}/config`;
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