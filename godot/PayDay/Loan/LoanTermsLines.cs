namespace murph9.RallyGame2.godot.PayDay.Loan;

public static class LoanTermsLines {
    public static string GetFlavourText(int dayNumber) {
        return dayNumber switch {
            1 => "Welcome to EZ Cash Solutions! Just sign here, here, and here. Don't read the rest.",
            2 => "As per our standard terms, your rate is subject to 'market adjustment'. The market has adjusted.",
            3 => "Congratulations! You've unlocked our Premium Debtor tier. This is not a good thing.",
            4 => "Clause 12(c): In the event of non-payment, we reserve the right to feelings of disappointment.",
            5 => "As per clause 47(b), a 'vibes adjustment' fee of 12% now applies. The vibes are bad.",
            6 => "Your account has been flagged for our 'Loyalty Programme'. Loyalty means you keep paying.",
            7 => "Day 7 review complete. Our analyst, Dave, looked at your account. Dave is not optimistic.",
            8 => "Under the Financial Suffering Act 2019, we are required to tell you: this is a lot of money.",
            9 => "Your interest rate has been revised to account for 'ambient economic uncertainty'. It's worse.",
            10 => "At this rate of growth, your debt will exceed the GDP of a small nation by Day 14.",
            _ => $"Day {dayNumber}: We've stopped sending notices. The numbers speak for themselves."
        };
    }

    public static string GetTermsAndConditionsSummary(int dayNumber) {
        return dayNumber switch {
            1 => "Standard terms apply. Ha.",
            2 => "Terms have been updated. You agreed by continuing to exist.",
            3 => "New terms: worse. Same terms: also worse. All terms: worse.",
            4 => "We've added a 'breathing surcharge'. It's in the contract.",
            5 => "Terms now include a clause about your weekend plans.",
            _ => "Terms: bad. Very bad. Comically bad."
        };
    }
}
