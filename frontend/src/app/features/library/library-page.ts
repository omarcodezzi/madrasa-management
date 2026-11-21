import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { errorMessage } from '../../core/api';
import { DatePipe, DigitsPipe, MoneyPipe, NumPipe, toEnDigits } from '../../core/format';
import { ToastService } from '../../core/toast.service';
import { Badge, Button, CellDef, Chips, Column, ConfirmService, DataTable, FormField, Icon, Modal, PageHeader, RowActionsDef, Segmented, Skeleton } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { OfficeApi } from '../office/office.api';
import { Book, BookInput, BookLanguage, Borrower, Issue, LANG_LABEL, LibrarySummary } from '../office/office.models';

type Tab = 'books' | 'out' | 'overdue' | 'returned';
const blank = (): BookInput => ({ title: '', titleAr: null, author: null, language: 'Arabic', category: null, shelfNo: null, totalCopies: 1 });

@Component({
  selector: 'app-library-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePipe, DigitsPipe, MoneyPipe, NumPipe, Badge, Button, CellDef, Chips, DataTable, FormField, Icon, Modal, PageHeader, RowActionsDef, Segmented, Skeleton],
  templateUrl: './library-page.html',
})
export class LibraryPage {
  private api = inject(OfficeApi);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);

  protected summary = signal<LibrarySummary | null>(null);
  protected tab = signal<Tab>('books');
  protected tabs = computed(() => [
    { value: 'books' as Tab, label: 'বই' }, { value: 'out' as Tab, label: `ইস্যু করা (${(this.summary()?.out ?? 0).toLocaleString('bn-BD')})` },
    { value: 'overdue' as Tab, label: `মেয়াদোত্তীর্ণ (${(this.summary()?.overdue ?? 0).toLocaleString('bn-BD')})` }, { value: 'returned' as Tab, label: 'ফেরত' },
  ]);
  protected lLabel = (l: string) => (LANG_LABEL as Record<string, string>)[l] ?? l;
  protected lang = signal<'' | BookLanguage>('');
  protected langs = [{ value: '' as '' | BookLanguage, label: 'সব ভাষা' }, ...(Object.keys(LANG_LABEL) as BookLanguage[]).map((l) => ({ value: l as '' | BookLanguage, label: LANG_LABEL[l] }))];
  protected category = signal('');
  protected books = signal<Book[] | null>(null);
  protected issues = signal<Issue[] | null>(null);
  protected bookCols: Column<Book>[] = [
    { key: 'title', label: 'বই', sortable: true, primary: true, value: (b) => `${b.title} ${b.titleAr ?? ''} ${b.author ?? ''}` },
    { key: 'category', label: 'বিষয়', sortable: true },
    { key: 'language', label: 'ভাষা', value: (b) => LANG_LABEL[b.language] },
    { key: 'shelfNo', label: 'তাক', sortable: true },
    { key: 'available', label: 'তাকে আছে', sortable: true, align: 'end' },
  ];
  protected issueCols: Column<Issue>[] = [
    { key: 'title', label: 'বই', sortable: true, primary: true, value: (i) => `${i.title} ${i.borrowerName} ${i.borrowerCode}` },
    { key: 'borrowerName', label: 'নিয়েছেন', sortable: true },
    { key: 'issuedOn', label: 'ইস্যু', sortable: true },
    { key: 'dueOn', label: 'ফেরতের তারিখ', sortable: true },
    { key: 'fineDue', label: 'জরিমানা', sortable: true, align: 'end', value: (i) => (i.returnedOn ? i.fine : i.fineDue) },
  ];

  // book modal
  protected bookOpen = signal(false);
  protected bookId = signal<number | null>(null);
  protected form = signal<BookInput>(blank());
  protected errs = signal<Record<string, string>>({});
  protected saving = signal(false);

  // issue modal
  protected issueOpen = signal(false);
  protected issueBook = signal<Book | null>(null);
  protected bq = signal('');
  protected borrowers = signal<Borrower[]>([]);
  protected borrower = signal<Borrower | null>(null);
  protected days = signal('14');
  protected issuing = signal(false);

  // return modal
  protected retOpen = signal(false);
  protected retIssue = signal<Issue | null>(null);
  protected retFine = signal('0');
  protected returning = signal(false);

  constructor() {
    this.loadSummary();
    effect(() => { const t = this.tab(), l = this.lang(), c = this.category(); untracked(() => this.loadTab(t, l, c)); });
    let timer: ReturnType<typeof setTimeout> | undefined;
    effect(() => { const q = this.bq(); clearTimeout(timer); timer = setTimeout(() => untracked(() => this.findBorrowers(q)), 250); });
  }
  private loadSummary() { this.api.library().then((s) => this.summary.set(s)).catch((e) => this.toast.error(errorMessage(e))); }
  private async loadTab(t: Tab, l: string, c: string) {
    try {
      if (t === 'books') { this.books.set(null); this.books.set(await this.api.books('', l, c)); }
      else { this.issues.set(null); this.issues.set(await this.api.issues(t)); }
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
  private refresh() { this.loadSummary(); this.loadTab(this.tab(), this.lang(), this.category()); }
  private async findBorrowers(q: string) {
    if (q.trim().length < 2) { this.borrowers.set([]); return; }
    try { this.borrowers.set(await this.api.borrowers(toEnDigits(q.trim()))); } catch { this.borrowers.set([]); }
  }

  protected newBook() { this.bookId.set(null); this.form.set(blank()); this.errs.set({}); this.bookOpen.set(true); }
  protected editBook(b: Book) {
    this.bookId.set(b.id); this.errs.set({}); this.bookOpen.set(true);
    this.form.set({ title: b.title, titleAr: b.titleAr, author: b.author, language: b.language, category: b.category, shelfNo: b.shelfNo, totalCopies: b.totalCopies });
  }
  protected patch(p: Partial<BookInput>) { this.form.update((f) => ({ ...f, ...p })); }
  protected async saveBook() {
    const f = this.form(); this.saving.set(true); this.errs.set({});
    try {
      await this.api.saveBook(this.bookId(), { ...f, totalCopies: +toEnDigits(String(f.totalCopies)) || 0 });
      this.toast.success('বই সংরক্ষণ হয়েছে'); this.bookOpen.set(false); this.refresh();
    } catch (e) { const fe = fieldErrors(e); this.errs.set(fe); if (!Object.keys(fe).length) this.toast.error(errorMessage(e)); }
    finally { this.saving.set(false); }
  }
  protected async deleteBook() {
    const id = this.bookId(); if (!id) return;
    if (!(await this.confirm.ask({ message: 'বইটি ও তার ইস্যুর পুরনো রেকর্ড মুছে যাবে।', itemName: this.form().title, confirmLabel: 'মুছে ফেলুন' }))) return;
    try { await this.api.deleteBook(id); this.toast.success('বই মুছে ফেলা হয়েছে'); this.bookOpen.set(false); this.refresh(); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }

  protected openIssue(b: Book) { this.issueBook.set(b); this.borrower.set(null); this.bq.set(''); this.days.set('14'); this.issueOpen.set(true); }
  protected async issue() {
    const b = this.issueBook(), p = this.borrower(); if (!b || !p) return;
    this.issuing.set(true);
    try {
      await this.api.issue({ bookId: b.id, studentId: p.kind === 'student' ? p.id : null, staffId: p.kind === 'staff' ? p.id : null, days: +toEnDigits(this.days()) || 14 });
      this.toast.success(`"${b.title}" ${p.name}-কে দেওয়া হয়েছে`); this.issueOpen.set(false); this.refresh();
    } catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.issuing.set(false); }
  }
  protected openReturn(i: Issue) { this.retIssue.set(i); this.retFine.set(String(i.fineDue)); this.retOpen.set(true); }
  protected async doReturn() {
    const i = this.retIssue(); if (!i) return;
    this.returning.set(true);
    try {
      const r = await this.api.returnBook(i.id, +toEnDigits(this.retFine()) || 0);
      this.toast.success(r.fine ? `বই ফেরত নেওয়া হয়েছে, জরিমানা ${r.fine.toLocaleString('bn-BD')} টাকা` : 'বই ফেরত নেওয়া হয়েছে');
      this.retOpen.set(false); this.refresh();
    } catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.returning.set(false); }
  }
}
