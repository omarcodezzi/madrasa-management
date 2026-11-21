import { environment } from '../../environments/environment';

export const API = environment.apiUrl;

export interface PagedResult<T> { items: T[]; total: number; page: number; pageSize: number; }
export interface ProblemDetails { title?: string; status?: number; detail?: string; }

/** Pulls the Bangla message out of an API error, with a safe fallback. */
export function errorMessage(e: unknown, fallback = 'কিছু একটা সমস্যা হয়েছে। আবার চেষ্টা করুন।'): string {
  const err = e as { status?: number; error?: ProblemDetails };
  if (err?.status === 0) return 'সার্ভারের সাথে সংযোগ পাওয়া যায়নি। ইন্টারনেট দেখুন।';
  return err?.error?.title || fallback;
}

/** Uploaded files (/uploads/...) are served by the API host. */
export function fileUrl(path: string | null | undefined): string | null {
  if (!path) return null;
  if (/^(https?:|data:)/.test(path)) return path;
  return API.replace(/\/api$/, '') + path;
}

export type Gender = 'Male' | 'Female';
export type StudentStatus = 'Active' | 'Left' | 'Graduated' | 'Suspended';
export type AttendanceStatus = 'Present' | 'Absent' | 'Late' | 'Leave';
export type PaymentMethod = 'Cash' | 'BKash' | 'Nagad' | 'Bank';
