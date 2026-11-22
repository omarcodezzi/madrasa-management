import { bootstrapApplication } from '@angular/platform-browser';
import { loadTranslations } from './app/core/tr';

// English text must be in place before any module builds its labels (routes, menus,
// column titles), so it loads first and the app is imported after it.
loadTranslations()
  .then(async () => {
    const [{ appConfig }, { App }] = await Promise.all([import('./app/app.config'), import('./app/app')]);
    await bootstrapApplication(App, appConfig);
  })
  .catch((err) => console.error(err));
