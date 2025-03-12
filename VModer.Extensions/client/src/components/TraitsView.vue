<template>
  <div style="margin: 16px">
    <div style="display: flex; column-gap: 8px; margin-bottom: 8px">
      <vscode-textfield
        :placeholder="i18n.search"
        @keyup.enter="searchTrait"
        v-model.trim="searchValue"
      >
      </vscode-textfield>
      <vscode-button @click="searchTrait">{{ i18n.searchButton }}</vscode-button>
      <label for="typeSelect">{{ i18n.origin }}</label>
      <select id="typeSelect" @change="searchTrait" v-model="selectedOrigin">
        <option value="0">{{ i18n.all }}</option>
        <option value="1">{{ i18n.gameOnly }}</option>
        <option value="2">{{ i18n.modOnly }}</option>
      </select>

      <label for="traitTypeSelection">{{ i18n.traitType }}</label>
      <vscode-multi-select id="traitTypeSelection" ref="traitTypeSelection" @change="searchTrait">
        <vscode-option v-for="type in traitTypes" :value="type" selected="ture">{{
          TraitType[type]
        }}</vscode-option>
      </vscode-multi-select>

      <label>Count: {{ viewData.length }} </label>
    </div>

    <ListBox ref="listBox" :items="viewData">
      <template #tooltip="{ item }">
        <div>
          <span>id: {{ item.Name }}</span>
          <br />
          <span>origin: {{ FileOrigin[item.FileOrigin] }}</span>
          <pre style="margin: 0; font-family: inherit">{{ item.Modifiers }}</pre>

          <br v-if="item.Description" />
          <span v-if="item.Description">{{ item.Description }}</span>
        </div>
      </template>

      <template #item="{ item, index }">
        <div style="padding: 8px 12px;" @click.right="(event) => openMenu(event, item, index)">
          {{ item.LocalizedName }}
        </div>
      </template>
    </ListBox>

    <vscode-context-menu
      ref="contextMenu"
      style="position: fixed"
    ></vscode-context-menu>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, useTemplateRef } from "vue";
import { FileOrigin, TraitType, type TraitDto, getTraitTypeValues, hasFlag } from "../dto/TraitDto";
import { WebviewApi } from "@tomjs/vscode-webview";
import ListBox from "./ListBox.vue";
import type { TraitViewI18n } from "../types/TraitViewI18n";
import type { VscodeContextMenu, VscodeMultiSelect } from "@vscode-elements/elements";

const AllOrgin = "0";
const traitTypes: TraitType[] = getTraitTypeValues();
const vscode = new WebviewApi();
const i18n = ref<TraitViewI18n>({
  search: "Search",
  searchButton: "Search",
  origin: "Origin:",
  all: "All",
  gameOnly: "Game Only",
  modOnly: "Mod Only",
  traitType: "Trait Type:",
  copyTraitId: "Copy Trait ID",
});

const searchValue = ref("");
const selectedOrigin = ref(AllOrgin);
const traitTypeSelection = ref<VscodeMultiSelect | null>(null);
const contextMenu = ref<VscodeContextMenu | null>(null);

const listBox = useTemplateRef('listBox');

const viewData = ref<TraitDto[]>([]);
let rawTraits: TraitDto[] = [];
let currentItem: TraitDto | null = null;

onMounted(() => {
  vscode.postMessage("init_complete");
  contextMenu.value!.data.push({
    label: "复制特质ID",
    value: "copyTraitId",
  });
  contextMenu.value!.addEventListener("vsc-context-menu-select", (event) => {
    if (event.detail.value === "copyTraitId" && currentItem) {
      vscode.postMessage({ type: "copyToClipboard", data: currentItem.Name });
    }
  });
});

const documentClickListener = ref<((e: MouseEvent) => void) | null>(null);
const closeContextMenu = () => {
  if (contextMenu.value) {
    contextMenu.value.show = false;

    if (documentClickListener.value) {
      document.removeEventListener("click", documentClickListener.value);
      documentClickListener.value = null;
    }
  }
};

function openMenu(event: MouseEvent, item: TraitDto, itemIndex: number) {
  event.preventDefault();
  if (!contextMenu.value) {
    return;
  }

  listBox.value?.setSelectedIndex(itemIndex);
  closeContextMenu();

  currentItem = item;

  contextMenu.value.style.left = `${event.clientX}px`;
  contextMenu.value.style.top = `${event.clientY}px`;
  contextMenu.value.show = true;
  
  const newClickListener = (_: MouseEvent) => {
    closeContextMenu();
  };
  
  documentClickListener.value = newClickListener;

  setTimeout(() => {
    document.addEventListener("click", newClickListener);
  }, 10);
}

vscode.on<TraitDto[]>("traits", (receivedTraits) => {
  rawTraits = receivedTraits;

  viewData.value = rawTraits;
});

vscode.on<TraitViewI18n>("i18n", (i18nData) => {
  i18n.value = i18nData;
});

function searchTrait() {
  const selectedTraitType = traitTypeSelection.value?.value ?? [];

  if (
    searchValue.value === "" &&
    selectedOrigin.value === AllOrgin &&
    selectedTraitType.length === traitTypes.length
  ) {
    viewData.value = rawTraits;
    return;
  }

  const search = searchValue.value.toLowerCase();
  const includesSearchValue = (item: TraitDto) =>
    item.LocalizedName.toLowerCase().includes(search) || item.Name.toLowerCase().includes(search);

  const isTargetFileOrigin = (item: TraitDto) =>
    selectedOrigin.value === AllOrgin || item.FileOrigin.toString() === selectedOrigin.value;

  let traitTypeFlags = selectedTraitType.map<TraitType>((item) => TraitType[item]);

  viewData.value = rawTraits.filter(
    (item) =>
      isTargetFileOrigin(item) &&
      includesSearchValue(item) &&
      traitTypeFlags.some((traitFlag) => hasFlag(item.Type, traitFlag))
  );
}

onUnmounted(() => {
  vscode.off("traits");
  vscode.off("i18n");
  closeContextMenu();
});
</script>

<!--suppress CssUnresolvedCustomProperty -->
<style>
select {
  background-color: var(--vscode-editor-background);
  border: var(--vscode-foreground) 1px solid;
  color: var(--vscode-foreground);
  font-family: var(--vscode-font-family, sans-serif);
}

label {
  font-size: medium;
}
</style>
