using Godot;
using murph9.RallyGame2.godot.PayDay.Loan;

namespace murph9.RallyGame2.godot.PayDay.Loan;

/// <summary>
/// End-of-day loan screen. Shows the loan breakdown with escalating interest
/// and comedic flavour text. Player chooses: Pay All, Pay Minimum, or Skip.
/// </summary>
public partial class DayEndScreen : CenterContainer {

    [Signal]
    public delegate void DayEndedEventHandler(float amountPaid);

    private Label _principalLabel;
    private Label _interestLabel;
    private Label _tomorrowLabel;
    private Label _flavourLabel;
    private Button _payAllButton;
    private Button _payMinimumButton;

    public override void _Ready() {
        _principalLabel = GetNodeOrNull<Label>("Panel/VBox/PrincipalLabel");
        _interestLabel = GetNodeOrNull<Label>("Panel/VBox/InterestLabel");
        _tomorrowLabel = GetNodeOrNull<Label>("Panel/VBox/TomorrowLabel");
        _flavourLabel = GetNodeOrNull<Label>("Panel/VBox/FlavourLabel");
        _payAllButton = GetNodeOrNull<Button>("Panel/VBox/Buttons/PayAllButton");
        _payMinimumButton = GetNodeOrNull<Button>("Panel/VBox/Buttons/PayMinimumButton");
        Refresh();
    }

    public void Refresh() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        var loan = state.Loan;

        if (_principalLabel != null)
            _principalLabel.Text = $"Outstanding Balance: ${loan.Principal:F2}";

        if (_interestLabel != null)
            _interestLabel.Text = $"Today's Rate: {loan.DailyInterestRate * 100:F0}%  (interest owed: ${loan.MinimumPayment:F2})";

        if (_tomorrowLabel != null)
            _tomorrowLabel.Text = $"Skip and tomorrow's balance: ${loan.TomorrowPrincipalIfSkipped:F2}  @ {loan.TomorrowInterestRate * 100:F0}%";

        if (_flavourLabel != null)
            _flavourLabel.Text = LoanTermsLines.GetFlavourText(loan.DayNumber);

        if (_payAllButton != null)
            _payAllButton.Disabled = state.Money < loan.Principal;

        if (_payMinimumButton != null)
            _payMinimumButton.Disabled = state.Money < loan.MinimumPayment;
    }

    public void PayAllButton_Pressed() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        EmitSignal(SignalName.DayEnded, state.Loan.Principal);
    }

    public void PayMinimumButton_Pressed() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        EmitSignal(SignalName.DayEnded, state.Loan.MinimumPayment);
    }

    public void SkipButton_Pressed() {
        EmitSignal(SignalName.DayEnded, 0f);
    }
}
