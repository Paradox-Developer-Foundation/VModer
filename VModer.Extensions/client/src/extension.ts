import { workspace, ExtensionContext, window, ExtensionMode, l10n, StatusBarAlignment, StatusBarItem, commands, Uri } from 'vscode';
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

export async function activate(context: ExtensionContext) {

	const statusBarItem = window.createStatusBarItem(StatusBarAlignment.Right, 100000);
	const openLogs = commands.registerCommand('vmoder.openLogs', () => {
		const dirPath = path.dirname(command);
		commands.executeCommand('revealFileInOS', Uri.file(path.join(dirPath, "Logs")));
	});
	context.subscriptions.push(openLogs, statusBarItem);

	let serverOptions: ServerOptions;
	let command = "";
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
		await window.showWarningMessage(l10n.t("SelectGameRootPath"), l10n.t("SelectFolder"));
		const uri = await window.showOpenDialog({
			canSelectFiles: false,
			canSelectFolders: true,
			canSelectMany: false,
			openLabel: l10n.t("SelectFolder")
		});
		if (uri && uri[0]) {
			config.update("VModer.GameRootPath", uri[0].fsPath, true);
		}
		await window.showInformationMessage(l10n.t("MustRestart"));
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
	statusBarItem.show();
	updateStatusBarItem(statusBarItem, client);
	context.subscriptions.push(client.onDidChangeState(() => updateStatusBarItem(statusBarItem, client)));
	setInterval(() => {
		updateStatusBarServerInfo(statusBarItem, client);
	}, config.get<number>("VModer.RamQueryIntervalTime") || 1500);
}

async function updateStatusBarItem(statusBarItem: StatusBarItem, client: LanguageClient): Promise<void> {
	if (client.isRunning()) {
		statusBarItem.text = "$(notebook-state-success) VModer";
		statusBarItem.tooltip = "VModer is running";
	}
	else {
		statusBarItem.text = "$(extensions-warning-message) VModer";
		statusBarItem.tooltip = "VModer is stopped";
	}
}

async function updateStatusBarServerInfo(statusBarItem: StatusBarItem, client: LanguageClient) {
	if (client.isRunning()) {
		const info = await client.sendRequest("getRuntimeInfo");
		const size: number = info["memoryUsedBytes"];
		statusBarItem.text = "$(notebook-state-success) VModer RAM " + formatBytes(size);
	}
}

function formatBytes(bytes: number): string {
	if (bytes === 0) {
		return '0 Bytes';
	}
	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	const size = (bytes / Math.pow(k, i)).toFixed(2);
	return size + ' ' + sizes[i];
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}
