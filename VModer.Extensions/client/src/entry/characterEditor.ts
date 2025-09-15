import { createApp } from 'vue';
import CharacterEditorView from '../components/CharacterEditorView.vue';
import '@vscode-elements/elements';

const app = createApp(CharacterEditorView);
app.mount('#app');