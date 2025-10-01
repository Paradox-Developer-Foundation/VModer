import { ViewColumn, type ExtensionContext, type WebviewPanel } from "vscode";
import { Disposable } from "vscode-languageclient";
import { WebviewHelpers } from "./WebviewHelpers";
import type { LanguageClient } from "vscode-languageclient/node";

export abstract class BaseView implements Disposable {
  protected static currentPanel: BaseView | undefined;
  private readonly _panel: WebviewPanel;
  private readonly _client: LanguageClient;
  protected _disposables: Disposable[] = [];

  protected constructor(
    panel: WebviewPanel,
    context: ExtensionContext,
    client: LanguageClient,
    webviewName: string
  ) {
    this._panel = panel;
    this._client = client;

    this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
    this._panel.webview.html = WebviewHelpers.getHtml(this._panel.webview, context, webviewName);
  }

  protected static renderOrReveal<T extends BaseView>(createFn: () => T): T {
    const currentPanel = (this.constructor as typeof BaseView).currentPanel as T | undefined;
    if (currentPanel) {
      currentPanel._panel.reveal(ViewColumn.One);
      return currentPanel;
    } else {
      const view = createFn();
      view._panel.webview.onDidReceiveMessage(
        async (message: string) => {
          if (message == "init_complete") {
            view._panel.webview.postMessage({
              type: "i18n",
              data: view.getI18n(),
            });
            await view.onInitialized(view._panel, view._client);
          }
        },
        null,
        view._disposables
      );

      return view;
    }
  }

  protected abstract getI18n(): object;

  protected async onInitialized(_panel: WebviewPanel, _client: LanguageClient): Promise<void> {}

  /**
   * Cleans up and disposes of webview resources when the webview panel is closed.
   */
  public dispose() {
    (this.constructor as typeof BaseView).currentPanel = undefined;

    // Dispose of the current webview panel
    this._panel.dispose();

    // Dispose of all disposables (i.e. commands) for the current webview panel
    while (this._disposables.length) {
      const disposable = this._disposables.pop();
      if (disposable) {
        disposable.dispose();
      }
    }
  }
}
