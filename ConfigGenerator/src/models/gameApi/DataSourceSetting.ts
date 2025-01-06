import {DataSource} from "../DataSource";
import {GameConfig} from "../GameConfig";

/**
 * Data model used to send the data source configuration to the game.
 *
 * undefined fields are not sent to the game (ignored by json stringify)
 * so only relevant config options are pushed to the game.
 */
export class DataSourceSetting {
    DataSource: DataSource | undefined = undefined;
    PulsoidToken: string | undefined = undefined;
    HypeRateSessionID: string | undefined = undefined;

    static fromGameConfig(config: GameConfig): DataSourceSetting {
        const result = new DataSourceSetting();
        result.DataSource = config.DataSource;
        switch (config.DataSource) {
            case DataSource.Pulsoid:
                result.DataSource = DataSource.Pulsoid;
                result.PulsoidToken = config.PulsoidToken;
                break;
            case DataSource.HypeRate:
                result.DataSource = DataSource.HypeRate;
                result.HypeRateSessionID = config.HypeRateSessionID;
                break
        }
        return result;
    }
}