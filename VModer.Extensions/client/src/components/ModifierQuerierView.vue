<template>
  <div style="display: flex; flex-direction: column; height: 100vh; overflow: hidden">
    <div style="display: flex; margin-top: 8px">
      <vscode-textfield
        @keyup.enter="filterList"
        v-model.trim="searchText"
        placeholder="本地化名称 | 名称"
      ></vscode-textfield>
      <vscode-button style="margin-left: 4px" @click="filterList">搜索</vscode-button>
    </div>
    <vscode-table
      style="flex-grow: 1; margin-top: 4px; min-height: 0"
      zebra
      bordered-rows
      resizable
    >
      <vscode-table-header slot="header">
        <vscode-table-header-cell>本地化名称</vscode-table-header-cell>
        <vscode-table-header-cell>名称</vscode-table-header-cell>
        <vscode-table-header-cell>类别</vscode-table-header-cell>
      </vscode-table-header>
      <vscode-table-body slot="body">
        <vscode-table-row v-for="(item, index) in modifierList" :key="index">
          <vscode-table-cell v-html="marked.parseInline(item.LocalizedName)"></vscode-table-cell>
          <vscode-table-cell>{{ item.Name }}</vscode-table-cell>
          <vscode-table-cell>{{ item.Categories.join(", ") }}</vscode-table-cell>
        </vscode-table-row>
      </vscode-table-body>
    </vscode-table>
  </div>
</template>

<script lang="ts" setup>
import { WebviewApi } from "@tomjs/vscode-webview";
import { onMounted, onUnmounted, ref } from "vue";
import type { ModifierDto } from "../dto/ModifierDto";
import { marked } from "marked";

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

const modifierList = ref<ModifierDto[]>([]);
let rawModifierList: ModifierDto[] = [];
const searchText = ref<string>("");
const vscode = new WebviewApi();

onMounted(() => {
  vscode.postMessage("init_complete");
});

vscode.on<ModifierDto[]>("modifierList", (data) => {
  rawModifierList = data;
  modifierList.value = data;
});

onUnmounted(() => {
  vscode.off("modifierList");
});

function filterList() {
  if (searchText.value === "") {
    modifierList.value = rawModifierList;
    return;
  }
  const search = searchText.value.toLowerCase();
  modifierList.value = rawModifierList.filter((item) => {
    return (
      item.LocalizedName.toLowerCase().includes(search) || item.Name.toLowerCase().includes(search)
    );
  });
}
</script>
