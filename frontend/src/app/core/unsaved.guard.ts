import { CanDeactivateFn } from '@angular/router';

export interface HasUnsavedChanges { hasUnsavedChanges(): boolean; }

/** Never lose user input (spec 8.6): ask before leaving a dirty form. */
export const unsavedGuard: CanDeactivateFn<HasUnsavedChanges> = (c) =>
  !c.hasUnsavedChanges() || window.confirm('আপনার পরিবর্তনগুলো সংরক্ষণ করা হয়নি। পাতা ছেড়ে গেলে এগুলো হারিয়ে যাবে। তবুও যাবেন?');
