import {GameConnection} from "./GameConnection";

export class GeneratorState {
    nonce: string = "";
    game: GameConnection | null = null;

    static fromJson(json: any): GeneratorState {
        const state = new GeneratorState();
        state.nonce = json.nonce?.toString() ?? "";
        state.game = GameConnection.fromJson(json.game);
        return state;
    }
}