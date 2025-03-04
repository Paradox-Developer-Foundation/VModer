import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import vscode from "@tomjs/vite-plugin-vscode";
import { resolve } from "path";

export default defineConfig({
    plugins: [
        vue({
            template: {
                compilerOptions: {
                    isCustomElement: (tag: string) => tag.startsWith("vscode-"),
                },
            },
        }),
        vscode({ extension: { entry: "extension/extension.ts" } })
    ],
    build: {
        rollupOptions: {
            input: {
                traitsView: resolve(__dirname, 'src','traitsView.html')
            }
        },
    }
});
