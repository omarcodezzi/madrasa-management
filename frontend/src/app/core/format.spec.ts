import { addMonths, formatDate, formatMoney, formatNumber, formatTime, monthName, toBnDigits, toEnDigits } from './format';

describe('format', () => {
  it('converts digits both ways', () => {
    expect(toBnDigits('2026-11')).toBe('২০২৬-১১');
    expect(toEnDigits('০১৭১১')).toBe('01711');
  });
  it('groups money the Bangladeshi way', () => {
    expect(formatMoney(12500, 'bn')).toBe('৳ ১২,৫০০');
    expect(formatMoney(1234567, 'en')).toBe('৳ 12,34,567');
  });
  it('accepts Bangla digit strings', () => {
    expect(formatNumber('১২৩৪', 'en')).toBe('1,234');
  });
  it('formats dates, months and times', () => {
    expect(formatDate('2026-11-22')).toBe('২২ নভেম্বর ২০২৬');
    expect(monthName('2026-01', 'en', 'en')).toBe('January 2026');
    expect(formatTime('14:05:00', 'en')).toBe('2:05');
    expect(addMonths('2026-11', 2)).toBe('2027-01');
  });
});
