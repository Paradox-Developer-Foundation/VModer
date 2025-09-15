import { l10n, ViewColumn, window, type ExtensionContext, type WebviewPanel } from "vscode";
import type { LanguageClient } from "vscode-languageclient/node";
import { BaseView } from "./BaseView";

export class CharacterEditorView extends BaseView {
	protected getI18n(): object {
		return {};
	}
	public static currentPanel: CharacterEditorView | undefined;
	private static readonly Id = "characterEditorView";

	constructor(panel: WebviewPanel, context: ExtensionContext, client: LanguageClient) {
		super(panel, context, client, CharacterEditorView.Id);
	}

	public static render(context: ExtensionContext, client: LanguageClient) {
		return super.renderOrReveal<CharacterEditorView>(() => {
			const panel = window.createWebviewPanel(
				CharacterEditorView.Id,
				l10n.t("CharacterEditor.Title"),
				ViewColumn.One,
				{
					enableScripts: true,
					retainContextWhenHidden: true,
				}
			);
			CharacterEditorView.currentPanel = new CharacterEditorView(panel, context, client);
			return CharacterEditorView.currentPanel;
		});
	}
}
