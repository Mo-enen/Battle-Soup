using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class Strategy_RevealAndSnipe : SoupStrategy {




		#region --- VAR ---


		// Api
		public override string DisplayName => "Reveal & Snipe";
		public override string Description => "Knowledge is power! Find your enemy and take them down with precision.";
		public override string[] FleetID => new string[] { "Coracle", "Whale", "KillerSquid", "SeaTurtle", };

		// Data
		private List<ShipPosition>[] HiddenPositions = new List<ShipPosition>[0];
		private List<ShipPosition>[] ExposedPositions = new List<ShipPosition>[0];
		private float[,,] HiddenValues = new float[0, 0, 0];
		private float[,,] ExposedValues = new float[0, 0, 0];
		private (Int2 pos0, Int2 pos1, float max0, float max1, bool alone0) HiddenValueMax = default;
		private (Int2 pos0, Int2 pos1, float max0, float max1, bool alone0) ExposedValueMax = default;
		private (int index, int exposure)[] MostExposed = null;


		#endregion



		#region --- API ---


		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo opponentInfo, int usingAbilityIndex = -1) {

			var result = new AnalyseResult() {
				AbilityIndex = -1,
				ErrorMessage = "",
			};
			float highScore;

			// Calculate
			string msg = CalculateCache(opponentInfo);
			if (!string.IsNullOrEmpty(msg)) {
				result.ErrorMessage = msg;
				return result;
			}

			// Normal
			(result.TargetPosition, highScore) = AnalyseNormalAttack(opponentInfo);

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

			if (!CalculatePotentialPositions(
				info.Ships,
				info.ShipsAlive,
				info.Tiles,
				info.KnownPositions,
				Tile.GeneralWater,
				Tile.GeneralWater,
				ref HiddenPositions
			)) { return "Fail to calculate potential positions"; }

			if (!CalculatePotentialPositions(
				info.Ships,
				info.ShipsAlive,
				info.Tiles,
				info.KnownPositions,
				Tile.HittedShip | Tile.RevealedShip,
				Tile.GeneralWater | Tile.HittedShip | Tile.RevealedShip,
				ref ExposedPositions
			)) { return "Fail to calculate potential positions"; }

			RemoveImpossiblePositions(
				info.Ships,
				info.MapSize,
				ref HiddenPositions,
				ref ExposedPositions
			);

			if (!CalculatePotentialValues(
				info.Ships,
				info.MapSize,
				HiddenPositions,
				ref HiddenValues, out _, out _
			)) { return "Fail to calculate potential values"; }

			if (!CalculatePotentialValues(
				info.Ships,
				info.MapSize,
				ExposedPositions,
				ref ExposedValues, out _, out _
			)) { return "Fail to calculate potential values"; }

			// Get Value Max
			int shipCount = info.Ships.Length;
			int mapSize = info.MapSize;
			HiddenValueMax.alone0 = true;
			ExposedValueMax.alone0 = true;
			FillValue(true);
			FillValue(false);

			// Get Most Exposed
			if (MostExposed == null || MostExposed.Length != shipCount) {
				MostExposed = new (int, int)[shipCount];
			}
			for (int i = 0; i < shipCount; i++) {
				MostExposed[i] = GetMostExposedPosition(info.Ships[i], info.Tiles, ExposedPositions[i]);
			}

			return "";
			// Func
			void FillValue (bool hidden) {
				var values = hidden ? HiddenValues : ExposedValues;
				var valueMax = hidden ? HiddenValueMax : ExposedValueMax;
				valueMax.max0 = 0;
				valueMax.max1 = 0;
				for (int j = 0; j < mapSize; j++) {
					for (int i = 0; i < mapSize; i++) {
						float v0 = values[shipCount, i, j];
						if (v0 > valueMax.max0) {
							valueMax.max0 = v0;
							valueMax.pos0.x = i;
							valueMax.pos0.y = j;
						} else if (v0 == valueMax.max0) {
							valueMax.alone0 = false;
						}
						float v1 = values[shipCount + 1, i, j];
						if (v1 > valueMax.max1) {
							valueMax.max1 = v1;
							valueMax.pos1.x = i;
							valueMax.pos1.y = j;
						}
					}
				}
				if (hidden) {
					HiddenValueMax = valueMax;
				} else {
					ExposedValueMax = valueMax;
				}
			}
		}


		private (Int2 pos, float score) AnalyseNormalAttack (BattleInfo info) {

			Int2 finalPos = default;
			float finalScore = 0f;

			// Alive Check
			int aliveShipCount = 0;
			int lastAliveShipIndex = -1;
			for (int i = 0; i < info.ShipsAlive.Length; i++) {
				bool alive = info.ShipsAlive[i];
				if (alive) {
					aliveShipCount++;
					lastAliveShipIndex = i;
				}
			}
			if (aliveShipCount == 0) { return (default, 0); }

			// Hit Hidden Water
			var pos = HiddenValueMax.alone0 ? HiddenValueMax.pos0 : HiddenValueMax.pos1;
			var tile = info.Tiles[pos.x, pos.y];
			if (tile == Tile.GeneralWater) {
				TrySetScoreAndPos(40f, pos);
			}
			// Hit Reveal Ship
			pos = ExposedValueMax.alone0 ? ExposedValueMax.pos0 : ExposedValueMax.pos1;
			tile = info.Tiles[pos.x, pos.y];
			if (tile == Tile.GeneralWater || tile == Tile.RevealedShip) {
				TrySetScoreAndPos(45f, pos);
			}

			// Get Best Hunt Target
			int bestTargetIndex = aliveShipCount == 1 ?
				lastAliveShipIndex :
				GetBestHuntTarget(info, HiddenPositions, ExposedPositions);

			// Hunt Target Ship
			if (bestTargetIndex >= 0) {
				int huntID = Hunt(bestTargetIndex, info, out var huntTargetPos);
				switch (huntID) {
					case 2: // Hidden
						TrySetScoreAndPos(
							aliveShipCount == 1 ? 75f : aliveShipCount == 2 ? 65f : 50f,
							huntTargetPos
						);
						break;
					case 1: // Exposed
						TrySetScoreAndPos(
							aliveShipCount == 1 ? 85f : aliveShipCount == 2 ? 75f : 60f,
							huntTargetPos
						);
						break;
					case 3: // Known
						TrySetScoreAndPos(
							aliveShipCount == 1 ? 95 : aliveShipCount == 2 ? 85f : 65f,
							huntTargetPos
						);
						break;
				}
			}

			return (finalPos, finalScore);
			// Func
			void TrySetScoreAndPos (float _score, Int2 _pos) {
				if (_score > finalScore) {
					finalPos = _pos;
					finalScore = _score;
				}
			}
		}


		private (Int2 pos, AbilityDirection direction, float score) AnalyseAbility (
			int abilityIndex, BattleInfo opponentInfo
		) {





			return (default, default, 0f);
		}


		private int GetBestHuntTarget (BattleInfo info, List<ShipPosition>[] hiddenPositions, List<ShipPosition>[] exposedPositions) {
			int bestTargetIndex = -1;
			int bestHiddenPosLeft = int.MaxValue;
			int bestExposedPosLeft = int.MaxValue;
			int bestExTargetIndex = -1;
			int bestHdTargetIndex = -1;
			for (int i = 0; i < info.Ships.Length; i++) {
				var alive = info.ShipsAlive[i];
				if (!alive) { continue; }
				if (bestTargetIndex == -1) {
					bestTargetIndex = i;
					continue;
				}
				int exCount = exposedPositions[i].Count;
				if (exCount > 0 && exCount < bestExposedPosLeft) {
					bestExposedPosLeft = exCount;
					bestExTargetIndex = i;
				}
				int hdCount = hiddenPositions[i].Count;
				if (hdCount > 0 && hdCount < bestHiddenPosLeft) {
					bestHiddenPosLeft = hdCount;
					bestHdTargetIndex = i;
				}
			}
			if (bestExTargetIndex >= 0) {
				bestTargetIndex = bestExTargetIndex;
			} else if (bestHdTargetIndex >= 0) {
				bestTargetIndex = bestHdTargetIndex;
			}
			return bestTargetIndex;
		}


		private int Hunt (int shipIndex, BattleInfo info, out Int2 result) {
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
				int mostExIndex = MostExposed[shipIndex].index;
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
				var pos = HiddenValueMax.alone0 ? HiddenValueMax.pos0 : HiddenValueMax.pos1;
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
