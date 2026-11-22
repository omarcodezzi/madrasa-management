import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { filter, map } from 'rxjs';
import { errorMessage } from '../core/api';
import { AuthService, P } from '../core/auth/auth.service';
import { DigitsPipe } from '../core/format';
import { PreferencesService } from '../core/preferences.service';
import { SessionService } from '../core/session.service';
import { ToastService } from '../core/toast.service';
import { Avatar, Button, FormField, Icon, IconButton, Modal } from '../shared/ui';
import { CommandPalette } from './command-palette';
import { ALL_NAV, BOTTOM_NAV, NAV } from './nav';
import { TrPipe } from '../core/tr';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, RouterOutlet, RouterLink, RouterLinkActive, FormsModule, TranslocoPipe, DigitsPipe, Icon, IconButton, Avatar, Button, Modal, FormField, CommandPalette],
  host: {
    class: 'block min-h-dvh',
    '(document:keydown)': 'onKey($event)',
  },
  templateUrl: './shell.html',
})
export class Shell {
  protected auth = inject(AuthService);
  protected prefs = inject(PreferencesService);
  protected session = inject(SessionService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toast = inject(ToastService);
  private t = inject(TranslocoService);

  protected collapsed = signal(localStorage.getItem('mms.sidebar') === 'collapsed');
  protected drawer = signal(false);
  protected palette = signal(false);
  protected menu = signal<'profile' | 'bell' | null>(null);
  protected isMac = /Mac|iPhone|iPad/.test(navigator.platform);

  protected groups = computed(() =>
    NAV.map((g) => ({
      ...g,
      // Staff work from the dashboard; the portal is only for students and guardians
      items: g.items.filter((i) => this.auth.can(i.perm) && (i.key !== 'portal' || !this.auth.can(P.dashboard))),
    })).filter((g) => g.items.length),
  );
  protected bottom = computed(() => BOTTOM_NAV.map((k) => ALL_NAV.find((n) => n.key === k)!).filter((n) => this.auth.can(n.perm)));

  /** Current page from route data { nav: 'students' } */
  private navKey = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => {
        let r = this.route.snapshot;
        while (r.firstChild) r = r.firstChild;
        return (r.data['nav'] as string | undefined) ?? null;
      }),
    ),
    { initialValue: null },
  );
  protected current = computed(() => ALL_NAV.find((n) => n.key === this.navKey()) ?? null);
  protected currentGroup = computed(() => NAV.find((g) => g.items.some((i) => i.key === this.navKey()))?.key ?? null);
  protected roleLabel = computed(() => {
    this.lang(); this.loaded();
    return (this.auth.user()?.roles ?? []).map((r) => this.t.translate('roles.' + r)).join(', ');
  });
  private lang = toSignal(this.t.langChanges$, { initialValue: this.t.getActiveLang() });
  /** translate() is synchronous: re-run once the language file has arrived. */
  private loaded = toSignal(this.t.events$.pipe(filter((e) => e.type === 'translationLoadSuccess')), { initialValue: null });

  protected toggleCollapse() {
    this.collapsed.update((c) => !c);
    try { localStorage.setItem('mms.sidebar', this.collapsed() ? 'collapsed' : ''); } catch { /* ignore */ }
  }
  protected themeIcon = computed(() => (this.prefs.theme() === 'dark' ? 'moon' : this.prefs.theme() === 'light' ? 'sun' : 'monitor'));
  protected toggleLang() { this.prefs.lang.set(this.prefs.lang() === 'bn' ? 'en' : 'bn'); }

  protected onKey(e: KeyboardEvent) {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); this.palette.set(true); }
    if (e.key === 'Escape') { this.menu.set(null); this.drawer.set(false); }
  }
  protected closeMenus() { this.menu.set(null); }
  protected navClick() { this.drawer.set(false); }

  // change password
  protected pwOpen = signal(false);
  protected pwCur = signal('');
  protected pwNew = signal('');
  protected pwBusy = signal(false);
  protected pwErr = signal<string | null>(null);
  protected openPw() { this.menu.set(null); this.pwCur.set(''); this.pwNew.set(''); this.pwErr.set(null); this.pwOpen.set(true); }
  protected async savePw() {
    if (this.pwNew().length < 8) { this.pwErr.set(this.t.translate('auth.newPasswordHint')); return; }
    this.pwBusy.set(true);
    try {
      await this.auth.changePassword(this.pwCur(), this.pwNew());
      this.pwOpen.set(false);
      this.toast.success(this.t.translate('auth.passwordChanged'));
    } catch (e) { this.pwErr.set(errorMessage(e)); }
    finally { this.pwBusy.set(false); }
  }
}
