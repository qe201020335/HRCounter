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
}