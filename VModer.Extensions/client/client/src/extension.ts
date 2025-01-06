/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { workspace, ExtensionContext, ExtensionMode } from 'vscode';
import * as net from "net";
import * as fs from 'fs';
import * as os from 'os';
import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	StreamInfo,
	TransportKind,
	// TransportKind,
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

	// 控制语言客户端的选项
	const clientOptions: LanguageClientOptions = {
		// 为纯文本文档注册服务器
		documentSelector: [{ scheme: 'file', language: 'hoi4' }],
		synchronize: {
			fileEvents: [workspace.createFileSystemWatcher('**/*.txt')],
		},
		initializationOptions: {
			"GameRootFolderPath": workspace.getConfiguration().get<string>("VModer.GameRootPath")
		}
	};

	// 创建语言客户端并启动客户端。
	client = new LanguageClient(
		'vmoder',
		'VModer Server',
		serverOptions,
		clientOptions
	);
	// Start the client. This will also launch the server
	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}
