import { Gender, StudentStatus } from '../../core/api';
import { tr } from '../../core/tr';

export interface StudentRow {
  id: number; studentCode: string; name: string; gender: Gender; classId?: number; className?: string; roll?: number;
  fatherName?: string; phone?: string; photoPath?: string; status: StudentStatus; isOrphan: boolean; isResidential: boolean; admissionDate: string;
}
export interface Guardian {
  id?: number | null; name: string; relation: string; phone: string; altPhone?: string | null; occupation?: string | null;
  nid?: string | null; address?: string | null; isPrimary: boolean;
}
export interface StudentDetail {
  id: number; studentCode: string; name: string; nameAr?: string; gender: Gender; dateOfBirth?: string; birthRegNo?: string; bloodGroup?: string;
  fatherName?: string; motherName?: string; phone?: string; presentAddress?: string; permanentAddress?: string; photoPath?: string;
  admissionDate: string; isOrphan: boolean; isResidential: boolean; status: StudentStatus;
  classId?: number; className?: string; departmentName?: string; sectionId?: number; roll?: number; monthlyFee?: number; monthlyFeeOverride?: number;
  guardians: Guardian[];
}
export interface StudentInput {
  name: string; nameAr?: string | null; gender: Gender; dateOfBirth?: string | null; birthRegNo?: string | null; bloodGroup?: string | null;
  fatherName?: string | null; motherName?: string | null; phone?: string | null; presentAddress?: string | null; permanentAddress?: string | null;
  admissionDate?: string | null; isOrphan: boolean; isResidential: boolean; status?: StudentStatus | null;
  classId: number | null; sectionId?: number | null; roll?: number | null; monthlyFeeOverride?: number | null;
  guardians: Guardian[]; photoDataUrl?: string | null;
}
export interface DayMark { date: string; status?: import('../../core/api').AttendanceStatus | null; checkIn?: string | null; isWorkDay: boolean; }
export interface StudentMonth { month: string; workDays: number; present: number; late: number; absent: number; leave: number; percent: number; days: DayMark[]; }

export const STATUS_LABEL: Record<StudentStatus, string> = { Active: tr('সক্রিয়'), Left: tr('চলে গেছে'), Graduated: tr('পাস করেছে'), Suspended: tr('স্থগিত') };
export const GENDER_LABEL: Record<Gender, string> = { Male: tr('ছাত্র'), Female: tr('ছাত্রী') };
