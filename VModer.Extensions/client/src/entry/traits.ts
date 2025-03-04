import { createApp } from 'vue';
import TraitsView from '../components/TraitsView.vue';
import '@vscode-elements/elements';

// 创建并挂载Vue应用
const app = createApp(TraitsView);
app.mount('#app');
