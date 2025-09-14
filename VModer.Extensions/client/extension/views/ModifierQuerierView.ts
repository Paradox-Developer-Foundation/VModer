import { type ExtensionContext, l10n, ViewColumn, type WebviewPanel, window } from "vscode";
import { LanguageClient } from "vscode-languageclient/node";
import type { ModifierQuerierViewI18n } from "../../src/types/ModifierQuerierViewI18";
import { BaseView } from "./BaseView";

export class ModifierQuerierView extends BaseView {
  public static currentPanel: ModifierQuerierView | undefined;
  private static readonly Id = "modifierQuerierView";

  private constructor(panel: WebviewPanel, context: ExtensionContext, client: LanguageClient) {
    super(panel, context, client, ModifierQuerierView.Id);
  }

  public static render(context: ExtensionContext, client: LanguageClient) {
    return super.renderOrReveal<ModifierQuerierView>(() => {
      const panel = window.createWebviewPanel(
        ModifierQuerierView.Id,
        l10n.t("ModifierQuerierView.Title"),
        ViewColumn.One,
        {
          enableScripts: true,
          retainContextWhenHidden: true,
        }
      );

      ModifierQuerierView.currentPanel = new ModifierQuerierView(panel, context, client);
      return ModifierQuerierView.currentPanel;
    });
  }

  protected getI18n(): ModifierQuerierViewI18n {
    const i18n: ModifierQuerierViewI18n = {
      searchPlaceholder: l10n.t("ModifierQuerierView.SearchPlaceholder"),
      searchButton: l10n.t("SearchButton"),
      categories: l10n.t("ModifierQuerierView.Categories"),
      name: l10n.t("ModifierQuerierView.Name"),
      localizedName: l10n.t("ModifierQuerierView.LocalizedName"),
    };
    return i18n;
  }

  protected override async onInitialized(panel: WebviewPanel, client: LanguageClient) {
    panel.webview.postMessage({
      type: "modifierList",
      data: await client.sendRequest("getAllModifier"),
    });
  }
}
