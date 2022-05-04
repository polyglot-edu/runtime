using System.Collections.Generic;

namespace Polyglot.Gamification;

public record GameStateReport(double ExercisePoints, double AssignmentPoints, double ExerciseGoldCoins, double AssignmentGoldCoins, IEnumerable<string> Badges);