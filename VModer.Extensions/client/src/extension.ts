import { workspace, ExtensionContext, window, ExtensionMode, l10n } from 'vscode';
import * as net from "net";
import * as fs from 'fs';
import * as os from 'os';
import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	StreamInfo,
	TransportKind,
} from 'vscode-languageclient/node';
import * as path from 'path';

let client: LanguageClient;

export function activate(context: ExtensionContext) {
	let serverOptions: ServerOptions;

	if (context.extensionMode == ExtensionMode.Development) {
		const connectionInfo = {
			port: 1231
		};
		serverOptions = () => {
			// Connect to language server via socket
			const socket = net.connect(connectionInfo);
			const result: StreamInfo = {
				writer: socket,
				reader: socket as NodeJS.ReadableStream
			};
			socket.on("close", () => {
				console.log("client connect error!");
			});
			return Promise.resolve(result);
		};
	}
	else {
		const platform: string = os.platform();

		let command = "";
		switch (platform) {
			case "win32":
				command = path.join(
					context.extensionPath,
					'server',
					'win-x64',
					'VModer.Core.exe'
				);
				break;
			case "linux":
				command = path.join(
					context.extensionPath,
					'server',
					'linux-x64',
					'VModer.Core'
				);
				fs.chmodSync(command, '777');
				break;
			case "darwin":
				command = path.join(
					context.extensionPath,
					'server',
					'osx-x64',
					'VModer.Core'
				);
				break;
		}
		console.log("command: " + command);

		serverOptions = {
			run: { command: command, args: [], transport: TransportKind.stdio },
			debug: { command: command, args: [] }
		};
	}

	const config = workspace.getConfiguration();
	const gameRootFolderPath = config.get<string>("VModer.GameRootPath") || config.get<string>("cwtools.cache.hoi4");

	if (gameRootFolderPath === undefined || gameRootFolderPath === "") {
		window.showWarningMessage(l10n.t("SelectGameRootPath"), l10n.t("SelectFolder"))
			.then(() => {
				window.showOpenDialog({
					canSelectFiles: false,
					canSelectFolders: true,
					canSelectMany: false,
					openLabel: l10n.t("SelectFolder")
				}).then((uri) => {
					if (uri && uri[0]) {
						config.update("VModer.GameRootPath", uri[0].fsPath, true);
					}
				}).then(() => window.showInformationMessage(l10n.t("MustRestart")));
			});
	}
	
	// 控制语言客户端的选项
	const clientOptions: LanguageClientOptions = {
		documentSelector: [{ scheme: 'file', language: 'hoi4' }],
		synchronize: {
			fileEvents: [workspace.createFileSystemWatcher('**/*.txt')],
		},
		initializationOptions: {
			"GameRootFolderPath": gameRootFolderPath
		}
	};

	// 创建语言客户端并启动客户端。
	client = new LanguageClient(
		'vmoder',
		'VModer Server',
		serverOptions,
		clientOptions
	);

	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}
