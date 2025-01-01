/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

// import * as path from 'path';
import { workspace, ExtensionContext } from 'vscode';
import * as net from "net";
import {
	LanguageClient,
	LanguageClientOptions,
	// ServerOptions,
	StreamInfo,
	// TransportKind,
} from 'vscode-languageclient/node';

let client: LanguageClient;

export function activate(_context: ExtensionContext) {
	// The server is implemented in node
	// const serverModule = context.asAbsolutePath(
	// 	path.join('server', 'out', 'server.js')
	// );

	// The server is a started as a separate app and listens on port 5007
	const connectionInfo = {
		port: 1231
	};
	const serverOptions = () => {
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
	// If the extension is launched in debug mode then the debug server options are used
	// Otherwise the run options are used
	// const serverOptions: ServerOptions = {
	// 	run: {
	// 		command: 'dotnet',
	// 		args: [
	// 			'D:\\Code\\Project\\Rid_C#\\VModer\\VModer.Client\\bin\\Debug\\net9.0\\VModer.Client.dll',
	// 		],
	// 		transport: {kind: TransportKind.socket, port: 1231},
	// 	},
	// 	debug: {
	// 		command: 'dotnet',
	// 		args: [
	// 			'D:\\Code\\Project\\Rid_C#\\VModer\\VModer.Client\\bin\\Debug\\net9.0\\VModer.Client.dll',
	// 		],
	// 		transport: {kind: TransportKind.socket, port: 1231},
	// 	},
	// };

	// 控制语言客户端的选项
	const clientOptions: LanguageClientOptions = {
		// 为纯文本文档注册服务器
		documentSelector: [{ scheme: 'file', language: 'plaintext' }, { scheme: 'file', language: 'hoi4' }],
		synchronize: {
			// Notify the server about file changes to '.clientrc files contained in the workspace
			fileEvents: [workspace.createFileSystemWatcher('**/.clientrc'), workspace.createFileSystemWatcher('**/*.txt')],
		},
		initializationOptions: {  }
	};

	// Create the language client and start the client.
	// 创建语言客户端并启动客户端。
	client = new LanguageClient(
		'languageServerExample',
		'Language Server Example',
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
