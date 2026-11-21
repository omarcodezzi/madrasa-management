using Madrasa.Domain.Entities;

namespace Madrasa.Domain.Services;

/// <summary>Befaq-style division: any failed subject → রাসিব, otherwise highest rule whose MinAverage ≤ average.</summary>
public static class ResultCalculator
{
    public record SubjectMark(decimal? Marks, bool IsAbsent, int PassMarks);
    public record Outcome(decimal Total, decimal Average, int FailedSubjects, GradeRule? Grade);

    public static Outcome Calculate(IReadOnlyList<SubjectMark> marks, IReadOnlyList<GradeRule> rules)
    {
        if (marks.Count == 0) return new Outcome(0, 0, 0, null);
        decimal total = marks.Sum(m => m.IsAbsent ? 0 : m.Marks ?? 0);
        decimal avg = Math.Round(total / marks.Count, 2);
        int failed = marks.Count(m => m.IsAbsent || (m.Marks ?? 0) < m.PassMarks);
        var ordered = rules.OrderByDescending(r => r.MinAverage).ToList();
        var grade = failed > 0
            ? ordered.LastOrDefault()                       // lowest rule = রাসিব
            : ordered.FirstOrDefault(r => avg >= r.MinAverage);
        return new Outcome(total, avg, failed, grade);
    }
}
