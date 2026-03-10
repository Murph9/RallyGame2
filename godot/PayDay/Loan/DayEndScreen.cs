using Godot;

namespace murph9.RallyGame2.godot.PayDay.Loan;

/// <summary>
/// End-of-day loan screen. Shows the loan breakdown with escalating interest
/// and comedic flavour text. Player chooses: Pay All, Pay Minimum, or Skip.
/// Also displays how much money was earned in the last run for context.
/// </summary>
public partial class DayEndScreen : CenterContainer {

    [Signal]
    public delegate void DayEndedEventHandler(float amountPaid);

    private PayDayGlobalState _state;

    private Label _dayLabel;
    private Label _runMoneyLabel;
    private Label _principalLabel;
    private Label _interestLabel;
    private Label _tomorrowLabel;
    private Label _flavourLabel;
    private Button _payAllButton;
    private Button _payMinimumButton;

    private float _runMoney;

    public void SetRunMoney(float runMoney) {
        _runMoney = runMoney;
        // Refresh if already in tree
        if (_runMoneyLabel != null)
            Refresh();
    }

    public override void _Ready() {
        _state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        _dayLabel = GetNode<Label>("Panel/VBox/DayLabel");
        _runMoneyLabel = GetNode<Label>("Panel/VBox/RunMoneyLabel");
        _principalLabel = GetNode<Label>("Panel/VBox/PrincipalLabel");
        _interestLabel = GetNode<Label>("Panel/VBox/InterestLabel");
        _tomorrowLabel = GetNode<Label>("Panel/VBox/TomorrowLabel");
        _flavourLabel = GetNode<Label>("Panel/VBox/FlavourLabel");
        _payAllButton = GetNode<Button>("Panel/VBox/Buttons/PayAllButton");
        _payMinimumButton = GetNode<Button>("Panel/VBox/Buttons/PayMinimumButton");
        Refresh();
    }

    public void Refresh() {
        var loan = _state.Loan;

        if (_dayLabel != null)
            _dayLabel.Text = $"Day {loan.DayNumber}";

        if (_runMoneyLabel != null) {
            if (_runMoney > 0f)
                _runMoneyLabel.Text = $"Earned this run: ${_runMoney:F0}   |   Wallet: ${_state.Money:F0}";
            else
                _runMoneyLabel.Text = $"Wallet: ${_state.Money:F0}";
        }

        if (_principalLabel != null)
            _principalLabel.Text = $"Outstanding Balance: ${loan.Principal:F2}";

        if (_interestLabel != null)
            _interestLabel.Text = $"Today's Rate: {loan.DailyInterestRate * 100:F0}%  (interest owed: ${loan.MinimumPayment:F2})";

        if (_tomorrowLabel != null)
            _tomorrowLabel.Text = $"Skip and tomorrow's balance: ${loan.TomorrowPrincipalIfSkipped:F2}  @ {loan.TomorrowInterestRate * 100:F0}%";

        if (_flavourLabel != null)
            _flavourLabel.Text = LoanTermsLines.GetFlavourText(loan.DayNumber);

        if (_payAllButton != null)
            _payAllButton.Disabled = _state.Money < loan.Principal;

        if (_payMinimumButton != null)
            _payMinimumButton.Disabled = _state.Money < loan.MinimumPayment;
    }

    public void PayAllButton_Pressed() {
        EmitSignal(SignalName.DayEnded, _state.Loan.Principal);
    }

    public void PayMinimumButton_Pressed() {
        EmitSignal(SignalName.DayEnded, _state.Loan.MinimumPayment);
    }

    public void SkipButton_Pressed() {
        EmitSignal(SignalName.DayEnded, 0f);
    }
}
