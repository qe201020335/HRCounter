export class FileSaver {
    static saveBinary(content: Blob, mimeType: string, filename: string) {
        const a = document.createElement('a') // Create "a" element
        const blob = new Blob([content], {type: mimeType}) // Create a blob (file-like object)
        const url = URL.createObjectURL(blob) // Create an object URL from blob
        a.setAttribute('href', url) // Set "a" element link
        a.setAttribute('download', filename) // Set download filename
        a.click() // Start downloading
    }

    static saveText(content: string, filename: string) {
        const a = document.createElement('a')
        a.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(content));
        a.setAttribute('download', filename)
        a.click()
    }
}