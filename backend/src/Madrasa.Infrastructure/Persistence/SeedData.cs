using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Infrastructure.Persistence;

/// <summary>Reference data every madrasa needs on day one. Sample students are added by DevSeeder in Development only.</summary>
internal static class SeedData
{
    public static void Apply(ModelBuilder b)
    {
        b.Entity<MadrasaProfile>().HasData(new MadrasaProfile
        {
            Id = 1, NameBn = "রামারবাগ তা'মীরুল কুরআন মাদরাসা", NameAr = "رماربغ تعمير القرآن مدرسة",
            Address = "রামারবাগ কেন্দ্রিয় ঈদগা, রামারবাগ, উত্তরদা, লাকসাম, কুমিল্লা", Phone = "01975448094", StudentIdPrefix = "RM"
        });
        b.Entity<AcademicSession>().HasData(new AcademicSession
        { Id = 1, Name = "2026", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31), IsCurrent = true });

        b.Entity<AppSetting>().HasData(
            new AppSetting { Key = "digits", Value = "bn" },
            new AppSetting { Key = "weeklyHoliday", Value = "5" },   // Friday
            new AppSetting { Key = "language", Value = "bn" });

        b.Entity<Department>().HasData(
            new Department { Id = 1, NameBn = "নূরানী বিভাগ", Kind = DepartmentKind.Nurani, SortOrder = 1 },
            new Department { Id = 2, NameBn = "নাজেরা বিভাগ", Kind = DepartmentKind.Nazera, SortOrder = 2 },
            new Department { Id = 3, NameBn = "হিফজ বিভাগ", NameAr = "قسم تحفيظ القرآن", Kind = DepartmentKind.Hifz, SortOrder = 3 },
            new Department { Id = 4, NameBn = "কিতাব বিভাগ", Kind = DepartmentKind.Kitab, SortOrder = 4 });

        b.Entity<GradeRule>().HasData(
            new GradeRule { Id = 1, NameBn = "মুমতায", NameAr = "ممتاز", MinAverage = 80, SortOrder = 1 },
            new GradeRule { Id = 2, NameBn = "জায়্যিদ জিদ্দান", NameAr = "جيد جدا", MinAverage = 65, SortOrder = 2 },
            new GradeRule { Id = 3, NameBn = "জায়্যিদ", NameAr = "جيد", MinAverage = 50, SortOrder = 3 },
            new GradeRule { Id = 4, NameBn = "মাকবুল", NameAr = "مقبول", MinAverage = 33, SortOrder = 4 },
            new GradeRule { Id = 5, NameBn = "রাসিব", NameAr = "راسب", MinAverage = 0, SortOrder = 5 });

        b.Entity<Fund>().HasData(
            new Fund { Id = 1, NameBn = "সাধারণ ফান্ড", Code = "GENERAL" },
            new Fund { Id = 2, NameBn = "যাকাত ফান্ড", Code = "ZAKAT", IsRestricted = true },
            new Fund { Id = 3, NameBn = "সদকা ফান্ড", Code = "SADAQAH", IsRestricted = true },
            new Fund { Id = 4, NameBn = "লিল্লাহ বোর্ডিং ফান্ড", Code = "LILLAH", IsRestricted = true });

        b.Entity<FeeHead>().HasData(
            new FeeHead { Id = 1, NameBn = "ভর্তি ফি", Frequency = FeeFrequency.Once, DefaultAmount = 1000 },
            new FeeHead { Id = 2, NameBn = "মাসিক বেতন", Frequency = FeeFrequency.Monthly, DefaultAmount = 500 },
            new FeeHead { Id = 3, NameBn = "পরীক্ষা ফি", Frequency = FeeFrequency.PerExam, DefaultAmount = 200 },
            new FeeHead { Id = 4, NameBn = "বোর্ডিং ফি", Frequency = FeeFrequency.Monthly, DefaultAmount = 2500 });

        b.Entity<LedgerCategory>().HasData(
            new LedgerCategory { Id = 1, NameBn = "ছাত্র বেতন ও ফি", Kind = LedgerKind.Income },
            new LedgerCategory { Id = 2, NameBn = "দান / অনুদান", Kind = LedgerKind.Income },
            new LedgerCategory { Id = 3, NameBn = "শিক্ষক-কর্মচারী বেতন", Kind = LedgerKind.Expense },
            new LedgerCategory { Id = 4, NameBn = "বোর্ডিং ও খাবার", Kind = LedgerKind.Expense },
            new LedgerCategory { Id = 5, NameBn = "বিদ্যুৎ ও ইউটিলিটি", Kind = LedgerKind.Expense },
            new LedgerCategory { Id = 6, NameBn = "নির্মাণ ও মেরামত", Kind = LedgerKind.Expense },
            new LedgerCategory { Id = 7, NameBn = "অন্যান্য", Kind = LedgerKind.Expense });
    }
}
