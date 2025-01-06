import {Buffer} from 'buffer'
export class EncodingHelper {
    private static textEncoder = new TextEncoder()
    private static textDecoder = new TextDecoder()

    static base64Encode(bytes: Uint8Array): string {
        const buf = Buffer.from(bytes)
        return buf.toString('base64')
    }

    static base64EncodeString(str: string): string {
        const bytes = this.textEncoder.encode(str)
        return this.base64Encode(bytes)
    }

    static base64DecodeString(str: string): string {
        const buf = Buffer.from(str, 'base64')
        return this.textDecoder.decode(buf)
    }
}