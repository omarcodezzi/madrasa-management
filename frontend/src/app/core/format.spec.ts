import { formatMoney, formatNumber, toBnDigits, toEnDigits } from './format';

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
});
