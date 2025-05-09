import { marked } from "marked";

export function initMarked() {
  marked.use({
    extensions: [
      {
        name: "image",
        renderer(token) {
          const url = new URL(token.href);
          // TODO: 跨平台兼容性?
          return `<img src="https://file+.vscode-resource.vscode-cdn.net${url.pathname}" alt="${token.text}"/>`;
        },
      },
    ],
  });
}
