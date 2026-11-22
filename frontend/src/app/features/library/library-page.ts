import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { errorMessage } from '../../core/api';
import { DatePipe, DigitsPipe, MoneyPipe, NumPipe, toEnDigits } from '../../core/format';
import { ToastService } from '../../core/toast.service';
import { Badge, Button, CellDef, Chips, Column, ConfirmService, DataTable, FormField, Icon, LoadError, Modal, PageHeader, RowActionsDef, Segmented, Skeleton } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { OfficeApi } from '../office/office.api';
import { Book, BookInput, BookLanguage, Borrower, Issue, LANG_LABEL, LibrarySummary } from '../office/office.models';

type Tab = 'books' | 'out' | 'overdue' | 'fines' | 'returned';
const blank = (): BookInput => ({ title: '', titleAr: null, author: null, language: 'Arabic', category: null, shelfNo: null, totalCopies: 1 });

@Component({
  selector: 'app-library-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePipe, DigitsPipe, MoneyPipe, NumPipe, Badge, Button, CellDef, Chips, DataTable, FormField, Icon, LoadError, Modal, PageHeader, RowActionsDef, Segmented, Skeleton],
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
    { value: 'overdue' as Tab, label: `মেয়াদোত্তীর্ণ (${(this.summary()?.overdue ?? 0).toLocaleString('bn-BD')})` }, { value: 'fines' as Tab, label: `জরিমানা বাকি (${(this.summary()?.finesPending ?? 0).toLocaleString('bn-BD')})` }, { value: 'returned' as Tab, label: 'ফেরত' },
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
    { key: 'fineDue', label: 'জরিমানা', sortable: true, align: 'end', value: (i) => (i.returnedOn ? (this.tab() === 'fines' ? i.fineOutstanding : i.fine) : i.fineDue) },
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
  protected retCollect = signal(true);
  protected returning = signal(false);

  protected loadError = signal<string | null>(null);
  private failed(e: unknown) { this.loadError.set(errorMessage(e, 'তথ্য লোড করা যায়নি।')); }
  protected retry() { this.loadError.set(null); this.loadSummary(); this.loadTab(this.tab(), this.lang(), this.category()); }

  constructor() {
    this.loadSummary();
    effect(() => { const t = this.tab(), l = this.lang(), c = this.category(); untracked(() => this.loadTab(t, l, c)); });
    let timer: ReturnType<typeof setTimeout> | undefined;
    effect(() => { const q = this.bq(); clearTimeout(timer); timer = setTimeout(() => untracked(() => this.findBorrowers(q)), 250); });
  }
  private loadSummary() { this.api.library().then((s) => this.summary.set(s)).catch((e) => this.failed(e)); }
  private async loadTab(t: Tab, l: string, c: string) {
    try {
      if (t === 'books') { this.books.set(null); this.books.set(await this.api.books('', l, c)); }
      else { this.issues.set(null); this.issues.set(await this.api.issues(t)); }
    } catch (e) { this.failed(e); }
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
  protected openReturn(i: Issue) { this.retIssue.set(i); this.retFine.set(String(i.fineDue)); this.retCollect.set(true); this.retOpen.set(true); }
  protected async doReturn() {
    const i = this.retIssue(); if (!i) return;
    this.returning.set(true);
    try {
      const fine = +toEnDigits(this.retFine()) || 0;
      const r = await this.api.returnBook(i.id, fine, fine > 0 && this.retCollect());
      const amt = r.fine.toLocaleString('bn-BD');
      this.toast.success(!r.fine ? 'বই ফেরত নেওয়া হয়েছে' : r.finePaid ? `বই ফেরত নেওয়া হয়েছে, জরিমানা ${amt} টাকা আয়ে জমা হয়েছে` : `বই ফেরত নেওয়া হয়েছে, জরিমানা ${amt} টাকা বাকি রইল`);
      this.retOpen.set(false); this.refresh();
    } catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.returning.set(false); }
  }

  // pending fines
  protected fineOpen = signal(false);
  protected fineIssue = signal<Issue | null>(null);
  protected fineAmt = signal('');
  protected fineBusy = signal(false);
  protected openFine(i: Issue) { this.fineIssue.set(i); this.fineAmt.set(String(i.fineOutstanding)); this.fineOpen.set(true); }
  protected async collectFine() {
    const i = this.fineIssue(); if (!i) return;
    this.fineBusy.set(true);
    try {
      const r = await this.api.collectFine(i.id, +toEnDigits(this.fineAmt()) || null);
      this.toast.success(r.fineOutstanding ? `জমা হয়েছে, এখনও বাকি ${r.fineOutstanding.toLocaleString('bn-BD')} টাকা` : 'জরিমানা আদায় হয়ে আয়ে জমা হয়েছে');
      this.fineOpen.set(false); this.refresh();
    } catch (e) { const fe = fieldErrors(e); this.toast.error(Object.values(fe)[0] ?? errorMessage(e)); }
    finally { this.fineBusy.set(false); }
  }
  protected async waiveFine() {
    const i = this.fineIssue(); if (!i) return;
    if (!(await this.confirm.ask({ message: 'বাকি জরিমানা মাফ হয়ে যাবে। আগে যা আদায় হয়েছে তা হিসাবে থেকে যাবে।', itemName: `${i.borrowerName} · ${i.fineOutstanding.toLocaleString('bn-BD')} টাকা`, confirmLabel: 'মাফ করুন' }))) return;
    try { await this.api.waiveFine(i.id); this.toast.success('জরিমানা মাফ করা হয়েছে'); this.fineOpen.set(false); this.refresh(); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }
}
