/* ============================================================================
   MMS — মাদরাসা ম্যানেজমেন্ট সিস্টেম : পুরো ডেটাবেস স্ক্রিপ্ট (Microsoft SQL Server 2019/2022)
   ----------------------------------------------------------------------------
   কী আছে:
     • ডেটাবেস [MMS] তৈরি (না থাকলে)
     • ৫১টি টেবিল, প্রাইমারি/ফরেন কী, ইউনিক ও অন্যান্য ইনডেক্স
     • শুরুর ডেটা: ফান্ড (সাধারণ, যাকাত, সদকা, লিল্লাহ), আয়-ব্যয়ের খাত, ফি-এর খাত, গ্রেড, বিভাগ, মাদরাসার তথ্য
     • রিপোর্টের ভিউ, ফাংশন ও স্টোরড প্রসিডিউর (নিচে তালিকা)
     • __EFMigrationsHistory — অ্যাপ বুঝতে পারে ডেটাবেস কোন সংস্করণে আছে

   চালানো: SSMS বা Azure Data Studio-তে খুলে Execute (F5), অথবা
       sqlcmd -S localhost -U sa -P "<password>" -C -i MMS.sql
   স্ক্রিপ্টটি বারবার চালানো নিরাপদ (idempotent): যা আগে থেকে আছে তা আবার তৈরি করে না।

   যা স্ক্রিপ্টে নেই, অ্যাপ প্রথমবার চালু হলে নিজে করে:
     • ৯টি রোল (সুপার অ্যাডমিন, মুহতামিম, অফিস, হিসাবরক্ষক, উস্তাদ…) ও তাদের অনুমতি
     • প্রথম অ্যাডমিন ইউজার — পাসওয়ার্ড হ্যাশ করতে হয়, তাই SQL দিয়ে নয়
       (Seed__AdminPassword সেটিং থেকে; deploy/setup.sh এটি তৈরি করে)

   রিপোর্ট অবজেক্ট (শুধু পড়ে, ডেটা বদলায় না):
     ভিউ      vw_CurrentStudents   চলতি শিক্ষাবর্ষের সব শিক্ষার্থী, শ্রেণি, রোল, অভিভাবক
              vw_FeeDues           প্রতিটি বকেয়া ফি (খাত, মাস, পরিশোধ, বাকি)
              vw_FundBalances      ফান্ড অনুযায়ী মোট আয়, ব্যয় ও ব্যালান্স
              vw_MonthlyLedger     মাস ও খাত অনুযায়ী আয়-ব্যয়
     ফাংশন    fn_CurrentSessionId(), fn_StudentDue(@StudentId)
     প্রসিডিউর usp_DashboardSummary                         এক লাইনে আজকের সারসংক্ষেপ
              usp_DailyAttendance @Date                    শ্রেণি অনুযায়ী দিনের হাজিরা
              usp_StudentAttendance @StudentId,@From,@To   একজনের হাজিরা ও মোট
              usp_MonthlyCollection '2026-11'              মাসের ফি আদায়, শ্রেণি ও খাত অনুযায়ী
              usp_DueList @ClassId                         বকেয়া তালিকা (অভিভাবকের মোবাইলসহ)
              usp_LedgerStatement @From,@To,@FundId        আয়-ব্যয় বিবরণী, চলমান ব্যালান্সসহ
              usp_ExamMeritList @ExamId,@ClassId           পরীক্ষার মেধা তালিকা
              usp_SalarySheet '2026-11'                    মাসের বেতন শিট (দেওয়া/বাকি)
              usp_BackupDatabase N'D:\Backup'              পুরো ব্যাকআপ ও যাচাই

   কোড: লিঙ্গ 1=ছাত্র 2=ছাত্রী · অবস্থা 1=সক্রিয় 2=চলে গেছে 3=উত্তীর্ণ 4=স্থগিত
        হাজিরা 1=উপস্থিত 2=অনুপস্থিত 3=দেরি 4=ছুটি · লেনদেন 1=আয় 2=ব্যয়
   টাকা decimal(12,2), তারিখ date, মাস 'yyyy-MM' টেক্সট। বাংলা লেখা nvarchar (Unicode)।

   এই ফাইলটি database/build.sh দিয়ে EF Core মাইগ্রেশন থেকে তৈরি — হাতে বদলাবেন না।
   ============================================================================ */

-- Filtered indexes need these (SSMS sets them by default, sqlcmd does not)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF DB_ID(N'MMS') IS NULL
BEGIN
    CREATE DATABASE [MMS];
END;
GO
-- Nightly full backups (deploy/backup.sh) are the recovery plan, so the log does not need to grow between them
ALTER DATABASE [MMS] SET RECOVERY SIMPLE;
GO
USE [MMS];
GO

