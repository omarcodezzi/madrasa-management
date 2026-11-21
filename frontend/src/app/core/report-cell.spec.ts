import { reportCell } from './format';

describe('reportCell', () => {
  it('formats each column kind', () => {
    expect(reportCell(12500, 'money', 'bn')).toBe('৳ ১২,৫০০');
    expect(reportCell(84.25, 'pct', 'en')).toBe('84.3%');
    expect(reportCell('2026-11-05', 'date', 'en')).toContain('2026');
    expect(reportCell('2026-11', 'month', 'bn')).toBe('নভেম্বর ২০২৬');
    expect(reportCell(null, 'money', 'bn')).toBe('');
    expect(reportCell('প্রারম্ভিক জের', 'text', 'bn')).toBe('প্রারম্ভিক জের');
  });
});
