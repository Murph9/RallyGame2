using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.PayDay.Loan;

public record LoanHistoryEntry(int DayNumber, float PrincipalBefore, float Payment, float InterestRate, float PrincipalAfter);

public class LoanState {
    private const float BASE_INTEREST_RATE = 0.08f;
    private const float INTEREST_RATE_INCREASE_PER_DAY = 0.02f;

    public float StartPrincipal { get; }
    public float Principal { get; private set; }
    public int DayNumber { get; private set; }

    public float DailyInterestRate => BASE_INTEREST_RATE + (DayNumber - 1) * INTEREST_RATE_INCREASE_PER_DAY;
    public float TomorrowInterestRate => BASE_INTEREST_RATE + DayNumber * INTEREST_RATE_INCREASE_PER_DAY;
    public float MinimumPayment => Principal * DailyInterestRate;
    public float TomorrowPrincipalIfSkipped => Principal * (1f + DailyInterestRate);

    public bool IsRepaid => Principal <= 0;
    public bool IsDefaulted => Principal > StartPrincipal * 10f;

    public List<LoanHistoryEntry> History { get; } = [];

    public LoanState(float startPrincipal) {
        StartPrincipal = startPrincipal;
        Principal = startPrincipal;
        DayNumber = 1;
    }

    /// <summary>Apply end-of-day: deduct payment, compound interest, advance day.</summary>
    public void EndOfDay(float payment) {
        float before = Principal;
        float rate = DailyInterestRate;
        float remaining = Math.Max(0f, Principal - payment);
        Principal = remaining * (1f + rate);
        History.Add(new LoanHistoryEntry(DayNumber, before, payment, rate, Principal));
        DayNumber++;
    }
}
