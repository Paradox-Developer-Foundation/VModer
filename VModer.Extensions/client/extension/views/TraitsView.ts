import { env, ExtensionContext, l10n, ViewColumn, WebviewPanel, window } from "vscode";
import { Disposable } from "vscode-languageclient";
import { WebviewHelpers } from "./WebviewHelpers";
import { LanguageClient } from "vscode-languageclient/node";

export class TraitView {
  public static currentPanel: TraitView | undefined;
  private readonly _panel: WebviewPanel;
  private _disposables: Disposable[] = [];

  private constructor(panel: WebviewPanel, context: ExtensionContext) {
    this._panel = panel;

    this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
    this._panel.webview.html = WebviewHelpers.getHtml(this._panel.webview, context, "traitsView");
  }

  public static render(context: ExtensionContext, client: LanguageClient) {
    if (TraitView.currentPanel) {
      TraitView.currentPanel._panel.reveal(ViewColumn.One);
    } else {
      const panel = window.createWebviewPanel("traitsView", l10n.t("TraitsView.Title"), ViewColumn.One, {
        enableScripts: true,
        retainContextWhenHidden: true,
      });

      TraitView.currentPanel = new TraitView(panel, context);

      const i18n: TraitViewI18n = {
        search: l10n.t("TraitsView.Search"),
        searchButton: l10n.t("TraitsView.SearchButton"),
        origin: l10n.t("TraitsView.Origin"),
        all: l10n.t("TraitsView.All"),
        gameOnly: l10n.t("TraitsView.GameOnly"),
        modOnly: l10n.t("TraitsView.ModOnly"),
        traitType: l10n.t("TraitsView.TraitType"),
        copyTraitId: l10n.t("TraitsView.CopyTraitId"),
      };

      panel.webview.onDidReceiveMessage(
        async (message) => {
          if (message == "init_complete") {
            panel.webview.postMessage({ type: "i18n", data: i18n });
            panel.webview.postMessage({
              type: "traits",
              data: await client.sendRequest("getAllTrait"),
            });
          }
        },
        null,
        TraitView.currentPanel._disposables
      );

      panel.webview.onDidReceiveMessage(
        async (message) => {
          if (message.type === "copyToClipboard") {
            try {
              await env.clipboard.writeText(message.data);
              window.showInformationMessage("已复制内容到剪贴板");
            } catch (err) {
              console.error("无法复制文本:", err);
              window.showErrorMessage("复制失败");
            }
          }
        },
        null,
        TraitView.currentPanel._disposables
      );
    }
  }

  /**
   * Cleans up and disposes of webview resources when the webview panel is closed.
   */
  public dispose() {
    TraitView.currentPanel = undefined;

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

export interface TraitViewI18n {
  search: string;
  searchButton: string;
  origin: string;
  all: string;
  gameOnly: string;
  modOnly: string;
  traitType: string;
  copyTraitId: string;
}
