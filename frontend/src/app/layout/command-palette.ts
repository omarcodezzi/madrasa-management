import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, model, signal, untracked, viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { API, PagedResult } from '../core/api';
import { AuthService, P } from '../core/auth/auth.service';
import { DigitsPipe, toEnDigits, fmt } from '../core/format';
import { Avatar, Icon, IconName } from '../shared/ui';
import { ALL_NAV } from './nav';
import { TrPipe, tr } from '../core/tr';

interface StudentHit { id: number; studentCode: string; name: string; className?: string; roll?: number; phone?: string; }
interface Item { kind: 'page' | 'action' | 'student'; label: string; sub?: string; icon: IconName; url: string; student?: StudentHit; }

/** Ctrl/⌘+K: jump to any page, action or student (search by name, ID, roll, phone, guardian phone — spec 8.2). */
@Component({
  selector: 'app-command-palette',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, TranslocoPipe, DigitsPipe, Icon, Avatar],
  template: `
    <dialog #dlg (close)="open.set(false)" (click)="$event.target === dlg && open.set(false)" [attr.aria-label]="'খুঁজুন' | tr"
      class="mx-auto mt-[10vh] w-[min(640px,calc(100vw-24px))] p-0 rounded-2xl border border-line bg-card text-ink shadow-2xl backdrop:bg-black/40 backdrop:backdrop-blur-[2px]">
      <div class="flex items-center gap-3 px-4 border-b border-line">
        <m-icon name="search" class="text-mut" />
        <input #inp class="flex-1 min-h-14 bg-transparent outline-none text-[17px]" [placeholder]="'shell.search' | transloco"
          role="combobox" aria-controls="cp-list" [attr.aria-activedescendant]="'cp-' + active()" aria-autocomplete="list"
          [value]="q()" (input)="q.set(inp.value)" (keydown)="onKey($event)">
        <kbd class="hidden sm:inline text-xs text-mut border border-line rounded px-1.5 py-0.5">Esc</kbd>
      </div>
      <ul id="cp-list" role="listbox" class="list-none m-0 p-2 max-h-[60vh] overflow-y-auto">
        @for (it of items(); track it.url + it.label; let i = $index) {
          @if (i === 0 || items()[i - 1].kind !== it.kind) {
            <li role="presentation" class="px-3 pt-3 pb-1 text-xs font-semibold text-mut">{{ ('palette.' + (it.kind === 'page' ? 'pages' : it.kind === 'action' ? 'actions' : 'students')) | transloco }}</li>
          }
          <li [id]="'cp-' + i" role="option" [attr.aria-selected]="i === active()" (click)="go(it)" (mousemove)="active.set(i)"
            class="flex items-center gap-3 px-3 py-2 rounded-xl cursor-pointer" [class.bg-soft]="i === active()">
            @if (it.student) { <m-avatar [name]="it.student.name" [size]="34" /> }
            @else { <span class="size-[34px] grid place-items-center rounded-lg bg-soft text-g2"><m-icon [name]="it.icon" [size]="18" /></span> }
            <div class="flex-1 leading-tight">
              <b class="font-semibold">{{ it.label }}</b>
              @if (it.sub) { <small class="block text-mut text-[13px]">{{ it.sub | digits }}</small> }
            </div>
            @if (i === active()) { <m-icon name="chevronRight" class="text-mut" [size]="18" /> }
          </li>
        } @empty {
          <li class="px-3 py-8 text-center text-mut">{{ (searching() ? 'palette.searching' : 'palette.noResults') | transloco }}</li>
        }
        @if (searching() && items().length) { <li class="px-3 py-2 text-sm text-mut">{{ 'palette.searching' | transloco }}</li> }
      </ul>
      <div class="hidden sm:block px-4 py-2.5 border-t border-line text-xs text-mut">{{ 'palette.hint' | transloco }}</div>
    </dialog>
  `,
})
export class CommandPalette {
  readonly open = model(false);
  private http = inject(HttpClient);
  private router = inject(Router);
  private auth = inject(AuthService);
  private t = inject(TranslocoService);
  private dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');
  private inp = viewChild.required<ElementRef<HTMLInputElement>>('inp');

  protected q = signal('');
  protected active = signal(0);
  protected searching = signal(false);
  private students = signal<StudentHit[]>([]);
  private timer?: ReturnType<typeof setTimeout>;
  private lang = signal(this.t.getActiveLang());

  private actions = computed(() => {
    this.lang();
    const a: (Item & { perm: string })[] = [
      { kind: 'action', label: this.t.translate('palette.addStudent'), icon: 'plus', url: '/admission/new', perm: P.admission },
      { kind: 'action', label: this.t.translate('palette.takeAttendance'), icon: 'scan', url: '/attendance', perm: P.attendance },
      { kind: 'action', label: this.t.translate('palette.collectFee'), icon: 'wallet', url: '/fees', perm: P.fees },
      { kind: 'action', label: this.t.translate('palette.publishNotice'), icon: 'mega', url: '/notices', perm: P.notices },
    ];
    return a.filter((x) => this.auth.can(x.perm));
  });
  private pages = computed<Item[]>(() => {
    this.lang();
    return ALL_NAV.filter((n) => this.auth.can(n.perm)).map((n) => ({
      kind: 'page' as const, label: this.t.translate('nav.' + n.key), sub: n.desc, icon: n.icon, url: '/' + n.path,
    }));
  });

  protected items = computed<Item[]>(() => {
    const q = this.q().trim().toLowerCase();
    const match = (i: Item) => !q || i.label.toLowerCase().includes(q) || (i.sub ?? '').toLowerCase().includes(q);
    const stu = this.students().map<Item>((s) => ({
      kind: 'student', label: s.name, icon: 'users', url: `/students/${s.id}`, student: s,
      sub: [s.studentCode, s.className, s.roll ? tr('রোল {n}', { n: fmt(s.roll) }) : '', s.phone].filter(Boolean).join(' · '),
    }));
    return [...stu, ...this.actions().filter(match), ...this.pages().filter(match).slice(0, q ? 6 : 8)];
  });

  constructor() {
    this.t.langChanges$.subscribe((l) => this.lang.set(l));
    effect(() => {
      const d = this.dlg().nativeElement;
      if (this.open() && !d.open) {
        untracked(() => { this.q.set(''); this.students.set([]); this.active.set(0); });
        d.showModal();
        queueMicrotask(() => this.inp().nativeElement.focus());
      }
      if (!this.open() && d.open) d.close();
    });
    effect(() => {
      const q = this.q().trim();
      untracked(() => this.active.set(0));
      clearTimeout(this.timer);
      if (q.length < 2 || !this.auth.can(P.studentsView)) { untracked(() => { this.students.set([]); this.searching.set(false); }); return; }
      untracked(() => this.searching.set(true));
      this.timer = setTimeout(() => {
        this.http.get<PagedResult<StudentHit>>(`${API}/students`, { params: { q: toEnDigits(q), pageSize: 6 } }).subscribe({
          next: (r) => { if (this.q().trim() === q) { this.students.set(r.items); this.searching.set(false); } },
          error: () => { this.students.set([]); this.searching.set(false); },
        });
      }, 220);
    });
  }

  protected onKey(e: KeyboardEvent) {
    const n = this.items().length;
    if (e.key === 'ArrowDown') { e.preventDefault(); this.active.set(n ? (this.active() + 1) % n : 0); this.scroll(); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); this.active.set(n ? (this.active() - 1 + n) % n : 0); this.scroll(); }
    else if (e.key === 'Enter') { e.preventDefault(); const it = this.items()[this.active()]; if (it) this.go(it); }
  }
  private scroll() {
    queueMicrotask(() => this.dlg().nativeElement.querySelector(`#cp-${this.active()}`)?.scrollIntoView({ block: 'nearest' }));
  }
  protected go(it: Item) {
    this.open.set(false);
    this.router.navigateByUrl(it.url);
  }
}
