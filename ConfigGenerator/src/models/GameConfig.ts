import {DataSource} from "./DataSource";

/**
 * The configuration of the game. Only includes what we need for now.
 */
export class GameConfig {
    PulsoidToken: string = "";
    HypeRateSessionID: string = "";

    populate(json: any) {
        this.PulsoidToken = json.PulsoidToken ?? "";
        this.HypeRateSessionID = json.HypeRateSessionID ?? "";
    }

    getConfigForSource(source: DataSource): string {
        switch (source) {
            case DataSource.Pulsoid:
                return this.PulsoidToken;
            case DataSource.HypeRate:
                return this.HypeRateSessionID;
            default:
                return "";
        }
    }
}