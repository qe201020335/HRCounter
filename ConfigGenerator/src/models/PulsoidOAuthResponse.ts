import {GeneratorState} from "./GeneratorState";

export class PulsoidOAuthResponse {
    accessToken: string = "";
    state: GeneratorState | null = null;
}