import { type ExtensionContext, l10n, ViewColumn, type WebviewPanel, window } from "vscode";
import { Disposable } from "vscode-languageclient";
import { WebviewHelpers } from "./WebviewHelpers";
import { LanguageClient } from "vscode-languageclient/node";
import type { ModifierQuerierViewI18n } from "../../src/types/ModifierQuerierViewI18";

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
        l10n.t("ModifierQuerierView.Title"),
        ViewColumn.One,
        {
          enableScripts: true,
          retainContextWhenHidden: true,
        }
      );

      const i18n: ModifierQuerierViewI18n = {
        searchPlaceholder: l10n.t("ModifierQuerierView.SearchPlaceholder"),
        searchButton: l10n.t("SearchButton"),
        categories: l10n.t("ModifierQuerierView.Categories"),
        name: l10n.t("ModifierQuerierView.Name"),
        localizedName: l10n.t("ModifierQuerierView.LocalizedName"),
      };

      ModifierQuerierView.currentPanel = new ModifierQuerierView(panel, context);

      panel.webview.onDidReceiveMessage(async (message: string) => {
        if (message == "init_complete") {
          panel.webview.postMessage({
            type: "modifierList",
            data: await client.sendRequest("getAllModifier"),
          });

          panel.webview.postMessage({
            type: "i18n",
            data: i18n,
          });
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
