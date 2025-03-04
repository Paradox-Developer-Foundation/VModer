import {
    commands,
    type ExtensionContext,
    ExtensionMode,
    l10n,
    StatusBarAlignment,
    type StatusBarItem,
    Uri,
    window,
    workspace,
} from "vscode";
import * as net from "net";
import * as fs from "fs";
import * as os from "os";
import {
    LanguageClient,
    type LanguageClientOptions,
    type ServerOptions,
    type StreamInfo,
    TransportKind,
} from "vscode-languageclient/node";
import * as path from "path";
import { TraitView } from "./views/TraitsView";

let client: LanguageClient;
let analyzeAllFilesEnd = false;

export async function activate(context: ExtensionContext) {
    const statusBarItem = window.createStatusBarItem(StatusBarAlignment.Right, 100000);
    const openLogs = commands.registerCommand("vmoder.openLogs", () => {
        const dirPath = path.dirname(command);
        commands.executeCommand("revealFileInOS", Uri.file(path.join(dirPath, "Logs")));
    });

    context.subscriptions.push(openLogs, statusBarItem);

    let serverOptions: ServerOptions;
    let command = "";
    if (context.extensionMode == ExtensionMode.Development) {
        const connectionInfo = {
            port: 1231,
        };
        serverOptions = () => {
            // Connect to language server via socket
            const socket = net.connect(connectionInfo);
            const result: StreamInfo = {
                writer: socket,
                reader: socket as NodeJS.ReadableStream,
            };
            socket.on("close", () => {
                console.log("client connect error!");
            });
            return Promise.resolve(result);
        };
    } else {
        const platform: string = os.platform();

        switch (platform) {
            case "win32":
                command = path.join(context.extensionPath, "server", "win-x64", "VModer.Core.exe");
                break;
            case "linux":
                command = path.join(context.extensionPath, "server", "linux-x64", "VModer.Core");
                fs.chmodSync(command, "777");
                break;
            case "darwin":
                command = path.join(context.extensionPath, "server", "osx-x64", "VModer.Core");
                break;
        }
        console.log("command: " + command);

        serverOptions = {
            run: { command: command, args: [], transport: TransportKind.stdio },
            debug: { command: command, args: [] },
        };
    }

    const config = workspace.getConfiguration();
    const gameRootFolderPath =
        config.get<string>("VModer.GameRootPath") || config.get<string>("cwtools.cache.hoi4");

    if (gameRootFolderPath === undefined || gameRootFolderPath === "") {
        await window.showWarningMessage(l10n.t("SelectGameRootPath"), l10n.t("SelectFolder"));
        const uri = await window.showOpenDialog({
            canSelectFiles: false,
            canSelectFolders: true,
            canSelectMany: false,
            openLabel: l10n.t("SelectFolder"),
        });
        if (uri && uri[0]) {
            config.update("VModer.GameRootPath", uri[0].fsPath, true);
        }
        await window.showInformationMessage(l10n.t("MustRestart"));
    }

    // 控制语言客户端的选项
    const clientOptions: LanguageClientOptions = {
        documentSelector: [{ scheme: "file", language: "hoi4" }],
        initializationOptions: {
            GameRootFolderPath: gameRootFolderPath,
            Blacklist: config.get<string[]>("VModer.Blacklist") || [],
            ParseFileMaxSize: config.get<number>("VModer.ParseFileMaxSize") || 2,
            GameLanguage: config.get<string>("VModer.GameLocalizedLanguage") || "default",
            ExtensionPath: context.extensionPath,
        },
    };

    // 创建语言客户端并启动客户端。
    client = new LanguageClient("vmoder", "VModer Server", serverOptions, clientOptions);

    client.onNotification("analyzeAllFilesStart", () => {
        statusBarItem.text = "$(extensions-sync-enabled) VModer Analyzing";
    });

    client.onNotification("analyzeAllFilesEnd", () => {
        analyzeAllFilesEnd = true;
    });

    const isOpenWorkspace = workspace.workspaceFolders !== undefined;
    if (isOpenWorkspace) {
        await client.start();
        setInterval(() => {
            updateStatusBarServerInfo(statusBarItem, client);
        }, config.get<number>("VModer.RamQueryIntervalTime") || 1500);

        client.info("VModer 服务端启动中...");
    } else {
        client.info("未打开工作区, 无法启动 VModer 服务端");
    }
    statusBarItem.show();
    updateStatusBarItem(statusBarItem, client);
    const clearImageCache = commands.registerCommand("vmoder.clearImageCache", () => {
        client.sendNotification("clearImageCache");
    });

    const openTraitsView = commands.registerCommand("vmoder.openTraitsView", async () => {
        if (!client.isRunning()) {
            return;
        }

        TraitView.render(context, client);
    });

    context.subscriptions.push(
        client.onDidChangeState(() => updateStatusBarItem(statusBarItem, client)),
        clearImageCache,
        openTraitsView
    );
}

async function updateStatusBarItem(
    statusBarItem: StatusBarItem,
    client: LanguageClient
): Promise<void> {
    if (client.isRunning()) {
        statusBarItem.text = "$(notebook-state-success) VModer";
        statusBarItem.tooltip = "VModer is running";
    } else {
        statusBarItem.text = "$(extensions-warning-message) VModer";
        statusBarItem.tooltip = "VModer is stopped";
    }
}

async function updateStatusBarServerInfo(statusBarItem: StatusBarItem, client: LanguageClient) {
    if (client.isRunning() && analyzeAllFilesEnd) {
        const info: { memoryUsedBytes: number } = await client.sendRequest("getRuntimeInfo");
        statusBarItem.text =
            "$(notebook-state-success) VModer RAM " + formatBytes(info.memoryUsedBytes);
    }
}

function formatBytes(bytes: number): string {
    if (!Number.isFinite(bytes) || bytes < 0) {
        return "0 Bytes";
    }
    if (bytes === 0) {
        return "0 Bytes";
    }
    const k = 1024;
    const sizes = ["Bytes", "KB", "MB", "GB", "TB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    const size = (bytes / Math.pow(k, i)).toFixed(2);
    return size + " " + sizes[i];
}

export function deactivate(): Thenable<void> | undefined {
    if (!client) {
        return undefined;
    }
    return client.stop();
}
