<template>
	<div style="display: flex; flex-direction: row; gap: 8px; margin: 12px;">
		<div style="display: flex; flex-direction: column; gap: 4px;">
			<vscode-label>名称</vscode-label>
			<vscode-textfield></vscode-textfield>
			<vscode-label>本地化名称</vscode-label>
			<vscode-textfield></vscode-textfield>
			<vscode-label>图像</vscode-label>
			<vscode-textfield placeholder="需在interface/*.gfx中定义"></vscode-textfield>
			<vscode-label>类别</vscode-label>
			<vscode-single-select placeholder="类别" @change="handleTypeChange">
				<vscode-option value="corps_commander">将军</vscode-option>
				<vscode-option value="field_marshal">陆军元帅</vscode-option>
				<vscode-option :value="navyLeader">海军将领</vscode-option>
			</vscode-single-select>
		</div>
		<div style="display: flex; flex-direction: column; gap: 4px;">
			<vscode-label>攻击</vscode-label>
			<vscode-textfield type="number" min="1" max="10">
			</vscode-textfield>
			<vscode-label>防御</vscode-label>
			<vscode-textfield type="number" min="1" max="10">
			</vscode-textfield>
			<div v-show="selectedType !== navyLeader">
				<vscode-label>计划</vscode-label>
				<vscode-textfield type="number" min="1" max="10">
				</vscode-textfield>
			</div>
			<div v-show="selectedType !== navyLeader">
				<vscode-label>后勤</vscode-label>
				<vscode-textfield type="number" min="1" max="10"></vscode-textfield>
			</div>
			<div v-show="selectedType === navyLeader">
				<vscode-label>机动</vscode-label>
				<vscode-textfield type="number" min="1" max="10"></vscode-textfield>
			</div>
			<div v-show="selectedType === navyLeader">
				<vscode-label>协调</vscode-label>
				<vscode-textfield type="number" min="1" max="10"></vscode-textfield>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup>
// import { WebviewApi } from "@tomjs/vscode-webview";
// import type { VscodeSingleSelect } from "@vscode-elements/elements";
import { ref } from "vue";

const navyLeader = "navy_leader";
const selectedType = ref<string>("");
// const vscode = new WebviewApi();

const handleTypeChange = (event: Event) => {
	const target = event.target as HTMLSelectElement;
	selectedType.value = target.value;
};
</script>