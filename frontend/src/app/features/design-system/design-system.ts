import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { DigitsPipe, MoneyPipe, NumPipe } from '../../core/format';
import { PreferencesService, Theme } from '../../core/preferences.service';
import { ToastService } from '../../core/toast.service';
import { Lang } from '../../core/i18n';
import {
  Avatar, Badge, BadgeTone, BulkActionsDef, Button, Card, CellDef, Chips, Column, ConfirmService, DataTable, EmptyState,
  FormField, Icon, IconButton, ICONS, IconName, Modal, PageHeader, RowActionsDef, Segmented, Skeleton,
} from '../../shared/ui';

interface DemoStudent { id: string; name: string; father: string; cls: string; roll: number; phone: string; due: number; status: 'present' | 'absent' | 'late' | 'leave'; }

const NAMES = ['আব্দুল্লাহ আল মামুন', 'মুহাম্মাদ হাসান', 'উমর ফারুক', 'তাহমিদ হোসেন', 'আরিফ বিল্লাহ', 'ইয়াসিন আরাফাত', 'সাদিক রহমান',
  'মাহমুদুল হাসান', 'আনাস ইবনে কাসেম', 'জুবায়ের আহমদ', 'ফাতিমা আক্তার', 'আয়েশা সিদ্দিকা', 'মারিয়াম খাতুন', 'খাদিজা ইসলাম',
  'সুমাইয়া আক্তার', 'হাফসা বিনতে রফিক', 'নাফিসা তাবাসসুম', 'রাকিব হাসান'];
const FATHERS = ['আবুল কাশেম', 'মোঃ রফিকুল ইসলাম', 'আব্দুর রহিম', 'মোঃ জসিম উদ্দিন', 'নুরুল আমিন', 'মাওলানা ইউসুফ'];
const CLASSES = ['নূরানী', 'নাজেরা', 'হিফজ', 'ইবতেদায়ী', 'মিশকাত', 'দাওরায়ে হাদীস'];
const STATUSES: DemoStudent['status'][] = ['present', 'present', 'present', 'absent', 'late', 'present', 'leave'];

@Component({
  selector: 'app-design-system',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, TranslocoPipe, NumPipe, MoneyPipe, DigitsPipe, Avatar, Badge, Button, Card, CellDef, RowActionsDef, BulkActionsDef,
    Chips, DataTable, EmptyState, FormField, Icon, IconButton, Modal, PageHeader, Segmented, Skeleton,
  ],
  templateUrl: './design-system.html',
})
export class DesignSystemPage {
  protected prefs = inject(PreferencesService);
  protected toast = inject(ToastService);
  private confirm = inject(ConfirmService);

  protected themeOpts: { value: Theme; label: string }[] = [
    { value: 'system', label: 'সিস্টেম' }, { value: 'light', label: 'লাইট' }, { value: 'dark', label: 'ডার্ক' },
  ];
  protected langOpts: { value: Lang; label: string }[] = [{ value: 'bn', label: 'বাংলা' }, { value: 'en', label: 'English' }];
  protected digitOpts = [{ value: 'bn' as const, label: '০১২৩' }, { value: 'en' as const, label: '0123' }];

  protected colors = [
    { name: 'মাদরাসা সবুজ', v: '--g', hex: '#0b5d33' }, { name: 'সবুজ (লেখা)', v: '--g2', hex: '#0e7a43' },
    { name: 'সোনালি', v: '--gold', hex: '#e0a50f' }, { name: 'সাইডবার', v: '--side', hex: '#0a4729' },
    { name: 'পটভূমি', v: '--bg', hex: '#f3f6f1' }, { name: 'কার্ড', v: '--card', hex: '#ffffff' },
    { name: 'হালকা', v: '--soft', hex: '#edf4ea' }, { name: 'রেখা', v: '--line', hex: '#dfe7db' },
    { name: 'লেখা', v: '--ink', hex: '#12281b' }, { name: 'হালকা লেখা', v: '--mut', hex: '#5a6d60' },
    { name: 'লাল', v: '--red', hex: '#b42318' }, { name: 'সতর্কতা', v: '--warn', hex: '#b45309' },
  ];
  protected badges: { tone: BadgeTone; label: string }[] = [
    { tone: 'success', label: 'উপস্থিত' }, { tone: 'danger', label: 'অনুপস্থিত' }, { tone: 'warning', label: 'দেরি' },
    { tone: 'info', label: 'ছুটি' }, { tone: 'gold', label: 'মুমতায' }, { tone: 'neutral', label: 'খসড়া' },
  ];
  protected icons = Object.keys(ICONS) as IconName[];

  protected classFilter = signal<string>('all');
  protected classChips = [{ value: 'all', label: 'সব শ্রেণি' }, ...CLASSES.map((c) => ({ value: c, label: c }))];
  protected attTab = signal<'qr' | 'manual'>('qr');

  protected students: DemoStudent[] = NAMES.map((n, i) => ({
    id: `RM-2026-${String(i + 1).padStart(3, '0')}`, name: n, father: FATHERS[i % FATHERS.length], cls: CLASSES[i % CLASSES.length],
    roll: Math.floor(i / CLASSES.length) + 1, phone: `01711-0000${10 + i}`, due: i % 3 === 1 ? 500 * ((i % 4) + 1) : 0, status: STATUSES[i % STATUSES.length],
  }));
  protected columns: Column<DemoStudent>[] = [
    { key: 'name', label: 'নাম', sortable: true, primary: true },
    { key: 'id', label: 'আইডি', sortable: true },
    { key: 'cls', label: 'শ্রেণি', sortable: true },
    { key: 'roll', label: 'রোল', sortable: true, align: 'end' },
    { key: 'phone', label: 'মোবাইল' },
    { key: 'father', label: 'পিতা', hidden: true },
    { key: 'status', label: 'আজ', align: 'center' },
    { key: 'due', label: 'বকেয়া', sortable: true, align: 'end' },
  ];
  protected statusTone: Record<string, BadgeTone> = { present: 'success', absent: 'danger', late: 'warning', leave: 'info' };
  protected filteredStudents = () => this.classFilter() === 'all' ? this.students : this.students.filter((s) => s.cls === this.classFilter());

  // form demo
  protected form = signal({ name: '', phone: '', cls: '', dob: '', note: '' });
  protected submitted = signal(false);
  protected saving = signal(false);
  protected nameError = () => (this.submitted() && !this.form().name.trim() ? 'নাম লিখুন' : null);
  protected phoneError = () => {
    const p = this.form().phone.replace(/[^0-9০-৯]/g, '');
    return this.submitted() && p.length !== 11 ? 'সঠিক মোবাইল নম্বর লিখুন (১১ সংখ্যা)' : null;
  };
  protected patch(k: keyof ReturnType<typeof this.form>, v: string) { this.form.update((f) => ({ ...f, [k]: v })); }
  protected save() {
    this.submitted.set(true);
    if (this.nameError() || this.phoneError()) { this.toast.error('লাল চিহ্নিত ঘরগুলো ঠিক করুন'); return; }
    this.saving.set(true);
    setTimeout(() => { this.saving.set(false); this.toast.success('সংরক্ষণ হয়েছে'); }, 900);
  }

  protected modalOpen = signal(false);
  protected loadingDemo = signal(true);

  protected async remove(s: DemoStudent) {
    if (await this.confirm.ask({ message: 'এই শিক্ষার্থীর সব তথ্য মুছে যাবে।', itemName: s.name })) this.toast.success(`${s.name} মুছে ফেলা হয়েছে (ডেমো)`);
  }
  protected bulkSms(rows: DemoStudent[]) { this.toast.show(`${rows.length} জন অভিভাবককে SMS পাঠানো হবে (ডেমো)`, 'info'); }
}
