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
		private (Int2 pos, float max) HiddenValueMax = default;
		private (Int2 pos, float max) ExposedValueMax = default;
		private (int index, int exposure)[] MostExposed = null;
		private int AliveShipCount = 0;
		private int BestTargetIndex = -1;


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

			// Ship Alive Check
			AliveShipCount = 0;
			int lastAliveShipIndex = -1;
			for (int i = 0; i < info.ShipsAlive.Length; i++) {
				bool alive = info.ShipsAlive[i];
				if (alive) {
					AliveShipCount++;
					lastAliveShipIndex = i;
				}
			}
			if (AliveShipCount == 0) { return "No Ship Alive"; }

			// Positions
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

			// Values
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
			FillValue(true);
			FillValue(false);

			// Get Most Exposed
			if (MostExposed == null || MostExposed.Length != shipCount) {
				MostExposed = new (int, int)[shipCount];
			}
			for (int i = 0; i < shipCount; i++) {
				MostExposed[i] = GetMostExposedPosition(info.Ships[i], info.Tiles, ExposedPositions[i]);
			}

			// Get Best Hunt Target
			BestTargetIndex = AliveShipCount == 1 ?
				lastAliveShipIndex :
				GetBestHuntTarget(info, HiddenPositions, ExposedPositions);

			return "";
			// Func
			void FillValue (bool hidden) {
				var values = hidden ? HiddenValues : ExposedValues;
				var valueMax = hidden ? HiddenValueMax : ExposedValueMax;
				valueMax.max = 0;
				for (int j = 0; j < mapSize; j++) {
					for (int i = 0; i < mapSize; i++) {
						float v0 = values[shipCount, i, j];
						if (v0 > valueMax.max) {
							valueMax.max = v0;
							valueMax.pos.x = i;
							valueMax.pos.y = j;
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

			if (AliveShipCount == 0) { return (default, 0); }

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
				int huntID = Hunt(BestTargetIndex, info, out var huntTargetPos);
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

			return (finalPos, finalScore);
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
					if (GetBestTile(ExposedValues, info.Ships.Length, info.Tiles, Tile.RevealedShip, false, out var bestRPos)) {
						pos = bestRPos;
						if (AliveShipCount == 1) {
							score = 9999f;
						} else {
							score = 85f;
						}
					}
					break;
				}
				case 1: {
					// Whale
					if (AliveShipCount == 1) {
						if (BestTargetIndex >= 0) {
							int huntID = Hunt(BestTargetIndex, info, out var huntTargetPos);
							if (huntID > 0) {
								pos = huntTargetPos;
								score = 85f;
							}
						}
					} else {
						if (GetBestTile(ExposedValues, info.Ships.Length, info.Tiles, Tile.GeneralWater, true, out var bestRPos)) {
							bool isShip = info.Tiles[bestRPos.x, bestRPos.y] == Tile.RevealedShip;
							pos = bestRPos;
							score = isShip ? 70f : 85f;
						}
					}
					break;
				}
				case 2: {
					// KillerSquid
					if (GetBestTile(ExposedValues, info.Ships.Length, info.Tiles, Tile.RevealedShip | Tile.GeneralWater, true, out var bestRPos)) {
						bool isShip = info.Tiles[bestRPos.x, bestRPos.y] == Tile.RevealedShip;
						pos = bestRPos;
						score = isShip ? 70f : 85f;
					}
					break;
				}
				case 3: {
					// SeaTurtle
					if (BestTargetIndex >= 0) {
						int huntID = Hunt(BestTargetIndex, info, out var huntTargetPos);
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


		private bool GetBestTile (float[,,] values, int shipCount, Tile[,] tiles, Tile filter, bool neighbour, out Int2 pos) {
			int size = tiles.GetLength(0);
			float maxValue = 0;
			bool success = false;
			pos = default;
			for (int j = 0; j < size; j++) {
				for (int i = 0; i < size; i++) {
					float value = GetValue(i, j);
					var tile = tiles[i, j];
					if (value > maxValue && filter.HasFlag(tile)) {
						maxValue = value;
						pos.x = i;
						pos.y = j;
					}
				}
			}
			return success;
			// Func
			float GetValue (int _i, int _j) {
				float result = values[shipCount, _i, _j];
				if (neighbour) {
					if (_i - 1 >= 0) {
						result += values[shipCount, _i - 1, _j];
					}
					if (_j - 1 >= 0) {
						result += values[shipCount, _i, _j - 1];
					}
					if (_i + 1 < size) {
						result += values[shipCount, _i + 1, _j];
					}
					if (_j + 1 < size) {
						result += values[shipCount, _i, _j + 1];
					}
				}
				return result;
			}
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
