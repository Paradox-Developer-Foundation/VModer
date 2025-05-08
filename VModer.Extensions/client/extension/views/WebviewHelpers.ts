import type { ExtensionContext, Webview } from "vscode";

export class WebviewHelpers {
    static getHtml(webview: Webview, context: ExtensionContext, input: string) {
        return process.env.VITE_DEV_SERVER_URL
            ? __getWebviewHtml__(`${process.env.VITE_DEV_SERVER_URL}src/html/${input}.html`)
            : __getWebviewHtml__(webview, context, input);
    }
}
