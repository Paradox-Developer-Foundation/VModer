import {
  ExtensionContext,
  l10n,
  ViewColumn,
  WebviewPanel,
  window,
} from "vscode";
import { Disposable } from "vscode-languageclient";
import { WebviewHelpers } from "./WebviewHelpers";
import { LanguageClient } from 'vscode-languageclient/node';

export class ModifierQuerierView {
  public static currentPanel: ModifierQuerierView | undefined;
  private readonly _panel: WebviewPanel;
  private _disposables: Disposable[] = [];

  constructor(panel: WebviewPanel, context: ExtensionContext) {
    this._panel = panel;

    this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
    this._panel.webview.html = WebviewHelpers.getHtml(
      this._panel.webview,
      context,
      "modifierQuerierView"
    );
  }

  public static render(context: ExtensionContext, client: LanguageClient) {
    if (ModifierQuerierView.currentPanel) {
      ModifierQuerierView.currentPanel._panel.reveal(ViewColumn.One);
    } else {
      const panel = window.createWebviewPanel(
        "traitsView",
        l10n.t("TraitsView.Title"),
        ViewColumn.One,
        {
          enableScripts: true,
          retainContextWhenHidden: true,
        }
      );

      ModifierQuerierView.currentPanel = new ModifierQuerierView(panel, context);

      panel.webview.onDidReceiveMessage(async (message: string) => {
        if (message == "init_complete") {
			panel.webview.postMessage({type: "modifierList", data: await client.sendRequest("getAllModifier")});
        }
      });
    }
  }

  public dispose() {
    ModifierQuerierView.currentPanel = undefined;

    this._panel.dispose();

    while (this._disposables.length) {
      const disposable = this._disposables.pop();
      if (disposable) {
        disposable.dispose();
      }
    }
  }
}
