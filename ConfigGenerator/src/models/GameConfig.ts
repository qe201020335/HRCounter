import {DataSource} from "./DataSource";

/**
 * The configuration of the game. Only includes what we need for now.
 */
export class GameConfig {
    DataSource: DataSource = DataSource.Pulsoid;
    PulsoidToken: string = "";
    HypeRateSessionID: string = "";

    populate(json: any) {
        this.PulsoidToken = json.PulsoidToken ?? "";
        this.HypeRateSessionID = json.HypeRateSessionID ?? "";
        if (json.DataSource)
        {
            switch (json.DataSource) {
                case DataSource.Pulsoid:
                    this.DataSource = DataSource.Pulsoid;
                    break;
                case DataSource.HypeRate:
                    this.DataSource = DataSource.HypeRate;
                    break;
            } // we only support this two sources for now
        }
    }

    copyFrom(other: GameConfig | null) {
        if (!other) return;
        this.DataSource = other.DataSource;
        this.PulsoidToken = other.PulsoidToken;
        this.HypeRateSessionID = other.HypeRateSessionID;
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