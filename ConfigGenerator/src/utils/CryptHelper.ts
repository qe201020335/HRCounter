import {EncodingHelper} from "./EncodingHelper";

export class CryptHelper {
    static generateNonce(): string {
        const bytes = new Uint8Array(16);
        crypto.getRandomValues(bytes);
        return EncodingHelper.base64Encode(bytes);
    }
}