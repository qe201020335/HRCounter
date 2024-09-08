import {DataSource} from "./DataSource";

/**
 * Data model used to send the data source configuration to the game.
 *
 * undefined fields are not sent to the game (ignored by json stringify)
 * so only relevant config options are pushed to the game.
 */
export class DataSourceConfig {
    DataSource: DataSource | undefined = undefined;
    PulsoidToken: string | undefined = undefined;
    HypeRateSessionID: string | undefined = undefined;
}