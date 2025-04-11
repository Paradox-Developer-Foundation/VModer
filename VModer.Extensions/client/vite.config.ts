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
    vscode({
      extension: { entry: "extension/extension.ts" },
      webview: {
        csp: `<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src {{cspSource}}; style-src {{cspSource}} 'unsafe-inline'; script-src 'nonce-{{nonce}}' 'unsafe-eval';">`,
      },
    }),
  ],
  build: {
    rollupOptions: {
      input: {
        traitsView: resolve(__dirname, "src", "html", "traitsView.html"),
        modifierQuerierView: resolve(__dirname, "src", "html", "modifierQuerierView.html")
      },
    },
  },
});
