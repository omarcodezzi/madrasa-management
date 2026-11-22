import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { DigitsPipe, MoneyPipe, NumPipe, fmt } from '../../core/format';
import { PreferencesService, Theme } from '../../core/preferences.service';
import { ToastService } from '../../core/toast.service';
import { Lang } from '../../core/i18n';
import {
  Avatar, Badge, BadgeTone, BulkActionsDef, Button, Card, CellDef, Chips, Column, ConfirmService, DataTable, EmptyState,
  FormField, Icon, IconButton, ICONS, IconName, Modal, PageHeader, RowActionsDef, Segmented, Skeleton,
} from '../../shared/ui';
import { TrPipe, tr } from '../../core/tr';

interface DemoStudent { id: string; name: string; father: string; cls: string; roll: number; phone: string; due: number; status: 'present' | 'absent' | 'late' | 'leave'; }

const NAMES = [tr('আব্দুল্লাহ আল মামুন'), tr('মুহাম্মাদ হাসান'), tr('উমর ফারুক'), tr('তাহমিদ হোসেন'), tr('আরিফ বিল্লাহ'), tr('ইয়াসিন আরাফাত'), tr('সাদিক রহমান'),
  tr('মাহমুদুল হাসান'), tr('আনাস ইবনে কাসেম'), tr('জুবায়ের আহমদ'), tr('ফাতিমা আক্তার'), tr('আয়েশা সিদ্দিকা'), tr('মারিয়াম খাতুন'), tr('খাদিজা ইসলাম'),
  tr('সুমাইয়া আক্তার'), tr('হাফসা বিনতে রফিক'), tr('নাফিসা তাবাসসুম'), tr('রাকিব হাসান')];
const FATHERS = [tr('আবুল কাশেম'), tr('মোঃ রফিকুল ইসলাম'), tr('আব্দুর রহিম'), tr('মোঃ জসিম উদ্দিন'), tr('নুরুল আমিন'), tr('মাওলানা ইউসুফ')];
const CLASSES = [tr('নূরানী'), tr('নাজেরা'), tr('হিফজ'), tr('ইবতেদায়ী'), tr('মিশকাত'), tr('দাওরায়ে হাদীস')];
const STATUSES: DemoStudent['status'][] = ['present', 'present', 'present', 'absent', 'late', 'present', 'leave'];

@Component({
  selector: 'app-design-system',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, 
    FormsModule, TranslocoPipe, NumPipe, MoneyPipe, DigitsPipe, Avatar, Badge, Button, Card, CellDef, RowActionsDef, BulkActionsDef,
    Chips, DataTable, EmptyState, FormField, Icon, IconButton, Modal, PageHeader, Segmented, Skeleton,
  ],
  templateUrl: './design-system.html',
})
export class DesignSystemPage {
  protected readonly tr = tr;
  protected prefs = inject(PreferencesService);
  protected toast = inject(ToastService);
  private confirm = inject(ConfirmService);

  protected themeOpts: { value: Theme; label: string }[] = [
    { value: 'system', label: tr('সিস্টেম') }, { value: 'light', label: tr('লাইট') }, { value: 'dark', label: tr('ডার্ক') },
  ];
  protected langOpts: { value: Lang; label: string }[] = [{ value: 'bn', label: tr('বাংলা') }, { value: 'en', label: 'English' }];
  protected digitOpts = [{ value: 'bn' as const, label: tr('০১২৩') }, { value: 'en' as const, label: '0123' }];

  protected colors = [
    { name: tr('মাদরাসা সবুজ'), v: '--g', hex: '#0b5d33' }, { name: tr('সবুজ (লেখা)'), v: '--g2', hex: '#0e7a43' },
    { name: tr('সোনালি'), v: '--gold', hex: '#e0a50f' }, { name: tr('সাইডবার'), v: '--side', hex: '#0a4729' },
    { name: tr('পটভূমি'), v: '--bg', hex: '#f3f6f1' }, { name: tr('কার্ড'), v: '--card', hex: '#ffffff' },
    { name: tr('হালকা'), v: '--soft', hex: '#edf4ea' }, { name: tr('রেখা'), v: '--line', hex: '#dfe7db' },
    { name: tr('লেখা'), v: '--ink', hex: '#12281b' }, { name: tr('হালকা লেখা'), v: '--mut', hex: '#5a6d60' },
    { name: tr('লাল'), v: '--red', hex: '#b42318' }, { name: tr('সতর্কতা'), v: '--warn', hex: '#b45309' },
  ];
  protected badges: { tone: BadgeTone; label: string }[] = [
    { tone: 'success', label: tr('উপস্থিত') }, { tone: 'danger', label: tr('অনুপস্থিত') }, { tone: 'warning', label: tr('দেরি') },
    { tone: 'info', label: tr('ছুটি') }, { tone: 'gold', label: tr('মুমতায') }, { tone: 'neutral', label: tr('খসড়া') },
  ];
  protected icons = Object.keys(ICONS) as IconName[];

  protected classFilter = signal<string>('all');
  protected classChips = [{ value: 'all', label: tr('সব শ্রেণি') }, ...CLASSES.map((c) => ({ value: c, label: c }))];
  protected attTab = signal<'qr' | 'manual'>('qr');

  protected students: DemoStudent[] = NAMES.map((n, i) => ({
    id: `RM-2026-${String(i + 1).padStart(3, '0')}`, name: n, father: FATHERS[i % FATHERS.length], cls: CLASSES[i % CLASSES.length],
    roll: Math.floor(i / CLASSES.length) + 1, phone: `01711-0000${10 + i}`, due: i % 3 === 1 ? 500 * ((i % 4) + 1) : 0, status: STATUSES[i % STATUSES.length],
  }));
  protected columns: Column<DemoStudent>[] = [
    { key: 'name', label: tr('নাম'), sortable: true, primary: true },
    { key: 'id', label: tr('আইডি'), sortable: true },
    { key: 'cls', label: tr('শ্রেণি'), sortable: true },
    { key: 'roll', label: tr('রোল'), sortable: true, align: 'end' },
    { key: 'phone', label: tr('মোবাইল') },
    { key: 'father', label: tr('পিতা'), hidden: true },
    { key: 'status', label: tr('আজ'), align: 'center' },
    { key: 'due', label: tr('বকেয়া'), sortable: true, align: 'end' },
  ];
  protected statusTone: Record<string, BadgeTone> = { present: 'success', absent: 'danger', late: 'warning', leave: 'info' };
  protected filteredStudents = () => this.classFilter() === 'all' ? this.students : this.students.filter((s) => s.cls === this.classFilter());

  // form demo
  protected form = signal({ name: '', phone: '', cls: '', dob: '', note: '' });
  protected submitted = signal(false);
  protected saving = signal(false);
  protected nameError = () => (this.submitted() && !this.form().name.trim() ? tr('নাম লিখুন') : null);
  protected phoneError = () => {
    const p = this.form().phone.replace(/[^0-9০-৯]/g, ''); // i18n-ignore
    return this.submitted() && p.length !== 11 ? tr('সঠিক মোবাইল নম্বর লিখুন (১১ সংখ্যা)') : null;
  };
  protected patch(k: keyof ReturnType<typeof this.form>, v: string) { this.form.update((f) => ({ ...f, [k]: v })); }
  protected save() {
    this.submitted.set(true);
    if (this.nameError() || this.phoneError()) { this.toast.error(tr('লাল চিহ্নিত ঘরগুলো ঠিক করুন')); return; }
    this.saving.set(true);
    setTimeout(() => { this.saving.set(false); this.toast.success(tr('সংরক্ষণ হয়েছে')); }, 900);
  }

  protected modalOpen = signal(false);
  protected loadingDemo = signal(true);

  protected async remove(s: DemoStudent) {
    if (await this.confirm.ask({ message: tr('এই শিক্ষার্থীর সব তথ্য মুছে যাবে।'), itemName: s.name })) this.toast.success(tr('{name} মুছে ফেলা হয়েছে (ডেমো)', { name: s.name }));
  }
  protected bulkSms(rows: DemoStudent[]) { this.toast.show(tr('{n} জন অভিভাবককে SMS পাঠানো হবে (ডেমো)', { n: fmt(rows.length) }), 'info'); }
}
