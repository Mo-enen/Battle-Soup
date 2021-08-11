using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public abstract class SoupStrategy_Advanced : SoupStrategy {


		// SUB
		public enum MVPConfig {
			Hidden = 0,
			Exposed = 1,
			Both = 2,
		}


		// Data
		protected int OwnAliveShipCount = 0;
		protected int OpponentAliveShipCount = 0;
		protected int ExposedShipCount = 0;
		protected int FoundShipCount = 0;
		protected int ShipWithMinimalPotentialPos = -1;
		protected int TileCount_GeneralWater = 0;
		protected int TileCount_RevealedWater = 0;
		protected int TileCount_RevealedShip = 0;
		protected int TileCount_HittedShip = 0;
		protected ShipPosition?[] ShipFoundPosition = null;
		protected List<ShipPosition>[] HiddenPotentialPos = null;
		protected List<ShipPosition>[] ExposedPotentialPos = null;
		protected int[] Cooldowns = null;
		protected float[,,] HiddenValues = new float[0, 0, 0];
		protected float[,,] ExposedValues = new float[0, 0, 0];
		protected int[,] SlimeValues = new int[0, 0];
		protected int[,] SlimeValues_HittedOnly = new int[0, 0];
		protected int[,] SlimeValues_RevealedOnly = new int[0, 0];
		protected int[] MostExposed = null;
		protected (Int2 pos, float max)[] HiddenValueMax = null;
		protected (Int2 pos, float max)[] ExposedValueMax = null;
		protected int UsingAbilityIndex = -1;



		// API
		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo oppInfo, int usingAbilityIndex = -1) {

			// Check
			string msg = AvailableCheck(ownInfo, oppInfo);
			if (!string.IsNullOrEmpty(msg)) {
				return new AnalyseResult() { ErrorMessage = msg, };
			}

			UsingAbilityIndex = usingAbilityIndex;

			// Cooldown
			Cooldowns = new int[ownInfo.Cooldowns.Length];
			for (int i = 0; i < ownInfo.Cooldowns.Length; i++) {
				Cooldowns[i] = ownInfo.ShipsAlive[i] ? ownInfo.Cooldowns[i] : -1;
			}

			// Alive Ship Count
			OwnAliveShipCount = ownInfo.AliveShipCount;
			OpponentAliveShipCount = oppInfo.AliveShipCount;

			// Revealed Tile Count
			TileCount_RevealedShip = 0;
			TileCount_HittedShip = 0;
			TileCount_RevealedWater = 0;
			TileCount_GeneralWater = 0;
			for (int y = 0; y < oppInfo.MapSize; y++) {
				for (int x = 0; x < oppInfo.MapSize; x++) {
					switch (oppInfo.Tiles[x, y]) {
						case Tile.RevealedShip:
							TileCount_RevealedShip++;
							break;
						case Tile.HittedShip:
							TileCount_HittedShip++;
							break;
						case Tile.GeneralWater:
							TileCount_GeneralWater++;
							break;
						case Tile.RevealedWater:
							TileCount_RevealedWater++;
							break;
					}
				}
			}

			// Potential
			if (!CalculatePotentialPositions(
				oppInfo,
				Tile.GeneralWater,
				Tile.GeneralWater,
				ref HiddenPotentialPos
			)) {
				return new AnalyseResult() {
					ErrorMessage = "Fail to calculate hidden positions",
				};
			}
			if (!CalculatePotentialPositions(
				oppInfo,
				Tile.HittedShip | Tile.RevealedShip,
				Tile.GeneralWater | Tile.HittedShip | Tile.RevealedShip,
				ref ExposedPotentialPos
			)) {
				return new AnalyseResult() {
					ErrorMessage = "Fail to calculate exposed positions",
				};
			}
			RemoveImpossiblePositions(
				oppInfo, ref HiddenPotentialPos, ref ExposedPotentialPos
			);

			// Values
			if (!CalculatePotentialValues(
				oppInfo,
				HiddenPotentialPos,
				ref HiddenValues, out _, out _
			)) {
				return new AnalyseResult() {
					ErrorMessage = "Fail to calculate hidden values",
				};
			}

			if (!CalculatePotentialValues(
				oppInfo,
				ExposedPotentialPos,
				ref ExposedValues, out _, out _
			)) {
				return new AnalyseResult() {
					ErrorMessage = "Fail to calculate exposed values",
				};
			}

			// Slime
			CalculateSlimeValues(oppInfo, Tile.All, ref SlimeValues);
			CalculateSlimeValues(oppInfo, Tile.RevealedShip, ref SlimeValues_RevealedOnly);
			CalculateSlimeValues(oppInfo, Tile.HittedShip, ref SlimeValues_HittedOnly);

			// Get Most Exposed
			if (MostExposed == null || MostExposed.Length != oppInfo.Ships.Length) {
				MostExposed = new int[oppInfo.Ships.Length];
			}
			for (int i = 0; i < oppInfo.Ships.Length; i++) {
				MostExposed[i] = GetMostExposedPositionIndex(oppInfo.Ships[i], oppInfo.Tiles, ExposedPotentialPos[i]);
			}

			// Get Value Max
			if (HiddenValueMax == null || HiddenValueMax.Length != oppInfo.Ships.Length + 1) {
				HiddenValueMax = new (Int2, float)[oppInfo.Ships.Length + 1];
			}
			if (ExposedValueMax == null || ExposedValueMax.Length != oppInfo.Ships.Length + 1) {
				ExposedValueMax = new (Int2, float)[oppInfo.Ships.Length + 1];
			}
			for (int i = 0; i < oppInfo.Ships.Length + 1; i++) {
				HiddenValueMax[i] = GetMaxValue(HiddenValues, i);
				ExposedValueMax[i] = GetMaxValue(ExposedValues, i);
			}

			// Exposed Ship Count
			ExposedShipCount = 0;
			for (int i = 0; i < ExposedPotentialPos.Length; i++) {
				int count = ExposedPotentialPos[i].Count;
				if (count > 0) {
					ExposedShipCount++;
				}
			}

			// Ship Found
			FoundShipCount = 0;
			if (ShipFoundPosition == null || ShipFoundPosition.Length != oppInfo.Ships.Length) {
				ShipFoundPosition = new ShipPosition?[oppInfo.Ships.Length];
			}
			for (int i = 0; i < ShipFoundPosition.Length; i++) {
				if (oppInfo.KnownPositions[i].HasValue) {
					ShipFoundPosition[i] = oppInfo.KnownPositions[i].Value;
					FoundShipCount++;
				} else if (HiddenPotentialPos[i].Count + ExposedPotentialPos[i].Count == 1) {
					ShipFoundPosition[i] = HiddenPotentialPos[i].Count > 0 ? HiddenPotentialPos[i][0] : ExposedPotentialPos[i][0];
					FoundShipCount++;
				}
			}

			// Ship with Minimal Potential-Pos-Count
			ShipWithMinimalPotentialPos = GetShipWithMinimalPotentialPosCount(oppInfo, HiddenPotentialPos, ExposedPotentialPos);

			return PerformTask(oppInfo, GetTask(oppInfo));
		}


		protected virtual string AvailableCheck (BattleInfo ownInfo, BattleInfo oppInfo) {

			if (oppInfo.Ships == null || oppInfo.Ships.Length == 0) {
				return "Can't analyse when opponent don't have ship";
			}
			if (FleetID.Length == 0 || ownInfo.Ships.Length == 0) {
				return "Can't analyse when the bot don't have ship";
			}
			if (ownInfo.Ships.Length != FleetID.Length) {
				string msg = $"There must be {FleetID.Length} ships (";
				foreach (var id in FleetID) {
					msg += id + ", ";
				}
				msg += ")";
				return msg;
			}
			for (int i = 0; i < FleetID.Length; i++) {
				var ship = ownInfo.Ships[i];
				if (ship.GlobalID != FleetID[i]) {
					return $"Ship No.{i + 1} must be {FleetID[i]}, not {ship.GlobalID}";
				}
			}
			if (ownInfo.AliveShipCount == 0 || oppInfo.AliveShipCount == 0) {
				return "No own/opponent ship alive now";
			}

			return "";
		}


		protected abstract string GetTask (BattleInfo oppInfo);


		protected abstract AnalyseResult PerformTask (BattleInfo oppInfo, string taskID);


		// Util
		public bool TryAttackShip (BattleInfo info, int targetIndex, Tile filter, out Int2 pos) {
			pos = default;
			var body = info.Ships[targetIndex].Body;
			foreach (var sPos in ExposedPotentialPos[targetIndex]) {
				foreach (var v in body) {
					var _pos = sPos.GetPosition(v);
					if (filter.HasFlag(info.Tiles[_pos.x, _pos.y])) {
						pos = _pos;
						return true;
					}
				}
			}
			foreach (var sPos in HiddenPotentialPos[targetIndex]) {
				foreach (var v in body) {
					var _pos = sPos.GetPosition(v);
					if (filter.HasFlag(info.Tiles[_pos.x, _pos.y])) {
						pos = _pos;
						return true;
					}
				}
			}
			return false;
		}


		public bool GetTileMVP (Tile[,] tiles, Tile filter, MVPConfig config, out Int2 pos) => GetTileMVP(tiles, filter, Tile.None, config, out pos);
		public bool GetTileMVP (Tile[,] tiles, Tile filter, Tile neighbourFilter, MVPConfig config, out Int2 pos) => GetTileMVP(tiles, filter, neighbourFilter, null, config, out pos, out _);
		public bool GetTileMVP (Tile[,] tiles, Tile filter, Tile neighbourFilter, List<Attack> attacks, MVPConfig config, out Int2 pos, out AbilityDirection direcion) {
			var hdValues = HiddenValues;
			var exValues = ExposedValues;
			int size = tiles.GetLength(0);
			int valueIndex = hdValues.GetLength(0) - 1;
			bool hasAttacks = attacks != null && attacks.Count > 0;
			float maxValue = 0;
			bool success = false;
			bool neighbour = neighbourFilter != Tile.None;
			pos = default;
			direcion = default;
			for (int j = 0; j < size; j++) {
				for (int i = 0; i < size; i++) {
					var tile = tiles[i, j];
					if (!filter.HasFlag(tile)) { continue; }
					float value = GetValue(i, j, out var _direcion);
					if (value > maxValue || (value == maxValue && Random.NextDouble() > 0.66666f)) {
						maxValue = value;
						pos.x = i;
						pos.y = j;
						success = true;
						direcion = _direcion;
					}
				}
			}
			return success;
			// Func
			float GetConfigValue (int _i, int _j) {
				float result = 0f;
				if (config == MVPConfig.Hidden || config == MVPConfig.Both) {
					result += hdValues[valueIndex, _i, _j];
				}
				if (config == MVPConfig.Exposed || config == MVPConfig.Both) {
					result += exValues[valueIndex, _i, _j];
				}
				return result;
			}
			float GetValue (int _i, int _j, out AbilityDirection _dir) {
				_dir = default;
				float result = GetConfigValue(_i, _j);
				if (neighbour) {
					if (!hasAttacks) {
						// Default Cross
						if (_i - 1 >= 0 && neighbourFilter.HasFlag(tiles[_i - 1, _j])) {
							result += GetConfigValue(_i - 1, _j);
						}
						if (_j - 1 >= 0 && neighbourFilter.HasFlag(tiles[_i, _j - 1])) {
							result += GetConfigValue(_i, _j - 1);
						}
						if (_i + 1 < size && neighbourFilter.HasFlag(tiles[_i + 1, _j])) {
							result += GetConfigValue(_i + 1, _j);
						}
						if (_j + 1 < size && neighbourFilter.HasFlag(tiles[_i, _j + 1])) {
							result += GetConfigValue(_i, _j + 1);
						}
					} else {
						// Attacks
						float attResult = 0f;
						for (int i = 0; i < 4; i++) {
							float currentResult = 0;
							var dir = (AbilityDirection)i;
							foreach (var att in attacks) {
								if (att.Trigger == AttackTrigger.PassiveRandom || att.Trigger == AttackTrigger.Random) { continue; }
								var (_x, _y) = att.GetPosition(_i, _j, dir);
								if (_x < 0 || _x >= size || _y < 0 || _y >= size) { continue; }
								var _tile = tiles[_x, _y];
								if (!att.AvailableTarget.HasFlag(_tile) || !neighbourFilter.HasFlag(_tile)) { continue; }
								currentResult += GetConfigValue(_x, _y);
							}
							if (currentResult > attResult) {
								attResult = currentResult;
								_dir = dir;
							}
						}
						result += attResult;
					}
				}
				return result;
			}
		}


	}
}
