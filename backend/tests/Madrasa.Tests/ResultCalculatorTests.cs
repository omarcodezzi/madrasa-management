using Madrasa.Domain.Entities;
using Madrasa.Domain.Services;
using static Madrasa.Domain.Services.ResultCalculator;

namespace Madrasa.Tests;

public class ResultCalculatorTests
{
    private static readonly GradeRule[] Rules =
    {
        new() { Id = 1, NameBn = "মুমতায", MinAverage = 80 },
        new() { Id = 2, NameBn = "জায়্যিদ জিদ্দান", MinAverage = 65 },
        new() { Id = 3, NameBn = "জায়্যিদ", MinAverage = 50 },
        new() { Id = 4, NameBn = "মাকবুল", MinAverage = 33 },
        new() { Id = 5, NameBn = "রাসিব", MinAverage = 0 },
    };

    [Theory]
    [InlineData(new[] { 90, 85, 80 }, "মুমতায")]
    [InlineData(new[] { 70, 65, 66 }, "জায়্যিদ জিদ্দান")]
    [InlineData(new[] { 50, 55, 52 }, "জায়্যিদ")]
    [InlineData(new[] { 40, 35, 34 }, "মাকবুল")]
    public void Division_follows_average(int[] marks, string expected)
    {
        var r = Calculate(marks.Select(m => new SubjectMark(m, false, 33)).ToList(), Rules);
        Assert.Equal(expected, r.Grade!.NameBn);
        Assert.Equal(0, r.FailedSubjects);
    }

    [Fact]
    public void Any_failed_subject_is_rasib_even_with_high_average()
    {
        var r = Calculate(new[] { new SubjectMark(99, false, 33), new SubjectMark(99, false, 33), new SubjectMark(20, false, 33) }, Rules);
        Assert.Equal("রাসিব", r.Grade!.NameBn);
        Assert.Equal(1, r.FailedSubjects);
    }

    [Fact]
    public void Absent_counts_as_failed()
    {
        var r = Calculate(new[] { new SubjectMark(90, false, 33), new SubjectMark(null, true, 33) }, Rules);
        Assert.Equal("রাসিব", r.Grade!.NameBn);
        Assert.Equal(45m, r.Average);
    }
}
