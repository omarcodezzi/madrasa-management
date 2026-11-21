import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PreferencesService } from './core/preferences.service';
import { ConfirmHost, Toaster } from './shared/ui';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, Toaster, ConfirmHost],
  template: `<router-outlet /><m-toaster /><m-confirm-host />`,
})
export class App {
  // Instantiate early so theme/language apply on start
  protected prefs = inject(PreferencesService);
}
