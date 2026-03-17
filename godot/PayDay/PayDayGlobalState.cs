using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.PayDay.Loan;
using murph9.RallyGame2.godot.PayDay.Parts;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.PayDay;

public partial class PayDayGlobalState : Node {

    [Signal]
    public delegate void MoneyChangedEventHandler(float newAmount);
    [Signal]
    public delegate void DayEndedEventHandler(int dayNumber);
    [Signal]
    public delegate void LoanPaidEventHandler();
    [Signal]
    public delegate void RunEndedEventHandler();
    [Signal]
    public delegate void GameWonEventHandler();
    [Signal]
    public delegate void GameLostEventHandler();

    private readonly List<CollectedPart> _partInventory = [];
    public IReadOnlyCollection<CollectedPart> PartInventory => _partInventory;

    public float Money { get; private set; }
    public int DayNumber { get; private set; }
    public CarDetails CarDetails { get; private set; }
    public LoanState Loan { get; private set; }

    public PayDayGlobalState() {
        Reset();
    }

    public void Reset() {
        Money = 0f;
        DayNumber = 1;
        Loan = new LoanState(startPrincipal: 5000f);
        CarDetails = null;
    }

    public void SetCarDetails(CarDetails carDetails) {
        CarDetails = carDetails;
    }

    public void AddMoney(float amount) {
        Money += amount;
        EmitSignal(SignalName.MoneyChanged, Money);
    }

    public void SpendMoney(float amount) {
        Money -= amount;
        EmitSignal(SignalName.MoneyChanged, Money);
    }

    public void AddCollectedPart(CollectedPart part) {
        if (part.Rarity == PartLevel.Common)
            return; // do not save common parts, we always own them
        _partInventory.Add(part);
    }

    public void RemoveCollectedPart(CollectedPart part) {
        if (part.Rarity == PartLevel.Common)
            return; // do not remove common parts, we always own them
        _partInventory.Remove(part);
    }

    public void EndRun(float moneyEarned) {
        AddMoney(moneyEarned);
        EmitSignal(SignalName.RunEnded);
    }

    public void EndDay(float payment) {
        if (payment > 0)
            SpendMoney(payment);
        Loan.EndOfDay(payment);
        DayNumber++;
        EmitSignal(SignalName.DayEnded, DayNumber);

        if (Loan.IsRepaid) {
            EmitSignal(SignalName.LoanPaid);
            EmitSignal(SignalName.GameWon);
        } else if (Loan.IsDefaulted) {
            EmitSignal(SignalName.GameLost);
        }
    }
}
