import { createApp } from 'vue';
import ModifierQuerierView from '../components/ModifierQuerierView.vue';
import '@vscode-elements/elements';

const app = createApp(ModifierQuerierView);
app.mount('#app');
