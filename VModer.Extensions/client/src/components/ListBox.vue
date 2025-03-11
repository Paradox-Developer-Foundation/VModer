<template>
  <div class="list-box" @click="handleItemClick">
    <div
      v-for="(item, index) in items"
      :key="index"
      class="list-item"
      :class="{ selected: index === selectedIndex }"
      :data-index="index"
      @mouseover="handleMouseOver($event, item)"
      @mouseout="handleMouseOut"
      @mousemove="handleMouseMove"
    >
      <slot name="item" :item="item" :index="index">
        <div>{{ String(item) }}</div>
      </slot>
    </div>

    <div
      class="tooltip"
      :class="{ show: isTooltipVisible }"
      ref="tooltip"
      :style="{
        display: isTooltipVisible ? 'block' : 'none',
        left: tooltipPosition.x + 'px',
        top: tooltipPosition.y + 'px',
      }"
    >
      <slot name="tooltip" :item="currentTooltipItem" v-if="currentTooltipItem">
        <div>{{ String(currentTooltipItem) }}</div>
      </slot>
    </div>
  </div>
</template>

<script setup lang="ts" generic="T">
import { ref, watch } from "vue";

export interface ListBoxProps<T> {
  items?: T[];
  showTooltip?: boolean;
  tooltipDelay?: number;
}

// 定义属性
const props = withDefaults(defineProps<ListBoxProps<T>>(), {
  items: () => [],
  showTooltip: true,
  tooltipDelay: 500,
});

// 定义事件
const emit = defineEmits<{
  (event: "selection-changed", item: T | null): void;
  (event: "update:items", items: T[]): void;
}>();

// 响应式状态
const items = ref<T[]>(props.items || []);
const selectedIndex = ref(-1);
const tooltip = ref<HTMLElement | null>(null);
const isTooltipVisible = ref(false);
const currentTooltipItem = ref<T | null>(null); // 新增：当前tooltip显示的项
const tooltipPosition = ref({ x: 0, y: 0 });
let tooltipTimer: number | null = null;

// 监听外部更新项目列表
watch(
  () => props.items,
  (newItems) => {
    items.value = newItems || [];
  },
  { deep: true }
);

// 公开的方法
function setItems(newItems: T[]) {
  items.value = newItems;
  emit("update:items", newItems);
}

function getSelectedItem(): T | null {
  return selectedIndex.value >= 0 ? (items.value[selectedIndex.value] as T) : null;
}

// 内部方法
function handleItemClick(event: MouseEvent) {
  const target = event.target as HTMLElement;
  const listItem = target.closest(".list-item") as HTMLElement | null;
  if (!listItem) return;

  const index = parseInt(listItem.dataset.index || "-1", 10);
  if (index !== selectedIndex.value) {
    selectedIndex.value = index;
    emit("selection-changed", getSelectedItem());
  }
}

function handleMouseOver(event: MouseEvent, item: T) {
  if (!props.showTooltip) return;

  clearTimeout(tooltipTimer as number);
  tooltipTimer = window.setTimeout(() => {
    currentTooltipItem.value = item;
    positionTooltip(event);
    isTooltipVisible.value = true;
  }, props.tooltipDelay);
}

function handleMouseOut() {
  clearTimeout(tooltipTimer as number);
  isTooltipVisible.value = false;
  currentTooltipItem.value = null;
}

function handleMouseMove(event: MouseEvent) {
  if (isTooltipVisible.value) {
    positionTooltip(event);
  }
}

function setSelectedIndex(index: number) {
  if (index < 0 || index >= items.value.length) {
    return;
  }

  selectedIndex.value = index;
}

function positionTooltip(event: MouseEvent) {
  const offset = 10;
  const scrollX = window.scrollX || document.documentElement.scrollLeft;
  const scrollY = window.scrollY || document.documentElement.scrollTop;

  let x = event.pageX + offset;
  let y = event.pageY + offset;

  const tooltipElement = tooltip.value;
  if (!tooltipElement) return;

  const tooltipRect = tooltipElement.getBoundingClientRect();
  const windowWidth = window.innerWidth;
  const windowHeight = window.innerHeight;

  if (x + tooltipRect.width > windowWidth + scrollX) {
    x = windowWidth + scrollX - tooltipRect.width - offset;
  }

  if (y + tooltipRect.height > windowHeight + scrollY) {
    y = windowHeight + scrollY - tooltipRect.height - offset;
  }

  tooltipPosition.value = { x, y };
}

// 暴露公共方法供父组件使用
defineExpose({
  setItems,
  getSelectedItem,
  setSelectedIndex,
});
</script>

<!--suppress CssUnresolvedCustomProperty -->
<style scoped>
/* 列表容器样式 */
.list-box {
  height: auto;
  border: 1px solid var(--vscode-editorWidget-border);
  border-radius: 3px;
  overflow-y: auto;
  font-family: var(--vscode-editor-font-family);
}

/* 列表项基础样式 */
.list-item {
  padding: 8px 12px;
  cursor: default;
  transition: background 0.2s;
  user-select: none;
  background: var(--vscode-editorActionList-background);
}

/* 悬停效果 */
.list-item:hover {
  background: var(--vscode-list-hoverBackground);
  color: var(--vscode-list-hoverForeground);
}

/* 选中状态 */
.list-item.selected {
  background: var(--vscode-list-activeSelectionBackground);
  color: var(--vscode-list-activeSelectionForeground);
}

/* 自定义Tooltip样式 */
.tooltip {
  position: absolute;
  background: var(--vscode-panel-background);
  color: var(--vscode-foreground);
  padding: 6px 10px;
  border-radius: 4px;
  font-size: 14px;
  z-index: 1000;
  max-width: 500px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  pointer-events: none;
  transition: opacity 0.2s;
  opacity: 0;
}

.tooltip.show {
  opacity: 1;
}
</style>
