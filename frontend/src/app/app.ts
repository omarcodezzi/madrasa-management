import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PreferencesService } from './core/preferences.service';
import { ConfirmHost, Toaster } from './shared/ui';
import { PrintHost } from './shared/print/print-host';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, Toaster, ConfirmHost, PrintHost],
  template: `<router-outlet /><m-toaster /><m-confirm-host /><app-print-host />`,
})
export class App {
  // Instantiate early so theme/language apply on start
  protected prefs = inject(PreferencesService);
}
