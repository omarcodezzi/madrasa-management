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
