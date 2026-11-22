import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { errorMessage, fileUrl, Gender } from '../../core/api';
import { DatePipe, DigitsPipe, NumPipe, toBnDigits, toEnDigits, todayIso } from '../../core/format';
import { ToastService } from '../../core/toast.service';
import { Avatar, Badge, Button, Card, ConfirmService, EmptyState, FormField, Icon, LoadError, Modal, PageHeader, Segmented, Skeleton } from '../../shared/ui';
import { fieldErrors } from '../academic/academic.service';
import { OfficeApi } from '../office/office.api';
import { HostelSummary, LeaveRow, Occupant, Resident, Room } from '../office/office.models';

type Tab = 'rooms' | 'leaves';
interface RoomForm { building: string; roomNo: string; seats: string; forGender: Gender; }

@Component({
  selector: 'app-hostel-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, DatePipe, DigitsPipe, NumPipe, Avatar, Badge, Button, Card, EmptyState, FormField, Icon, LoadError, Modal, PageHeader, Segmented, Skeleton],
  templateUrl: './hostel-page.html',
})
export class HostelPage {
  private api = inject(OfficeApi);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);

  protected tab = signal<Tab>('rooms');
  protected tabs = [{ value: 'rooms' as Tab, label: 'রুম ও সিট' }, { value: 'leaves' as Tab, label: 'ছুটির রেজিস্টার' }];
  protected data = signal<HostelSummary | null>(null);
  protected gender = signal<'' | Gender>('');
  protected genders = [{ value: '' as '' | Gender, label: 'সব' }, { value: 'Male' as '' | Gender, label: 'ছাত্র' }, { value: 'Female' as '' | Gender, label: 'ছাত্রী' }];
  protected rooms = computed(() => (this.data()?.roomList ?? []).filter((r) => !this.gender() || r.forGender === this.gender()));
  protected buildings = computed(() => {
    const map = new Map<string, Room[]>();
    for (const r of this.rooms()) { const k = r.building || 'অন্যান্য'; map.set(k, [...(map.get(k) ?? []), r]); }
    return [...map.entries()].map(([name, rooms]) => ({ name, rooms }));
  });
  protected photo = (p: string | null) => fileUrl(p);
  protected seats = (r: Room) => Array.from({ length: r.seats }, (_, i) => ({ no: i + 1, o: r.occupants.find((o) => o.seatNo === i + 1) ?? null }));

  // room modal
  protected roomOpen = signal(false);
  protected roomId = signal<number | null>(null);
  protected roomForm = signal<RoomForm>({ building: '', roomNo: '', seats: '4', forGender: 'Male' });
  protected roomErrs = signal<Record<string, string>>({});
  protected savingRoom = signal(false);

  // allocate
  protected allocOpen = signal(false);
  protected allocRoom = signal<Room | null>(null);
  protected allocSeat = signal<number | null>(null);
  protected q = signal('');
  protected candidates = signal<Resident[] | null>(null);
  protected fitting = computed(() => (this.candidates() ?? []).filter((c) => c.gender === this.allocRoom()?.forGender));

  // occupant
  protected occOpen = signal(false);
  protected occ = signal<{ o: Occupant; room: Room } | null>(null);

  // leaves
  protected openOnly = signal(true);
  protected leaves = signal<LeaveRow[] | null>(null);
  protected leaveOpen = signal(false);
  protected residents = signal<Resident[]>([]);
  protected leaveForm = signal({ studentId: 0, fromDate: todayIso(), toDate: '', reason: '', takenBy: '' });
  protected leaveErrs = signal<Record<string, string>>({});
  protected savingLeave = signal(false);
  protected today = todayIso();

  protected loadError = signal<string | null>(null);
  private failed(e: unknown) { this.loadError.set(errorMessage(e, 'তথ্য লোড করা যায়নি।')); }
  protected retry() { this.loadError.set(null); this.load(); if (this.tab() === 'leaves') this.loadLeaves(this.openOnly()); }

  constructor() {
    this.load();
    effect(() => { const t = this.tab(), o = this.openOnly(); if (t === 'leaves') untracked(() => this.loadLeaves(o)); });
    let timer: ReturnType<typeof setTimeout> | undefined;
    effect(() => { const q = this.q(); if (!this.allocOpen()) return; clearTimeout(timer); timer = setTimeout(() => untracked(() => this.search(q)), 250); });
  }
  private async load() { try { this.data.set(await this.api.hostel()); } catch (e) { this.failed(e); } }
  private async loadLeaves(open: boolean) { this.leaves.set(null); try { this.leaves.set(await this.api.leaves(open)); } catch (e) { this.failed(e); } }
  private async search(q: string) { try { this.candidates.set(await this.api.candidates(toEnDigits(q.trim()))); } catch { this.candidates.set([]); } }

  protected newRoom() { this.roomId.set(null); this.roomForm.set({ building: '', roomNo: '', seats: '4', forGender: 'Male' }); this.roomErrs.set({}); this.roomOpen.set(true); }
  protected editRoom(r: Room) { this.roomId.set(r.id); this.roomForm.set({ building: r.building ?? '', roomNo: r.roomNo, seats: String(r.seats), forGender: r.forGender }); this.roomErrs.set({}); this.roomOpen.set(true); }
  protected patchRoom(p: Partial<RoomForm>) { this.roomForm.update((f) => ({ ...f, ...p })); }
  protected async saveRoom() {
    const f = this.roomForm(); this.savingRoom.set(true); this.roomErrs.set({});
    try {
      await this.api.saveRoom(this.roomId(), { building: f.building.trim() || null, roomNo: f.roomNo, seats: +toEnDigits(f.seats) || 0, forGender: f.forGender });
      this.toast.success('রুম সংরক্ষণ হয়েছে'); this.roomOpen.set(false); this.load();
    } catch (e) { const fe = fieldErrors(e); this.roomErrs.set(fe); if (!Object.keys(fe).length) this.toast.error(errorMessage(e)); }
    finally { this.savingRoom.set(false); }
  }
  protected async deleteRoom() {
    const id = this.roomId(); if (!id) return;
    if (!(await this.confirm.ask({ message: 'রুমটি তালিকা থেকে মুছে যাবে।', itemName: `রুম ${toBnDigits(this.roomForm().roomNo)}`, confirmLabel: 'মুছে ফেলুন' }))) return;
    try { await this.api.deleteRoom(id); this.toast.success('রুম মুছে ফেলা হয়েছে'); this.roomOpen.set(false); this.load(); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }

  protected seatClick(room: Room, no: number, o: Occupant | null) {
    if (o) { this.occ.set({ o, room }); this.occOpen.set(true); return; }
    this.allocRoom.set(room); this.allocSeat.set(no); this.q.set(''); this.candidates.set(null); this.allocOpen.set(true); this.search('');
  }
  protected async allocate(c: Resident) {
    const r = this.allocRoom(); if (!r) return;
    try {
      await this.api.allocate({ studentId: c.studentId, roomId: r.id, seatNo: this.allocSeat() });
      this.toast.success(`${c.name}-কে রুম ${toBnDigits(r.roomNo)}, সিট ${toBnDigits(String(this.allocSeat() ?? ''))} দেওয়া হয়েছে`);
      this.allocOpen.set(false); this.load();
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
  protected async vacate() {
    const x = this.occ(); if (!x) return;
    if (!(await this.confirm.ask({ message: 'সিট খালি হবে এবং শিক্ষার্থী আর আবাসিক থাকবে না।', itemName: `${x.o.name} · রুম ${toBnDigits(x.room.roomNo)}`, confirmLabel: 'সিট খালি করুন' }))) return;
    try { await this.api.vacate(x.o.allocationId); this.toast.success('সিট খালি করা হয়েছে'); this.occOpen.set(false); this.load(); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }

  protected async newLeave(studentId = 0) {
    this.leaveErrs.set({}); this.occOpen.set(false);
    this.leaveForm.set({ studentId, fromDate: todayIso(), toDate: '', reason: '', takenBy: '' });
    this.leaveOpen.set(true);
    try { this.residents.set(await this.api.residents()); } catch { /* list stays empty */ }
  }
  protected patchLeave(p: Partial<{ studentId: number; fromDate: string; toDate: string; reason: string; takenBy: string }>) { this.leaveForm.update((f) => ({ ...f, ...p })); }
  protected async saveLeave() {
    const f = this.leaveForm();
    if (!f.studentId) { this.leaveErrs.set({ studentId: 'শিক্ষার্থী বাছাই করুন' }); return; }
    this.savingLeave.set(true); this.leaveErrs.set({});
    try {
      await this.api.addLeave({ studentId: +f.studentId, fromDate: f.fromDate, toDate: f.toDate || null, reason: f.reason.trim() || null, takenBy: f.takenBy.trim() || null });
      this.toast.success('ছুটি লেখা হয়েছে'); this.leaveOpen.set(false); this.tab.set('leaves'); this.loadLeaves(this.openOnly()); this.load();
    } catch (e) { const fe = fieldErrors(e); this.leaveErrs.set(fe); if (!Object.keys(fe).length) this.toast.error(errorMessage(e)); }
    finally { this.savingLeave.set(false); }
  }
  protected async back(l: LeaveRow) {
    try { await this.api.returned(l.id); this.toast.success(`${l.name} ফিরে এসেছে`); this.loadLeaves(this.openOnly()); this.load(); }
    catch (e) { this.toast.error(errorMessage(e)); }
  }
  protected genderLabel = (g: Gender) => (g === 'Male' ? 'ছাত্র' : 'ছাত্রী');
}
