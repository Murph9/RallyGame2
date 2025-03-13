using Godot;

namespace murph9.RallyGame2.godot.Utilities;

public class MyMath {
    public static float MsToKmh(float speed) => speed * 3.6f;
    public static double MsToKmh(double speed) => speed * 3.6f;
    public static float KmhToMs(float speed) => speed / 3.6f;
    public static double KmhToMs(double speed) => speed / 3.6f;

    public static double GetCircleCenterFrom(Vector3 p1, Vector3 p2, Vector3 p3) {
        // https://stackoverflow.com/a/68502665/9353639

        // triangle "edges"
        var t = p2 - p1;
        var u = p3 - p1;
        var v = p3 - p2;

        // triangle normal
        var w = t.Cross(u);
        var wsl = w.Dot(w);

        // area of the triangle is too small (you may additionally check the points for colinearity if you are paranoid)
        // if (wsl < 10e-14)
        //     return double.MaxValue;

        // helpers
        var iwsl2 = 1f / (2f * wsl);
        var tt = t.Dot(t);
        var uu = u.Dot(u);

        // result circle
        Vector3 circCenter = p1 + (u * tt * (u.Dot(v)) - t * uu * (t.Dot(v))) * iwsl2;
        var circRadius = Mathf.Sqrt(tt * uu * (v.Dot(v)) * iwsl2 * 0.5f);
        Vector3 circAxis = w / Mathf.Sqrt(wsl);

        return circRadius;
    }
}