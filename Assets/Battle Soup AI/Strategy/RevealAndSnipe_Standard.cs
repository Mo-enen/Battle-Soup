using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class RevealAndSnipe_Standard : SoupStrategy {




		#region --- VAR ---


		// Api
		public override string DisplayName => "Reveal & Snipe";
		public override string Description => "Standard strategy for Reveal&Snipe.";
		public override string[] FleetID => new string[] { "Coracle", "Whale", "KillerSquid", "SeaTurtle", };

		// Data
		private List<ShipPosition>[] HiddenPositions = new List<ShipPosition>[0];
		private List<ShipPosition>[] ExposedPositions = new List<ShipPosition>[0];
		private float[,,] HiddenValues = new float[0, 0, 0];
		private float[,,] ExposedValues = new float[0, 0, 0];
		private (Int2 pos, float max) HiddenValueMax = default;
		private (Int2 pos, float max) ExposedValueMax = default;
		private int[] MostExposed = null;
		private int AliveShipCount = 0;
		private int BestTargetIndex = -1;


		#endregion




		#region --- API ---


		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo opponentInfo, int usingAbilityIndex = -1) {

			var result = new AnalyseResult() {
				AbilityIndex = -1,
				ErrorMessage = "",
			};

			// Calculate
			string msg = CalculateCache(opponentInfo);
			if (!string.IsNullOrEmpty(msg)) {
				result.ErrorMessage = msg;
				return result;
			}

			// Normal
			result.TargetPosition = AnalyseNormalAttack(opponentInfo, out float highScore);

			// Ability
			for (int aIndex = 0; aIndex < ownInfo.Ships.Length; aIndex++) {
				if (ownInfo.ShipsAlive[aIndex] && ownInfo.Cooldowns[aIndex] <= 0) {
					var (_pos, _dir, _score) = AnalyseAbility(aIndex, opponentInfo);
					if (_score > highScore) {
						result.AbilityIndex = aIndex;
						result.TargetPosition = _pos;
						result.AbilityDirection = _dir;
						highScore = _score;
					}
				}
			}
			if (highScore <= 0f) {
				result.ErrorMessage = "Fail to analyse.";
			}

			return result;
		}


		#endregion




		#region --- LGC ---


		private string CalculateCache (BattleInfo info) {

			// Ship Alive Check
			AliveShipCount = GetLiveShipCount(info);
			if (AliveShipCount == 0) { return "No Ship Alive"; }

			// Positions
			if (!CalculatePotentialPositions(
				info,
				Tile.GeneralWater,
				Tile.GeneralWater,
				ref HiddenPositions
			)) { return "Fail to calculate potential positions"; }

			if (!CalculatePotentialPositions(
				info,
				Tile.HittedShip | Tile.RevealedShip,
				Tile.GeneralWater | Tile.HittedShip | Tile.RevealedShip,
				ref ExposedPositions
			)) { return "Fail to calculate potential positions"; }

			RemoveImpossiblePositions(
				info,
				ref HiddenPositions, ref ExposedPositions
			);

			// Values
			if (!CalculatePotentialValues(
				info,
				HiddenPositions,
				ref HiddenValues, out _, out _
			)) { return "Fail to calculate potential values"; }

			if (!CalculatePotentialValues(
				info,
				ExposedPositions,
				ref ExposedValues, out _, out _
			)) { return "Fail to calculate potential values"; }

			// Get Value Max
			int shipCount = info.Ships.Length;
			HiddenValueMax = GetMaxValue(HiddenValues, shipCount);
			ExposedValueMax = GetMaxValue(ExposedValues, shipCount);

			// Get Most Exposed
			if (MostExposed == null || MostExposed.Length != shipCount) {
				MostExposed = new int[shipCount];
			}
			for (int i = 0; i < shipCount; i++) {
				MostExposed[i] = GetMostExposedPositionIndex(info.Ships[i], info.Tiles, ExposedPositions[i]);
			}

			// Get Best Hunt Target
			BestTargetIndex = GetShipWithMinimalPotentialPosCount(info, HiddenPositions, ExposedPositions);

			return "";
		}


		private Int2 AnalyseNormalAttack (BattleInfo info, out float score) {

			Int2 finalPos = default;
			float finalScore = 0f;
			score = 0f;

			// Alive Check

			if (AliveShipCount == 0) { return default; }

			// Hit Hidden Water
			var pos = HiddenValueMax.pos;
			var tile = info.Tiles[pos.x, pos.y];
			if (tile == Tile.GeneralWater) {
				TrySetScoreAndPos(40f, pos);
			}
			// Hit Reveal Ship
			pos = ExposedValueMax.pos;
			tile = info.Tiles[pos.x, pos.y];
			if (tile == Tile.GeneralWater || tile == Tile.RevealedShip) {
				TrySetScoreAndPos(45f, pos);
			}


			// Hunt Target Ship
			if (BestTargetIndex >= 0) {
				int huntID = HuntShip(BestTargetIndex, info, out var huntTargetPos);
				switch (huntID) {
					case 2: // Hidden
						TrySetScoreAndPos(
							AliveShipCount == 1 ? 75f : AliveShipCount == 2 ? 65f : 50f,
							huntTargetPos
						);
						break;
					case 1: // Exposed
						TrySetScoreAndPos(
							AliveShipCount == 1 ? 85f : AliveShipCount == 2 ? 75f : 60f,
							huntTargetPos
						);
						break;
					case 3: // Known
						TrySetScoreAndPos(
							AliveShipCount == 1 ? 95 : AliveShipCount == 2 ? 85f : 65f,
							huntTargetPos
						);
						break;
				}
			}

			score = finalScore;

			return finalPos;
			// Func
			void TrySetScoreAndPos (float _score, Int2 _pos) {
				if (_score > finalScore) {
					finalPos = _pos;
					finalScore = _score;
				}
			}
		}


		private (Int2 pos, AbilityDirection direction, float score) AnalyseAbility (int abilityIndex, BattleInfo info) {
			Int2 pos = default;
			AbilityDirection dir = default;
			float score = 0f;
			switch (abilityIndex) {
				case 0: {
					// Coracle
					if (GetBestValuedTile(ExposedValues, info.Ships.Length, info.Tiles, Tile.RevealedShip, false, out var bestRPos)) {
						pos = bestRPos;
						if (AliveShipCount == 1) {
							score = 9999f;
						} else {
							score = 95f;
						}
					}
					break;
				}
				case 1: {
					// Whale
					if (AliveShipCount == 1) {
						if (BestTargetIndex >= 0) {
							int huntID = HuntShip(BestTargetIndex, info, out var huntTargetPos);
							if (huntID > 0) {
								pos = huntTargetPos;
								score = 85f;
							}
						}
					} else {
						if (GetBestValuedTile(ExposedValues, info.Ships.Length, info.Tiles, Tile.GeneralWater, true, out var bestRPos)) {
							bool isShip = info.Tiles[bestRPos.x, bestRPos.y] == Tile.RevealedShip;
							pos = bestRPos;
							score = isShip ? 70f : 85f;
						}
					}
					break;
				}
				case 2: {
					// KillerSquid
					if (GetBestValuedTile(ExposedValues, info.Ships.Length, info.Tiles, Tile.RevealedShip | Tile.GeneralWater, true, out var bestRPos)) {
						bool isShip = info.Tiles[bestRPos.x, bestRPos.y] == Tile.RevealedShip;
						pos = bestRPos;
						score = isShip ? 70f : 85f;
					}
					break;
				}
				case 3: {
					// SeaTurtle
					if (BestTargetIndex >= 0) {
						int huntID = HuntShip(BestTargetIndex, info, out var huntTargetPos);
						if (huntID > 0) {
							pos = huntTargetPos;
						}
					} else {
						pos = HiddenValueMax.pos;
					}
					var tile = info.Tiles[pos.x, pos.y];
					if (tile == Tile.GeneralWater || tile == Tile.RevealedShip) {
						score = 65f;
					}
					break;
				}
			}
			return (pos, dir, score);
		}


		private int HuntShip (int shipIndex, BattleInfo info, out Int2 result) {
			// 0:Fail, 1:Exposed, 2: Hidden, 3: Known

			result = default;

			if (info.KnownPositions[shipIndex].HasValue) {
				// Position is Known
				var sPos = info.KnownPositions[shipIndex].Value;
				var body = info.Ships[shipIndex].Body;
				foreach (var v in body) {
					var pos = sPos.GetPosition(v);
					var tile = info.Tiles[pos.x, pos.y];
					if (tile == Tile.GeneralWater || tile == Tile.RevealedShip) {
						result = pos;
						break;
					}
				}
				return 3;
			} else if (ExposedPositions[shipIndex].Count > 0) {
				// Exposed
				int mostExIndex = MostExposed[shipIndex];
				if (mostExIndex >= 0) {
					var sPos = ExposedPositions[shipIndex][mostExIndex];
					var body = info.Ships[shipIndex].Body;
					foreach (var v in body) {
						var pos = sPos.GetPosition(v);
						var tile = info.Tiles[pos.x, pos.y];
						if (tile == Tile.GeneralWater || tile == Tile.RevealedShip) {
							result = pos;
							break;
						}
					}
					return 1;
				}
			} else if (HiddenPositions[shipIndex].Count > 0) {
				// Hidden
				var pos = HiddenValueMax.pos;
				if (info.Tiles[pos.x, pos.y] == Tile.GeneralWater) {
					result = pos;
					return 2;
				}
			}

			return 0;
		}


		#endregion




	}
}
