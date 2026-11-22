import { setCodeDigits } from './format';
import { setTranslations, tr, trApi } from './tr';

describe('tr', () => {
  afterEach(() => { setTranslations('bn'); setCodeDigits('bn'); });

  it('keeps Bangla when the language is Bangla, and fills placeholders', () => {
    setTranslations('bn', { 'নতুন যোগ করুন': 'Add new' });
    expect(tr('নতুন যোগ করুন')).toBe('নতুন যোগ করুন');
    expect(tr('{n} জন', { n: '৫' })).toBe('৫ জন');
  });

  it('uses the English text and fills placeholders', () => {
    setTranslations('en', { 'নতুন যোগ করুন': 'Add new', '{n} জন': '{n}', 'রোল {n}': 'Roll {n}' });
    expect(tr('নতুন যোগ করুন')).toBe('Add new');
    expect(tr('রোল {n}', { n: 3 })).toBe('Roll 3');
  });

  it('falls back to the Bangla text when English is missing', () => {
    setTranslations('en', {});
    expect(tr('অজানা')).toBe('অজানা');
  });
});

describe('trApi', () => {
  afterEach(() => { setTranslations('bn'); setCodeDigits('bn'); });

  it('matches built API messages against templates and translates the parts', () => {
    setCodeDigits('en');
    setTranslations('en', {
      '{0} পাওয়া যায়নি': '{0} not found',
      'শিক্ষার্থী': 'Student',
      '{0} উপস্থিত': '{0} present',
      '{0} আগেই উপস্থিত': '{0} already present',
      'এই ফান্ডে আছে মাত্র {0} টাকা': 'This fund has only Tk {0}',
      '{0} পর্যন্ত': 'Up to {0}',
    });
    expect(trApi('শিক্ষার্থী পাওয়া যায়নি')).toBe('Student not found');
    expect(trApi('আব্দুল্লাহ আগেই উপস্থিত')).toBe('আব্দুল্লাহ already present');
    expect(trApi('আব্দুল্লাহ উপস্থিত')).toBe('আব্দুল্লাহ present');
    expect(trApi('এই ফান্ডে আছে মাত্র ১,২০০ টাকা')).toBe('This fund has only Tk 1,200');
    expect(trApi('নভেম্বর ২০২৬ পর্যন্ত')).toBe('Up to November 2026');
  });

  it('leaves Bangla API text alone in Bangla', () => {
    setTranslations('bn', { '{0} পাওয়া যায়নি': '{0} not found' });
    expect(trApi('শিক্ষার্থী পাওয়া যায়নি')).toBe('শিক্ষার্থী পাওয়া যায়নি');
  });
});
