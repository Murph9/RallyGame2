using Godot;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.Cars.Init;

public enum CarMake {
//	Normal,
	Runner,
	/*Rally,
	Roadster,

	Hunter,
	Ricer,
	Muscle,
	Wagon,
	Bus,

	Ultra,
	LeMans,
	Inline,
	TouringCar,
	Hill,

	WhiteSloth,
	Rocket,

	Debug*/
}

public static class CarMakeExtensions
{
	public static CarDetails LoadFromFile(this CarMake type, Vector3 gravity) {
        var carDetails = FileLoader.ReadJsonFile<CarDetails>("Cars", "Init", "Data", type.ToString() + ".json");
        carDetails.Engine = EngineDetails.LoadFromFile(carDetails.EngineFileName);

        carDetails.LoadSelf(gravity);
		return carDetails;
	}
}
