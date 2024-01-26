using Godot;
using System;

namespace murph9.RallyGame2.godot.Cars.Sim;

public class CalcWheelTraction {


    /// <summary>
    /// no pacejka, based on a stepped function instead
    /// every input is meant to be positive
    /// _S = radians, peak traction point
    /// _T = number, peak traction amount
    /// _L = radians, peak length
    /// _D = radians-ish, traction drop off after peak
    /// g(x)=If(x<_S, _T*((x)/(_S)), If(x>_S+_L, _T+(_S+_L-x) _D, _T))
    /// </summary>
    public static double Calc(double slip, double s, double t, double l, double d) {
        // because i am bad at math negative values go through as positive
        if (slip < 0) {
            return Mathf.Sign(slip) * Calc(Math.Abs(slip), s, t, l, d);
        }

        // before peak, quick ramp to t
        if (slip < s) {
            return t*slip/s;
        }

        // after peak with decay to t/2
        if (slip > s+l) {
            return Mathf.Max(t+(s+l-slip)*d, t/2);
        }

        // at peak
        return t;
    }
}
